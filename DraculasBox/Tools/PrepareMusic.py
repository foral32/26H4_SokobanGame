"""Prepare the selected, credited music without changing its musical dynamics."""
from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
import urllib.request
from pathlib import Path

import numpy as np

PROJECT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(tempfile.gettempdir()) / 'draculas-music-tools'))
import imageio_ffmpeg

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
REVIEW = PROJECT / 'DraculasBox/Reviews/20261110/Music'
OUTPUT = PROJECT / 'Assets/DraculasBox/Art/Audio/Music'
ARCHIVE = PROJECT / 'DraculasBox/Audio/Source'
REPORT = PROJECT / 'DraculasBox/Evidence/Music'
SELECTION = (
    ('hypnotic-jewels', 'hypnotic-jewels.ogg', 'HypnoticJewels.ogg'),
    ('puzzle-b', 'puzzle-b.mp3', 'PuzzleTune1B.ogg'),
    ('underclocked', 'underclocked.mp3', 'Reserve/Underclocked.mp3'),
)


def run(*args: str, raw: bool = False):
    return subprocess.run([FFMPEG, '-hide_banner', '-nostdin', *map(str, args)],
                          capture_output=True, check=True, text=not raw)


def loudness(path: Path) -> dict:
    output = run('-i', path, '-af', 'loudnorm=I=-20:TP=-2:LRA=11:print_format=json', '-f', 'null', '-').stderr
    return json.JSONDecoder().raw_decode(output[output.rfind('{'):])[0]


def main():
    for folder in (OUTPUT, OUTPUT / 'Reserve', ARCHIVE, REPORT):
        folder.mkdir(parents=True, exist_ok=True)
    catalog = json.loads((REVIEW / 'sources.json').read_text(encoding='utf-8'))
    tracks = {t['id']: t for t in catalog['tracks']}
    records = []
    for key, original, target in SELECTION:
        track = tracks[key]
        archived = ARCHIVE / original
        if not archived.exists():
            cached = REVIEW / 'source_audio' / original
            if cached.exists():
                shutil.copyfile(cached, archived)
            else:
                request = urllib.request.Request(track['download'], headers={'User-Agent': 'Mozilla/5.0'})
                with urllib.request.urlopen(request, timeout=60) as response:
                    archived.write_bytes(response.read())
        digest = hashlib.sha256(archived.read_bytes()).hexdigest()
        if digest != track['sourceHash']:
            raise ValueError(f'The audition source changed: {key}')
        output = OUTPUT / target
        record = {k: track[k] for k in ('id', 'title', 'artist', 'source', 'download', 'license', 'licenseUrl')}
        record.update(sourceSha256=digest, asset=output.relative_to(PROJECT).as_posix(), active=key != 'underclocked')
        if key == 'underclocked':
            shutil.copyfile(archived, output)
            record['changes'] = 'Original MP3, archived only; not referenced by the game.'
        else:
            measured = loudness(archived)
            # Constant gain preserves dynamics and the authored loop seam. Dynamic
            # loudnorm could change the gain at opposite ends of a looping track.
            gain = min(-20 - float(measured['input_i']), -2.2 - float(measured['input_tp']))
            raw = run('-v', 'error', '-i', archived, '-ar', '44100', '-ac', '2', '-f', 'f32le', 'pipe:1', raw=True).stdout
            samples = np.frombuffer(raw, dtype='<f4').reshape(-1, 2)
            trim_head, trim_tail = (1105, 623) if key == 'puzzle-b' else (0, 0)
            duration = (len(samples) - trim_head - trim_tail) / 44100
            record.update(frames=len(samples)-trim_head-trim_tail, sampleRate=44100, duration=duration,
                          inputLUFS=float(measured['input_i']), gainDb=round(gain, 5),
                          seamJumpBeforeGain=float(np.max(np.abs(samples[-1] - samples[0]))),
                          trimHeadSamples=trim_head, trimTailSamples=trim_tail, edgeFadeSeconds=.004)
            # Decode the MP3's gapless sample metadata before re-encoding to Vorbis.
            # There is no MP3 encoder padding in the runtime loop.
            filters = ['aresample=44100']
            if trim_head or trim_tail:
                filters.extend([f'atrim=start_sample={trim_head}:end_sample={len(samples)-trim_tail}', 'asetpts=PTS-STARTPTS'])
            filters.extend([f'volume={gain:.8f}dB', 'afade=t=in:d=0.004', f'afade=t=out:st={duration-.004:.8f}:d=0.004'])
            run('-v', 'error', '-y', '-i', archived, '-af', ','.join(filters),
                '-ar', '44100', '-ac', '2', '-map_metadata', '-1', '-codec:a', 'libvorbis', '-q:a', '6', output)
            result = loudness(output)
            record.update(outputLUFS=float(result['input_i']), outputTruePeak=float(result['input_tp']))
            record['changes'] = 'Constant-gain loudness matching; 4 ms click-prevention edges; decoded to 44.1 kHz stereo Vorbis. Puzzle B MP3 encoder padding removed. Arrangement and tempo unchanged.'
        record['assetSha256'] = hashlib.sha256(output.read_bytes()).hexdigest()
        records.append(record)
        print(f'Prepared {key}', flush=True)
    (REPORT / 'sources.json').write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps([{k: r[k] for k in ('id', 'active', 'asset')} | {k: r[k] for k in ('duration', 'outputLUFS', 'outputTruePeak', 'seamJumpBeforeGain') if k in r} for r in records], ensure_ascii=True), flush=True)


if __name__ == '__main__':
    main()
