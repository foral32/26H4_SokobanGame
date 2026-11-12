"""Review-only cursor variants. Writes no Unity assets, code, settings, or OS cursors."""
from pathlib import Path
import base64
import json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
INK, CREAM, LIGHT, SHADE, WINE, DARK, GOLD = (
    '#28212a', '#fff3dc', '#fffaf0', '#cbb7a4', '#923a52', '#5c293e', '#edca8f'
)

def poly(points, fill, stroke=INK, width=1.45):
    return f'<path d="{points}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round"/>'

def rect(x, y, w, h, fill, radius=0):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{fill}"/>'

def icon(key, state):
    hover, down = state == 'hover', state == 'pressed'
    cape = '#b65168' if hover else WINE
    face = '#f5dfbc' if down else CREAM
    bits = []
    arrow = 'M3 2 L23 18 L14.5 18 L19 27 L14 29.3 L9.5 20 L3 25 Z'
    if key == 'A':
        # A conventional arrow in front of a folded cape. Only the cape moves.
        wing = ('M12 12 L23 13 L20 20 L26 27 L17 28 L11 24 Z' if down else
                'M12 11 L27 12 L23 20 L30 27 L18 29 L10 24 Z')
        bits += [poly(wing, DARK), poly(wing, cape), poly(arrow, face)]
        bits += [poly('M5 5 L19.5 16 L13 16 Z', LIGHT, 'none'),
                 poly('M14.5 18 L19 27 L16.7 28 L12.1 18 Z', SHADE, 'none'),
                 poly('M24 19 L28 25 L21 23 Z', GOLD if hover else '#c66d7e', 'none')]
    elif key == 'B':
        # The pale arrow remains the sole aiming point; the darker wing sits behind it.
        wing = ('M11 13 L23 11 Q20 16 27 20 Q21 19 22 25 Q17 23 15 29 L10 22 Z' if down else
                'M10 12 L28 7 Q23 13 30.5 17 Q23.5 16 25 24 Q20 21 16 31 Q16 23 10 23 Z')
        bits += [poly(wing, cape), poly('M3 2 L20 15 L12 16 L15 27 L10 29 L7.5 19 L3 24 Z', face)]
        bits += [poly('M17 15 L24 11 Q21 15 27 17 Q21 17 23 22 Q19 21 16 27 Z', DARK, 'none'),
                 poly('M5 5 L16 13 L10.5 13 Z', LIGHT, 'none')]
        if hover:
            bits += [poly('M28 7 Q23 13 30 17', 'none', GOLD, .75)]
    elif key == 'C':
        # The little protagonist is behind the arrow, never in front of the hotspot.
        bits += [poly('M3 2 L19 15 L11.5 15 L9.5 23 L3 26 Z', face),
                 poly('M5 5 L15.5 13 L10 13 Z', LIGHT, 'none')]
        cloak = ('M15 19 L27 19 L29.5 30 L13 30 Z' if down else
                 'M12 19 L30 19 L27 23 L30.5 30 L12 30 L15 23 Z')
        bits += [poly(cloak, cape, INK, 1.2),
                 poly('M15 15 L27 15 L27 27 L15 27 Z', SHADE, INK, 1.2),
                 poly('M15 15 L27 15 L25.3 16.7 L16.7 16.7 L16.7 25.3 L15 27 Z', LIGHT, 'none'),
                 rect(16.7, 16.7, 8.6, 8.6, face)]
        if down:
            bits += [rect(18, 21.2, 1.8, .9, INK, .3), rect(22.1, 21.2, 1.8, .9, INK, .3)]
        else:
            bits += [rect(18.1, 20.1, 1.6, 3, INK, .8), rect(22.1, 20.1, 1.6, 3, INK, .8)]
        bits += [poly('M18 26 L20 26 L19 28 Z', LIGHT, 'none'),
                 poly('M22 26 L24 26 L23 28 Z', LIGHT, 'none')]
    elif key == 'D':
        # A small, lean arrow whose trailing edge becomes two fangs.
        shape = 'M3 2 L24 19 L17.2 19 L18.5 28 L13 24 L9 28 L8 19 L3 24 Z'
        bits += [poly(shape, face), poly('M5 5 L20 17 L14 17 Z', LIGHT, 'none'),
                 poly('M14 18 L21 18 L17.2 19 L18.5 28 L14.4 24.9 Z', SHADE, 'none'),
                 poly('M8 18 L17 18 L16.2 22 L13 20.7 L10 23 L9.5 19 Z', cape, 'none'),
                 poly('M10.3 20.1 L12.6 20.8 L11 24 Z', LIGHT, 'none'),
                 poly('M14 20.8 L16.5 20 L16.2 24.5 Z', LIGHT, 'none')]
        if hover or down:
            bits += [poly('M4.7 6 L4.7 20', 'none', GOLD if hover else WINE, 1.2)]
    elif key == 'E':
        # A familiar pointing glove. The index tip is fixed across every pose.
        hand = ('M10 16 V5 Q10 2 12.5 2 Q15 2 15 5 V13 Q17 11 19 13 Q21 11.7 23 14 Q25 12.8 27 16 V21 Q27 25 24 27 V29 H12 V26 L5 20 Q3.6 17.9 5.2 16.7 Q6.8 15.7 8.1 17.2 L10 19 Z' if down else
                'M10 15 V5 Q10 2 12.5 2 Q15 2 15 5 V12 Q17 10 19 12 Q21 10.7 23 13 Q25 11.8 27 15 V21 Q27 25 24 27 V29 H12 V26 L5 19 Q3.6 16.9 5.2 15.7 Q6.8 14.7 8.1 16.2 L10 18 Z')
        bits += [poly(hand, face), poly('M24 15 L26 17 L26 21 L22 26 L15 26 L13 24 L22 24 L24 20 Z', SHADE, 'none'),
                 poly('M19 13 L19 18 M23 14 L23 19 M15 14 L15 18', 'none', SHADE, .9),
                 poly('M10 26 L25 26 L25 31 L10 31 Z', cape, INK, 1.1),
                 poly('M11 27 L24 27', 'none', GOLD if hover else '#c67887', .8),
                 poly('M14 27.5 L16 27.5 L15 30 Z', LIGHT, 'none'),
                 poly('M19 27.5 L21 27.5 L20 30 Z', LIGHT, 'none')]
    return '<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">' + ''.join(bits) + '</svg>'

