"""Install the approved 01_error_005 audition without its player-only padding.

Source: Kenney, Interface Sounds, error_005.ogg (CC0-1.0).
The approved gain and attack/release are already baked into the audition.
No pitch changes, extra layers, or leading delay are added here.
"""
from pathlib import Path
import wave


ROOT = Path(__file__).resolve().parents[3]
AUDITION = ROOT / 'DraculasBox/Audio/Source/InterfaceSounds/01_error_005-approved.wav'


def build(path: Path):
    with wave.open(str(AUDITION), 'rb') as source:
        channels, width, rate = source.getnchannels(), source.getsampwidth(), source.getframerate()
        start, tail = round(rate * .15), round(rate * .25)
        count = source.getnframes() - start - tail
        assert channels == 2 and width == 2 and count > 0
        source.setpos(start)
        frames = source.readframes(count)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), 'wb') as output:
        output.setparams((channels, width, rate, 0, 'NONE', 'not compressed'))
        output.writeframes(frames)


if __name__ == '__main__':
    build(ROOT / 'Assets/DraculasBox/Art/Audio/burn.wav')
