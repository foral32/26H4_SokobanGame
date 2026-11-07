"""Build the atlas from local reference evidence. Does not change Unity assets."""
from pathlib import Path
import json, shutil, wave, array, subprocess, urllib.request, re, hashlib
from PIL import Image
from motion_sources import motion_sources

APP=Path(__file__).resolve().parents[1]
STUDY=APP.parent
PROJECT=STUDY.parents[1]
ASSETS=PROJECT/'Assets/Studies/BoxOrVoid'
MEDIA=APP/'public/media'
FFMPEG=Path(r'C:\Users\19118\.codex\tmp\sokoban-video-tools\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v9.0.2.exe')
for folder in ('sprites','audio','images','fonts'):
    (MEDIA/folder).mkdir(parents=True,exist_ok=True)
catalog=json.loads((ASSETS/'Content/Catalog.json').read_text(encoding='utf-8'))
doc=(STUDY/'Research/LevelAnalysis.md').read_text(encoding='utf-8')
analysis={}
for line in doc.splitlines():
    if line.startswith('|') and '/ n_' in line:
        fields=line.strip('|').split('|')
        analysis[fields[0].split('/')[1].strip()]={'lesson':fields[1].strip(),'evidence':fields[2].strip()}
sprite_meta={}
settings=json.loads((ASSETS/'Content/SpriteImport.json').read_text())['sprites']
for setting in settings:
    path=ASSETS/'Art/Sprites'/f'{setting["name"]}.png'
    shutil.copy2(path,MEDIA/'sprites'/path.name)
    with Image.open(path) as im:
        sprite_meta[setting['name']]={**setting,'w':im.width/setting['pixelsPerUnit'],'h':im.height/setting['pixelsPerUnit']}
order=[f'n_tut_{i}' for i in range(1,9)]+[f'n_box_{i}' for i in [1,2,13,5,4,6,3,7,8,9,10,11,12]]
levels=[]
for id in order:
    entry=next(x for x in catalog['levels'] if x['id']==id)
    data=json.loads((ASSETS/f'Content/Levels/{id}.json').read_text())
    trace=json.loads((STUDY/f'Evidence/Traces/{id}.json').read_text())
    counts={'boxes':0,'playerGoals':0,'boxGoals':0}
    for cell in data['grid']:
        for obj in cell.get('Objects',[]):
            for k,t in [('boxes',2),('playerGoals',4),('boxGoals',3)]:
                if obj['ObjectType']==t: counts[k]+=1
    levels.append({**entry,**analysis[id],'definition':data,'trace':trace['steps'],'commands':trace['commands'],**counts,
                   'switches':trace['commands'].count('S'),'steps':len(trace['commands'])})

