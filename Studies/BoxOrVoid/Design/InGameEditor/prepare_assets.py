"""Package existing study art and layout for the review-only HTML; no image modification."""
from pathlib import Path
import json, base64, shutil

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3] / 'Assets/Studies/BoxOrVoid'
catalog = json.loads((ROOT / 'Content/Catalog.json').read_text('utf-8-sig'))
names = set()
for theme in catalog['themes']:
    for group in ['sprites', 'backgrounds']:
        names.update(entry['sprite'] for entry in theme[group])
names.update(['Cursor', 'Main Menu Background', 'Particle_Box_Uncolored', 'Main Menu Button Panel', 'Main Menu Pointer', 'Play_active', 'Settings_active', 'Return_active', 'Goal_complete', 'Tut World Tile-sc', 'Box World Tile'])
sprites = {}
for name in names:
    path = ROOT / 'Art/Sprites' / (name + '.png')
    if not path.exists():
        path = ROOT / 'Art/Sprites' / (name + '_0.png')
    if path.exists():
        sprites[name] = 'data:image/png;base64,' + base64.b64encode(path.read_bytes()).decode('ascii')
data = {'themes': catalog['themes'], 'sprites': sprites, 'levels': []}
for name in ['n_box_6','n_tut_8','n_box_12']:
    data['levels'].append(json.loads((ROOT / 'Content/Levels' / (name + '.json')).read_text('utf-8-sig')))
(HERE / 'review-assets.js').write_text('window.REVIEW_ASSETS=' + json.dumps(data, ensure_ascii=False, separators=(',',':')) + ';', encoding='utf-8')
shutil.copyfile(ROOT / 'Art/Fonts/NotoSansSC-Black.ttf', HERE / 'ReviewFont.ttf')
print('Prepared', len(sprites), 'reference sprites for the design mockup.')