specs = [
    ('A', '披风', '标准箭头，尾部折成小披风。', '折起披风'),
    ('B', '蝠翼', '翅膀放在后方，保留明确的浅色尖端。', '收拢翅膀'),
    ('C', '小德', '主角藏在箭头后面，按下时眨眼。', '眨一下眼'),
    ('D', '獠牙', '更紧凑的牙形，细节最少。', '牙根微暗'),
    ('E', '手套', '清楚的指尖，酒红袖口点题。', '手掌轻收'),
]
variants = []
for key, name, desc, response in specs:
    folder = HERE / 'icons' / key
    folder.mkdir(parents=True, exist_ok=True)
    states = {state: icon(key, state) for state in ['normal', 'hover', 'pressed']}
    for state, svg in states.items():
        (folder / f'{state}.svg').write_text(svg, encoding='utf-8')
    variants.append(dict(key=key, name=name, desc=desc, response=response,
                         hotspot=[12, 2] if key == 'E' else [3, 2], states=states))

screens = {
    'menu': ROOT / 'DraculasBox/Reviews/TitlePortrait-20261112/menu-preview.png',
    'level': ROOT / 'DraculasBox/Reviews/ChapterCandidates-20261111/Play/ch34-4-04-start.png',
    'editor': ROOT / 'DraculasBox/Reviews/EditorUsability-20261111/Campaign/editor-wine.png',
}
screens = {key: 'data:image/png;base64,' + base64.b64encode(p.read_bytes()).decode()
           for key, p in screens.items()}

