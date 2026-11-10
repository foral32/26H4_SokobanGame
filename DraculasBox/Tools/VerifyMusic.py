"""Check decoded loop seams and export a short audition of the runtime fade curve."""
import json
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(tempfile.gettempdir()) / 'draculas-music-tools'))
import imageio_ffmpeg

ff = imageio_ffmpeg.get_ffmpeg_exe()
out = ROOT / 'DraculasBox/Evidence/Music'
out.mkdir(parents=True, exist_ok=True)
tracks = []
checks = []
for name in ('HypnoticJewels', 'PuzzleTune1B'):
    path = ROOT / 'Assets/DraculasBox/Art/Audio/Music' / (name + '.ogg')
    raw = subprocess.run([ff, '-v', 'error', '-i', str(path), '-ar', '44100', '-ac', '2', '-f', 'f32le', 'pipe:1'],
                         check=True, capture_output=True).stdout
    samples = np.frombuffer(raw, dtype='<f4').reshape(-1, 2)
    jump = float(np.max(np.abs(samples[-1] - samples[0])))
    assert jump < .002, (name, 'loop discontinuity', jump)
    assert float(np.max(np.abs(samples))) < .9, (name, 'insufficient mix headroom')
    tracks.append(samples)
    checks.append(dict(name=name, frames=len(samples), seconds=len(samples)/44100, seamJump=jump))
assert len(tracks[1]) == 48 * 44100
time = np.arange(24*44100, dtype=np.float64)/44100
blend = np.zeros(len(time))
for start, source, target in ((5, 0, 1), (14, 1, 0)):
    t = np.clip((time-start)/.9, 0, 1)
    area = time >= start
    blend[area] = source + (target-source)*(t[area]**2*(3-2*t[area]))
gains = (np.cos(blend*np.pi/2), np.sin(blend*np.pi/2))
assert np.max(np.abs(gains[0]**2+gains[1]**2-1)) < 1e-12
mixed = sum(track[(np.arange(len(time))+24*44100)%len(track)]*gain[:,None] for track,gain in zip(tracks,gains))*.8
# Fade the audition file itself at its ends; the in-game loops remain continuous.
edge = np.minimum(np.clip(time/.12,0,1),np.clip((24-time)/.2,0,1))
mixed = (mixed * edge[:,None]).astype('<f4')
assert np.max(np.abs(mixed)) < 1
preview = out / 'transition-preview.mp3'
subprocess.run([ff,'-v','error','-y','-f','f32le','-ar','44100','-ac','2','-i','pipe:0','-codec:a','libmp3lame','-b:a','192k',str(preview)],
               input=mixed.tobytes(),check=True,capture_output=True)
snippet=out/'transition-snippet.ogg'
subprocess.run([ff,'-v','error','-y','-ss','2','-i',str(preview),'-t','8','-ar','24000','-ac','1','-codec:a','libopus','-b:a','20k',str(snippet)],
               check=True,capture_output=True)
report=dict(passed=True, loops=checks, crossfadeSeconds=.9, preview=str(preview.relative_to(ROOT)),
            previewTimeline='0–5 s main; 5–5.9 s crossfade; 5.9–14 s editor; 14–14.9 s crossfade; 14.9–24 s main',
            previewNote='Offline audition using the same smooth equal-power curve, at an audition gain. Runtime behavior is verified separately in Play Mode.')
(out/'loop-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=True))
