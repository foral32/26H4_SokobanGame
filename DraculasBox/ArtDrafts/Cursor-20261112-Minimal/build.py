"""Three restrained arrow drafts. Review only; no Unity or system cursor changes."""
from pathlib import Path
import ast
import base64
import json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
INK, FACE, LIGHT = '#332a31', '#fff5e5', '#fffaf2'
ARROW = 'M3 2 L20.7 16.8 L12.7 16.8 L16.8 25 L12.7 27 L8.4 18.7 L3 23.5 Z'

def path(d, fill, stroke='none', width=1.2):
    return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round"/>'

def icon(key, state):
    fill = '#eddfcc' if state == 'pressed' else LIGHT if state == 'hover' else FACE
    if key == 'A':
        parts = [path(ARROW, fill, INK)]
    elif key == 'B':
        accent = '#703043' if state == 'pressed' else '#ac5265' if state == 'hover' else '#923a52'
        parts = [path(ARROW, fill, INK),
                 path('M12 17 L16.05 24.7 L12.8 26.2 L12.15 25.05 L14.7 24 L11 17 Z', accent),
                 path('M13.1 16.1 L19.1 16.1 L20 16.8 L13.4 16.8 Z', accent)]
    else:
        shape = 'M3 2 L20.5 16.3 L20 17 L12.9 17 L17.1 25.2 L16.8 26.2 L13.5 27.8 L12.6 27.3 L8.3 19 L3.9 23.4 L3 23 Z'
        parts = [path(shape, fill, INK),
                 path('M4 4 L4 21 L4.65 20.4 L4.65 5.2 Z', '#fffdf7'),
                 path('M12 17 L16.2 25 L13.5 26.4 L13 25.6 L15.1 24.6 L11.4 17 Z', '#d6c9b6'),
                 path('M12.6 16.2 L19.3 16.2 L20 17 L12.9 17 Z', '#d6c9b6')]
    return '<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">' + ''.join(parts) + '</svg>'

specs = [('A', '素白', '奶白主体，深色细描边。'),
         ('B', '细边', '只在右下沿留一线酒红。'),
         ('C', '切角', '尾端轻微切角，配合项目的几何字形。')]
variants = []
for key, name, desc in specs:
    folder = HERE / 'icons' / key
    folder.mkdir(parents=True, exist_ok=True)
    states = {state: icon(key, state) for state in ['normal', 'hover', 'pressed']}
    for state, svg in states.items():
        (folder / f'{state}.svg').write_text(svg, encoding='utf-8')
    variants.append(dict(key=key, name=name, desc=desc, response='轻微明暗反馈',
                         hotspot=[3, 2], states=states))

# Reuse the existing, verified review surface without executing its old asset generator.
source = HERE.parent / 'Cursor-20261112' / 'build.py'
tree = ast.parse(source.read_text(encoding='utf-8'))
template = next(ast.literal_eval(node.value) for node in tree.body
                if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'template' for t in node.targets))
template = template.replace('五个光标方案', '三个箭头方案').replace('光标方案', '箭头光标')
template = template.replace('<p>尖端负责点准，趣味放在后面。</p>', '')
template = template.replace('repeat(5,minmax(0,1fr))', 'repeat(3,minmax(0,1fr))')
template = template.replace('repeat(5,minmax(140px,1fr))', 'repeat(3,minmax(120px,1fr))')
template = template.replace('filter:drop-shadow(1px 2px 0 #74546122)', 'filter:none')
template = template.replace('A · 披风', 'A · 素白')
# No extra click rings: the cursor itself changes only by a small value shift.
template = template.replace("stage.appendChild(p);setTimeout(()=>p.remove(),350)", "")
screens = {
    'menu': ROOT / 'DraculasBox/Reviews/TitlePortrait-20261112/menu-preview.png',
    'level': ROOT / 'DraculasBox/Reviews/ChapterCandidates-20261111/Play/ch34-4-04-start.png',
    'editor': ROOT / 'DraculasBox/Reviews/EditorUsability-20261111/Campaign/editor-wine.png',
}
screens = {key: 'data:image/png;base64,' + base64.b64encode(p.read_bytes()).decode() for key, p in screens.items()}
html = template.replace('__VARIANTS__', json.dumps(variants, ensure_ascii=False)).replace('__SCREENS__', json.dumps(screens))
(HERE / '审核.html').write_text(html, encoding='utf-8')
(HERE / 'manifest.json').write_text(json.dumps({'status': 'review_only', 'logicalSize': 32,
    'variants': [{k: v[k] for k in ['key', 'name', 'desc', 'hotspot']} for v in variants]}, ensure_ascii=False, indent=2), encoding='utf-8')
(HERE / 'README.md').write_text('''# 箭头光标 · 克制版

[打开交互审核页](审核.html)。A 素白、B 细边、C 切角；均为标准箭头，三个状态仅作轻微明暗反馈，热点固定在 (3, 2)。

页面支持 24／32／40 px 和三种实际游戏背景，可移动、点击及试放箱子。所有操作仅发生在审核原型内。素材为透明 SVG 与 32／64 px PNG，尚未接入 Unity。
''', encoding='utf-8')
print(f'{HERE}: three arrow variants, nine SVGs; review-only.')