template = r'''<!doctype html>
<html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Dracula’s Box · 光标方案</title>
<style>
*{box-sizing:border-box}body{margin:0;color:#502f3e;background:#eee1cf;font-family:"Microsoft YaHei","Noto Sans SC",sans-serif}button{font:inherit;color:inherit;cursor:pointer}button:focus-visible{outline:2px solid #a23b56;outline-offset:4px}header{display:flex;align-items:end;justify-content:space-between;margin:0 0 35px}h1{font-size:32px;margin:0;font-weight:650;letter-spacing:.04em}header p{font-size:14px;margin:8px 0 0;color:#896a6e}.brand{font-size:18px;font-weight:650;color:#71515d}.page{max-width:1540px;margin:auto;padding:40px 40px 130px}.concepts{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));border-top:1px solid #c5a18f;border-bottom:1px solid #c5a18f}.concept{padding:23px 23px 24px;position:relative;border:0;border-right:1px solid #ceb4a3;text-align:left;background:transparent;cursor:pointer;min-width:0}.concept:last-child{border-right:0}.concept.selected{background:#faeedb}.concept.selected:after{content:"";position:absolute;bottom:-1px;height:3px;left:22px;right:22px;background:#923a52}.name{font-size:22px;display:flex;gap:12px;align-items:baseline}.letter{font-weight:700;color:#a57880;font-size:15px}.zoom{height:164px;display:flex;align-items:center;justify-content:center}.zoom svg{width:128px;height:128px;filter:drop-shadow(1px 2px 0 #74546122)}.poses{display:grid;grid-template-columns:repeat(3,1fr);gap:5px;margin:4px 0 22px}.pose{display:grid;justify-items:center;gap:8px;color:#84606b;font-size:11px}.pose svg{height:36px;width:36px}.desc{height:47px;font-size:12px;line-height:1.8;margin:0;color:#735360}.native-title{display:flex;justify-content:space-between;font-size:11px;color:#8a6a72;margin:16px 0 10px}.native{display:grid;grid-template-columns:repeat(4,1fr);height:53px;gap:4px}.native div{display:flex;align-items:center;justify-content:center}.native svg{width:32px;height:32px}.native div:nth-child(1){background:#403139}.native div:nth-child(2){background:#fff7e7}.native div:nth-child(3){background:#c9bda7}.native div:nth-child(4){background:#153766}.rule{display:flex;gap:28px;justify-content:space-between;font-size:12px;color:#84606b;margin:20px 0 0;line-height:1.8}.rule span:last-child{text-align:right}.try{margin:46px auto 0;max-width:1200px}.toolbar{display:flex;align-items:center;justify-content:space-between;margin-bottom:15px;gap:15px}.toolbar strong{font-size:18px}.tabs{display:flex;gap:3px}.tabs button{border:0;background:transparent;padding:8px 13px;color:#8d6873;font-size:13px}.tabs button.active{background:#923a52;color:#fff3dc}.controls{display:flex;gap:8px;align-items:center;font-size:12px}.controls select{background:#faeedb;border:1px solid #c3a08e;color:#612c3f;padding:6px}.stage{position:relative;width:100%;aspect-ratio:16/9;background:#403139;overflow:hidden;cursor:none;touch-action:none}.stage *{cursor:none!important}.stage>img.scene{display:block;width:100%;height:100%;pointer-events:none}.hit{position:absolute;border:0;background:transparent;padding:0}.hit:hover{box-shadow:inset 0 0 0 1px #edca8f77;background:#fff4dc0a}.hit:active{background:#edca8f1c}.pointer{position:absolute;width:32px;height:32px;pointer-events:none;z-index:10;display:none;will-change:left,top}.pulse{position:absolute;border:1px solid #edca8f;transform:translate(-50%,-50%);border-radius:50%;width:9px;height:9px;pointer-events:none;animation:ping .33s ease-out forwards;z-index:8}@keyframes ping{to{opacity:0;width:28px;height:28px}}.stamp{position:absolute;width:5.6%;aspect-ratio:1;background:#fff7e7;border:5px solid #cfb9a6;box-shadow:inset 3px 3px #fffaf0,2px 2px #251e2566;pointer-events:none}.stamp:after{content:"";position:absolute;inset:20%;border:2px solid #cbb7a4;background:#fff3dc}.foot{display:flex;justify-content:space-between;gap:15px;font-size:12px;color:#8b6773;margin-top:11px;min-height:22px}.feedback{color:#923a52;font-weight:600}.precision{display:flex;gap:12px;align-items:center}.target{border:1px solid #ac7a85;background:#f8eddc;color:#612c3f;padding:8px 13px}.target.passed{background:#923a52;color:#fff3dc}.switcher{position:fixed;z-index:50;bottom:22px;left:50%;transform:translateX(-50%);display:flex;align-items:center;gap:18px;background:#403139;color:#fff3dc;box-shadow:0 4px 16px #251e2525;padding:8px 12px}.switcher button{background:transparent;color:#edca8f;border:0;padding:6px 15px;font-size:21px}.switcher output{min-width:105px;text-align:center;font-size:14px}.shortcuts{font-size:11px;color:#8a6a72;margin:11px 0 0;text-align:center}.sheet .page{padding:38px 42px 30px;max-width:none}.sheet header{margin-bottom:28px}.sheet .try,.sheet .switcher,.sheet .shortcuts{display:none}.sheet .zoom{height:188px}.sheet .zoom svg{width:144px;height:144px}.sheet .concept{padding:24px 25px 28px}.sheet .rule{margin-top:22px}.sheet .page{padding-bottom:38px}.sheet .concept{cursor:default}.sheet .concept.selected{background:transparent}.sheet .concept.selected:after{display:none}.sheet .native{height:61px}.sheet .desc{height:50px;font-size:13px}.sheet h1{font-size:34px}
@media(max-width:1050px){.page{padding:28px 20px 110px}.concepts{grid-template-columns:repeat(5,minmax(140px,1fr));overflow-x:auto}.concept{padding:18px 12px}.zoom{height:130px}.zoom svg{width:100px;height:100px}.native{grid-template-columns:repeat(2,1fr);height:91px}.desc{height:67px}.toolbar{flex-wrap:wrap}.brand{font-size:14px}.rule{gap:10px}.foot{flex-wrap:wrap}}@media(prefers-reduced-motion:reduce){.pulse{animation:none;opacity:0}}
</style>
<body><main class="page"><header><div><h1>光标方案</h1><p>尖端负责点准，趣味放在后面。</p></div><div class="brand">Dracula’s Box</div></header><section class="concepts" id="concepts" aria-label="五个光标方案"></section><div class="rule"><span>32 px 实际尺寸 · 深浅背景对照</span><span>普通／悬停／按下，落点保持不动</span></div><section class="try"><div class="toolbar"><strong id="try-title">实际试用</strong><div class="tabs" aria-label="预览场景"><button data-mode="menu" class="active">主菜单</button><button data-mode="level">棋盘</button><button data-mode="editor">编辑器</button></div><div class="controls"><label for="size">光标大小</label><select id="size"><option value="24">24 px</option><option value="32" selected>32 px</option><option value="40">40 px</option></select></div></div><div id="stage" class="stage"><img class="scene" id="scene" alt="游戏界面，用于光标审核"><div id="hits"></div><img id="pointer" class="pointer" alt=""></div><div class="foot"><span id="instruction">把鼠标移入画面，试着点按钮。</span><span id="feedback" class="feedback" aria-live="polite"></span></div><div class="shortcuts">此页只作审核，不修改游戏或关卡。</div></section></main><nav class="switcher" aria-label="切换光标方案"><button id="prev" aria-label="上一个方案">←</button><output id="current">A · 披风</output><button id="next" aria-label="下一个方案">→</button></nav>
<script>
const variants=__VARIANTS__,screens=__SCREENS__;
const params=new URLSearchParams(location.search);if(params.has('sheet'))document.body.classList.add('sheet');
const $=s=>document.querySelector(s);const data=svg=>'data:image/svg+xml;charset=utf-8,'+encodeURIComponent(svg);
window.CURSOR_DRAFTS=variants;window.review={variant:'A',mode:'menu',state:'normal',size:32,hitCount:0};
const poseName={normal:'普通',hover:'悬停',pressed:'按下'};
for(const v of variants){const b=document.createElement('button');b.className='concept';b.dataset.key=v.key;b.setAttribute('aria-label',v.key+' '+v.name);b.innerHTML=`<div class="name"><span class="letter">${v.key}</span>${v.name}</div><div class="zoom">${v.states.normal}</div><div class="poses">${Object.keys(poseName).map(s=>`<div class="pose">${v.states[s]}<span>${poseName[s]}</span></div>`).join('')}</div><p class="desc">${v.desc}</p><div class="native-title"><span>实际尺寸</span><span>32 px</span></div><div class="native">${[1,2,3,4].map(()=>`<div>${v.states.normal}</div>`).join('')}</div>`;b.onclick=()=>select(v.key);$('#concepts').appendChild(b)}
let selected=0,mode='menu',size=32,position=null,hover=false,down=false;
function select(key){selected=variants.findIndex(v=>v.key===key);if(selected<0)selected=0;const v=variants[selected];window.review.variant=v.key;for(const b of document.querySelectorAll('.concept')){b.classList.toggle('selected',b.dataset.key===v.key);b.setAttribute('aria-pressed',b.dataset.key===v.key)}$('#current').value=v.key+' · '+v.name;const url=new URL(location.href);url.searchParams.set('variant',v.key);history.replaceState(null,'',url);renderPointer()}
function cycle(step){select(variants[(selected+step+variants.length)%variants.length].key)}
$('#prev').onclick=()=>cycle(-1);$('#next').onclick=()=>cycle(1);
document.addEventListener('keydown',e=>{if(e.target.matches('input,textarea,select,[contenteditable]'))return;if(e.key==='ArrowLeft'||e.key==='ArrowRight'){e.preventDefault();cycle(e.key==='ArrowLeft'?-1:1)}});
function renderPointer(){const p=$('#pointer'),v=variants[selected],state=down?'pressed':hover?'hover':'normal';p.src=data(v.states[state]);p.style.width=p.style.height=size+'px';window.review.state=state;window.review.size=size;if(position){p.style.display='block';p.style.left=(position.x-v.hotspot[0]*size/32)+'px';p.style.top=(position.y-v.hotspot[1]*size/32)+'px'}else p.style.display='none'}
function hit(x,y,w,h,label){const b=document.createElement('button');b.className='hit';b.style.cssText=`left:${x/12.8}%;top:${y/7.2}%;width:${w/12.8}%;height:${h/7.2}%`;b.setAttribute('aria-label',label);b.onpointerenter=()=>{hover=true;renderPointer()};b.onpointerleave=()=>{hover=false;renderPointer()};b.onclick=e=>{window.review.hitCount++;$('#feedback').textContent='命中：'+label;b.animate([{background:'#edca8f33'},{background:'transparent'}],{duration:260});};$('#hits').appendChild(b)}
function scene(key){mode=key;window.review.mode=key;$('#scene').src=screens[key];$('#hits').innerHTML='';for(const e of document.querySelectorAll('.stamp,.pulse'))e.remove();hover=down=false;for(const b of document.querySelectorAll('[data-mode]'))b.classList.toggle('active',b.dataset.mode===key);$('#feedback').textContent='';if(key==='menu'){[['开始游戏',416],['关卡创作',472],['设置',528],['退出',584]].forEach(([t,y])=>hit(485,y,310,54,t));hit(1010,638,240,64,'编辑正式关卡');$('#instruction').textContent='把鼠标移入画面，试着点按钮。'}else if(key==='editor'){hit(1060,25,180,50,'试玩');hit(835,25,185,50,'保存');hit(1090,85,150,44,'场景设置');$('#instruction').textContent='点棋盘试放箱子，检查落点是否清楚。'}else{hit(1120,185,100,95,'撤销');hit(1120,297,100,95,'重置');hit(1120,410,100,95,'暂停');$('#instruction').textContent='在亮地板、阴影和墙面之间移动，检查辨识度。'}renderPointer()}
for(const b of document.querySelectorAll('[data-mode]'))b.onclick=()=>scene(b.dataset.mode);
$('#size').onchange=e=>{size=+e.target.value;renderPointer()};
const stage=$('#stage');stage.addEventListener('pointermove',e=>{const r=stage.getBoundingClientRect();position={x:e.clientX-r.left,y:e.clientY-r.top};renderPointer()});stage.addEventListener('pointerleave',()=>{position=null;hover=down=false;renderPointer()});stage.addEventListener('pointerdown',e=>{down=true;const r=stage.getBoundingClientRect();position={x:e.clientX-r.left,y:e.clientY-r.top};renderPointer();const p=document.createElement('span');p.className='pulse';p.style.left=position.x+'px';p.style.top=position.y+'px';stage.appendChild(p);setTimeout(()=>p.remove(),350)});window.addEventListener('pointerup',()=>{down=false;renderPointer()});
stage.addEventListener('click',e=>{if(mode!=='editor'||e.target.closest('.hit'))return;const r=stage.getBoundingClientRect(),x=(e.clientX-r.left)/r.width*1280,y=(e.clientY-r.top)/r.height*720;if(x<375||x>1035||y<204||y>570)return;const col=Math.min(8,Math.floor((x-375)/73.3333)),row=Math.min(4,Math.floor((y-204)/73.2));const p=document.createElement('div');p.className='stamp';p.style.left=(375+col*73.3333)/12.8+'%';p.style.top=(204+row*73.2)/7.2+'%';stage.appendChild(p);window.review.hitCount++;$('#feedback').textContent='已试放一只箱子'});
select(params.get('variant')||'A');scene('menu');
window.exportCursorPNGs=async()=>{const results=[];for(const v of variants)for(const [state,svg] of Object.entries(v.states))for(const n of [32,64]){const im=new Image();im.src=data(svg);await im.decode();const c=document.createElement('canvas');c.width=c.height=n;const context=c.getContext('2d');context.drawImage(im,0,0,n,n);results.push({key:v.key,state,size:n,base64:c.toDataURL('image/png').split(',')[1]})}return results};
</script></body></html>'''
html = template.replace('__VARIANTS__', json.dumps(variants, ensure_ascii=False)).replace('__SCREENS__', json.dumps(screens))
(HERE / '审核.html').write_text(html, encoding='utf-8')
manifest = {'status': 'review_only', 'logicalSize': 32,
            'variants': [{k: v[k] for k in ['key', 'name', 'desc', 'response', 'hotspot']} for v in variants],
            'notes': ['Hotspots are top-left coordinates.', 'All states keep the same hotspot.',
                      'No Unity assets, scripts, settings, or system cursors are changed.']}
