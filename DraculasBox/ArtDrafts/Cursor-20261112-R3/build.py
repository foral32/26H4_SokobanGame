"""Four integrated arrow concepts for review; never changes Unity or OS cursors."""
from pathlib import Path
import ast
import base64
import json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
INK, FACE = '#30262e', '#fff2dd'

def path(d, fill, stroke='none', width=1.35, extra=''):
    return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round" {extra}/>'

def capsule(x, y, width, height, ink=INK):
    return f'<rect x="{x}" y="{y}" width="{width}" height="{height}" rx="{min(width,height)/2}" fill="{ink}"/>'

def icon(key, state):
    hover, pressed = state == 'hover', state == 'pressed'
    face = '#ebdcc5' if pressed else '#fff9ec' if hover else FACE
    parts = []
    if key == 'A':
        # The whole body is one folded surface; the red panel shares the fold from the tip.
        shape = 'M3 2 L24 18 L14.4 18 L18.8 26.9 L14.2 29.2 L9.7 20.2 L3 25 Z'
        wine = '#783449' if pressed else '#aa5268' if hover else '#924257'
        parts += [path(shape, face, INK),
                  path('M3.55 3.1 L13.1 17.65 L9.7 20.2 L3.55 23.9 Z', wine),
                  path('M13.1 17.65 L14.4 18 L18.8 26.9 L16.8 27.9 L11.7 18.7 Z', '#c4a88f'),
                  path('M3.55 3.1 L13.1 17.65 L11.7 18.7 Z', '#6a3045'),
                  path('M4.35 3.9 L21.9 17.1 L14.2 17.1 Z', '#fffdf4')]
    elif key == 'B':
        # A single-colored soft arrow. The light keyline is functional on dark surfaces.
        shape = 'M3 2 L23 17 Q24 17.8 22.8 18 L15.5 18 L19.2 25.6 Q19.6 26.5 18.8 26.9 L15.5 28.4 Q14.6 28.8 14.2 27.9 L10.1 19.8 L4 24.5 Q3 25.3 3 24 Z'
        wine = '#713247' if pressed else '#ac5267' if hover else '#914458'
        parts += [path(shape, wine, INK, 2.65), path(shape, wine, '#fff3dd', 1.15),
                  path('M5.1 6.2 L18.4 16 L14.3 16 L10.6 18 L5.1 21 Z', '#a65568' if not pressed else '#7b3a4f'),
                  path('M15.5 19 L18.4 25.8 L15.4 27.2 L11.5 19.3 Z', '#773548' if not hover else '#914257')]
    elif key == 'C':
        # Expression lives inside the arrow head; there is no attached mascot or extra silhouette.
        shape = 'M3 2 L23.8 18.5 L14.5 18.5 L18.6 27 L14.1 29.1 L9.8 20.4 L3 25 Z'
        parts += [path(shape, face, INK),
                  path('M14.5 18.5 L18.6 27 L16.9 27.8 L12.4 18.5 Z', '#cebaa1')]
        if pressed:
            parts += [capsule(7.05, 14.05, 2.1, .7), capsule(11.35, 14.05, 2.1, .7)]
        else:
            offset = .45 if hover else 0
            parts += [capsule(7.3+offset, 12.25, 1.55, 2.8), capsule(11.6+offset, 12.25, 1.55, 2.8)]
    else:
        # Negative space follows the arrow head, leaving the front tip solid and legible.
        outer = 'M3 2 L23.2 18 L14.5 18 L19.1 26.7 L14.4 29 L9.5 19.7 L3 25 Z'
        inner = 'M6.7 9.5 L15.6 15.6 L12 15.6 L6.7 20.6 Z'
        parts += [path(outer+' '+inner, face, INK, 1.25, 'fill-rule="evenodd"'),
                  path('M14.5 18 L19.1 26.7 L17.4 27.5 L12.3 18 Z', '#cfbca6')]
    return '<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">'+''.join(parts)+'</svg>'

specs = [
    ('A', '折面', '把整个箭头做成折面，奶白和酒红共用折线。'),
    ('B', '酒红', '完整的酒红箭头，用浅色轮廓保证辨识。'),
    ('C', '眨眼', '小表情放进箭头内部，按下时眨眼。'),
    ('D', '镂空', '中间留空，减少对棋盘内容的遮挡。'),
]
variants = []
for key, name, desc in specs:
    folder = HERE/'icons'/key
    folder.mkdir(parents=True, exist_ok=True)
    states = {s: icon(key,s) for s in ['normal','hover','pressed']}
    for state, svg in states.items():
        (folder/f'{state}.svg').write_text(svg,encoding='utf-8')
    variants.append(dict(key=key,name=name,desc=desc,hotspot=[3,2],states=states))

source = HERE.parent/'Cursor-20261112'/'build.py'
tree = ast.parse(source.read_text(encoding='utf-8'))
template = next(ast.literal_eval(n.value) for n in tree.body
    if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='template' for t in n.targets))
template = template.replace('五个光标方案','四个箭头方案').replace('光标方案','箭头新稿')
template = template.replace('<p>尖端负责点准，趣味放在后面。</p>','')
template = template.replace('repeat(5,minmax(0,1fr))','repeat(4,minmax(0,1fr))')
template = template.replace('repeat(5,minmax(140px,1fr))','repeat(4,minmax(150px,1fr))')
template = template.replace('filter:drop-shadow(1px 2px 0 #74546122)','filter:none')
template = template.replace('A · 披风','A · 折面')
template = template.replace('stage.appendChild(p);setTimeout(()=>p.remove(),350)','')
screens = {
    'menu': ROOT/'DraculasBox/Reviews/TitlePortrait-20261112/menu-preview.png',
    'level': ROOT/'DraculasBox/Reviews/ChapterCandidates-20261111/Play/ch34-4-04-start.png',
    'editor': ROOT/'DraculasBox/Reviews/EditorUsability-20261111/Campaign/editor-wine.png',
}
screens = {k:'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode() for k,p in screens.items()}
(HERE/'审核.html').write_text(template.replace('__VARIANTS__',json.dumps(variants,ensure_ascii=False)).replace('__SCREENS__',json.dumps(screens)),encoding='utf-8')
(HERE/'manifest.json').write_text(json.dumps({'status':'review_only','logicalSize':32,'variants':[
    {k:v[k] for k in ['key','name','desc','hotspot']} for v in variants]},ensure_ascii=False,indent=2),encoding='utf-8')
(HERE/'README.md').write_text('''# 箭头新稿

[交互审核页](审核.html)支持 A–D 切换、24／32／40 px 和三种实际游戏背景。可移动、悬停、按下，也可在编辑器背景试放箱子；操作仅发生在此原型内。

A 是整体折面；B 使用完整酒红主体和浅色轮廓；C 把两只小眼睛放进箭头内部，按下时闭眼；D 让箭头中间留空。每版热点均为 (3, 2)，三态不改变落点及外轮廓。

候选素材包含 SVG、32／64 px 透明 PNG。尚未接入 Unity。
''',encoding='utf-8')
print(f'{HERE}: four concepts, 12 SVG sources, review only.')