selected=['playerMove','playerMoveBlocked','playerSwitch','boxPush','goalFulfilled','undo','reset','levelVanishTick','uiNavigate','uiSubmit','selectionMove','selectionMoveBlocked','selectionSelect','pauseOpen','theme','boxWorld']
sounds=[]
for key in selected:
    clip=next(x['clip'] for x in catalog['audio'] if x['key']==key)
    source=ASSETS/'Audio'/f'{clip}.wav'
    target=MEDIA/'audio'/f'{key}.mp3'
    if not target.exists():
        subprocess.run([str(FFMPEG),'-hide_banner','-loglevel','error','-i',str(source),'-codec:a','libmp3lame','-q:a','4','-y',str(target)],check=True)
    with wave.open(str(source),'rb') as wav:
        rate=wav.getframerate(); channels=wav.getnchannels(); frames=wav.getnframes(); width=wav.getsampwidth()
        raw=wav.readframes(frames)
    assert width==2, source
    samples=array.array('h',raw)
    hop=max(1,len(samples)//72)
    peaks=[round(max(map(abs,samples[i:i+hop]),default=0)/32768,4) for i in range(0,len(samples),hop)][:72]
    sounds.append({'key':key,'clip':clip,'duration':round(frames/rate,3),'channels':channels,'rate':rate,'peaks':peaks,'path':f'/media/audio/{key}.mp3'})

images={
    'hero':STUDY/'Evidence/WindowsRun03/n_box_6_solved.png',
    'board':STUDY/'Evidence/WindowsRun03/n_box_6_initial.png',
    'title':STUDY/'Evidence/WindowsRun03/title.png',
    'selection':STUDY/'Evidence/WindowsRun03/selection_completed.png',
    'stack':STUDY/'Evidence/Runtime/triple_stack_after.png',
    'reference':STUDY/'Research/Frames/frame_0104.png',
    'replica':STUDY/'Evidence/Runtime/reference_tut2_0104.png',
}
for key,path in images.items():
    with Image.open(path) as image:
        image.convert('RGB').save(MEDIA/'images'/f'{key}.webp',quality=89)
shutil.copy2(STUDY/'Evidence/WindowsRun03/Box06_gameplay.mp4',MEDIA/'gameplay.mp4')
shutil.copy2(STUDY/'Evidence/Presentation/Transitions.mp4',MEDIA/'transitions.mp4')
for key,name in [('motion-title','title.png'),('motion-chapters','chapters.png'),('motion-board','n_box_6_initial.png')]:
    with Image.open(STUDY/'Evidence/Presentation/WindowsRun04'/name) as image:
        image.convert('RGB').save(MEDIA/'images'/f'{key}.webp',quality=89)

sources=[]
for label,rel,scope in [
    ('工作台说明','Atlas/README.md','工作台用法与验证边界'),
    ('动效拆解','Atlas/动效拆解.md','当前复刻工程的动画实现与演示边界'),
    ('规则取证','Research/Rules.md','原作事实与实现核对'),
    ('逐关分析','Research/LevelAnalysis.md','TD / LD 分析'),
    ('视频状态对照','Research/ReferenceComparison.md','六个可观察状态'),
    ('交付复盘','Review.md','复刻实现与边界'),
]:
    sources.append({'name':label,'path':f'Studies/BoxOrVoid/{rel}','scope':scope,'text':(STUDY/rel).read_text(encoding='utf-8')})
code=[]
for rel in ['Core/PuzzleSession.cs','Core/PuzzleState.cs','Core/PuzzleDefinition.cs','Runtime/PuzzleApp.cs','Runtime/StudyMenu.cs','Runtime/StudyTransition.cs','Runtime/StudyIrisGraphic.cs','Runtime/StudyBackdrop.cs','Runtime/PuzzleHud.cs','Runtime/BoardView.cs','Runtime/HistoryActionQueue.cs','Runtime/StudyInput.cs','Runtime/PuzzleAudio.cs','Runtime/StudyAssets.cs','Runtime/ProgressStore.cs']:
    code.append({'name':Path(rel).name,'path':f'Assets/Studies/BoxOrVoid/{rel}','text':(ASSETS/rel).read_text(encoding='utf-8')})
for rel in ['NegativeSpace.Audio/SfxPlayer.cs','NegativeSpace.Audio/UiSfxPlayer.cs']:
    path=STUDY/'Research/Decompiled'/rel
    sources.append({'name':Path(rel).stem+' 原作取证','path':str(path.relative_to(PROJECT)),'scope':'原作反编译，只用于本地学习','text':path.read_text(encoding='utf-8')})

try:
    if not all((MEDIA/'fonts'/f'satoshi-{w}.woff2').exists() for w in (400,500,700)):
        css=urllib.request.urlopen('https://api.fontshare.com/v2/css?f[]=satoshi@400,500,700&display=swap',timeout=15).read().decode()
        local_faces=[]
        for block in re.findall(r'@font-face\s*\{([^}]+)\}',css):
            url=re.search(r"url\(['\"]?([^'\"()]+\.woff2)['\"]?\)",block).group(1)
            weight=re.search(r'font-weight:\s*(\d+)',block).group(1)
            path=MEDIA/'fonts'/f'satoshi-{weight}.woff2'
            if not path.exists(): subprocess.run(['curl.exe','--fail','-L','--connect-timeout','5','--max-time','20','--silent','--show-error','https:'+url if url.startswith('//') else url,'-o',str(path)],check=True)
            local_faces.append("@font-face{font-family:'Satoshi';src:url('/media/fonts/"+path.name+"') format('woff2');font-weight:"+weight+";font-style:normal;font-display:swap;}")
        (MEDIA/'fonts/satoshi.css').write_text('\n'.join(local_faces),encoding='utf-8')
    else:
        (MEDIA/'fonts/satoshi.css').write_text('\n'.join("@font-face{font-family:'Satoshi';src:url('/media/fonts/satoshi-"+str(w)+".woff2') format('woff2');font-weight:"+str(w)+";font-style:normal;font-display:swap;}" for w in (400,500,700)),encoding='utf-8')
except Exception as error:
    print('Font source unavailable; system fallbacks remain readable:',type(error).__name__)

result={'levels':levels,'themes':catalog['themes'],'spriteMeta':sprite_meta,'sounds':sounds,'sources':sources,'navigation':catalog['navigation'],'motion':motion_sources(ASSETS),
        'provenance':{'game':'Box or Void Demo','referenceUnity':'6000.0.47f1','studyUnity':'6000.3.6f1','scope':'欢迎 8 关 / 箱即是空 13 关','source':'本地 Demo 关卡与素材、程序集取证、用户提供的实机录像'}}
(APP/'src/atlas-data.json').write_text(json.dumps(result,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
(APP/'src/atlas-code.json').write_text(json.dumps(code,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print(json.dumps({'levels':len(levels),'sounds':len(sounds),'sprites':len(sprite_meta),'dataBytes':(APP/'src/atlas-data.json').stat().st_size},ensure_ascii=False))