(HERE / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
(HERE / 'README.md').write_text('''# 光标审核稿

打开 [审核.html](审核.html)，点击 A–E 或用左右键切换。预览区域可实际移动、悬停、按下；编辑器画面可试放箱子。所有操作仅发生在原型中。

每版包含普通、悬停、按下三态，SVG 为 32×32 逻辑画布，另导出 32／64 像素透明 PNG。热点位于尖端或指尖，同版三个状态保持固定。64 像素版本使用两倍热点坐标。

| 方案 | 特点 | 取舍 |
|---|---|---|
| A 披风 | 标准箭头后方加披风 | 最适合全局使用，趣味较克制 |
| B 蝠翼 | 后方蝙蝠翅膀收放 | 轮廓更活泼，右侧占用略大 |
| C 小德 | 小主角附在箭头后方，按下眨眼 | 角色辨识度最高，24 px 时细节较小 |
| D 獠牙 | 紧凑牙形箭头 | 最省遮挡，题材表达较轻 |
| E 手套 | 明确的指尖和酒红袖口 | 更适合按钮悬停态；全局使用需接受手形 |

这些是候选素材与交互预览，尚未接入 Unity。浏览器原型不证明 Unity 硬件光标、DPI 或真实游戏输入已验证。
''', encoding='utf-8')
print(f'{HERE}: 5 variants, 15 SVGs, standalone preview ready.')
