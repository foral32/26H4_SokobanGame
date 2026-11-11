"""Deterministic, original short dry impact / burn / ash sound. No external samples."""
from pathlib import Path
import math
import random
import struct
import wave


def build(path: Path):
    hz, seconds = 44100, .62
    rng = random.Random(41551)
    values, low, slow = [], 0.0, 0.0
    for i in range(round(hz * seconds)):
        t = i / hz
        noise = rng.uniform(-1, 1)
        low += .32 * (noise - low)
        slow += .038 * (noise - slow)
        breath = low - slow
        attack = min(1, t / .004)
        tail = min(1, (seconds - t) / .08)
        hiss = breath * attack * math.exp(-t / .16) * .64
        body = math.sin(2 * math.pi * (198 * t - 42 * t * t)) * math.exp(-t / .027) * attack * .18
        papery = 0
        for at, gain in ((.045, .12), (.10, .09), (.17, .075), (.255, .055), (.35, .04)):
            age = t - at
            if 0 < age < .042:
                papery += breath * math.sin(min(1, age / .002) * math.pi / 2) * math.exp(-age / .009) * gain
        values.append((hiss + body + papery) * tail)
    gain = .50 / max(abs(value) for value in values)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), 'wb') as output:
        output.setparams((1, 2, hz, 0, 'NONE', 'not compressed'))
        output.writeframes(b''.join(struct.pack('<h', round(max(-1, min(1, value * gain)) * 32767)) for value in values))


if __name__ == '__main__':
    build(Path(__file__).resolve().parents[3] / 'Assets/DraculasBox/Art/Audio/burn.wav')
