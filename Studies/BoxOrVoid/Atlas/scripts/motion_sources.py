"""Read motion parameters from the study's current C# and serialized scene."""
import re, hashlib

def motion_sources(assets):
    names=['BoardView','StudyMenu','StudyTransition','StudyIrisGraphic','StudyBackdrop','PuzzleApp']
    code={name:(assets/f'Runtime/{name}.cs').read_text(encoding='utf-8') for name in names}
    def numbers(name,pattern):
        match=re.search(pattern,code[name])
        if not match: raise ValueError(f'Motion source changed: {name}: {pattern}')
        return [float(value) for value in match.groups()]
    def one(name,pattern):return numbers(name,pattern)[0]
    blocked,move=numbers('BoardView',r'lastKind==ActionKind.Blocked\?([.\d]+)f:([.\d]+)f')
    frequency,shake=numbers('BoardView',r'Mathf.Sin\(t\*Mathf.PI\*([.\d]+)\)\*([.\d]+)f')
    press_time,press=numbers('StudyMenu',r'Time.unscaledTime-item.pressedAt<([.\d]+)f\) target=([.\d]+)f')
    hold,vanish=numbers('PuzzleApp',r'board.SetCompletion\(Mathf.Clamp01\(\(t-([.\d]+)f\)/([.\d]+)f\)\)')
    scene=(assets/'Scenes/BoxOrVoid.unity').read_text(encoding='utf-8')
    def scene_value(name):
        match=re.search(r'^  '+name+r': ([.\d]+)$',scene,re.M)
        if not match: raise ValueError('Missing scene motion field: '+name)
        return float(match[1])
    return {
        'moveDuration':one('BoardView',r'duration=.*?\?\s*([.\d]+)f'),
        'moveAmplitude':move,'blockedAmplitude':blocked,'shakeAmplitude':shake,'shakePiMultiplier':frequency,
        'markerDuration':one('BoardView',r'elapsed-duration\)/([.\d]+)f'),
        'markerMinScale':one('BoardView',r'Mathf.Lerp\(([.\d]+)f,1,amount\)'),
        'backgroundFade':one('StudyBackdrop',r'fade\+Time.unscaledDeltaTime/([.\d]+)f'),
        'menuTau':one('StudyMenu',r'Mathf.Exp\(-Time.unscaledDeltaTime/([.\d]+)f\)'),
        'menuHover':one('StudyMenu',r'i==selected\?([.\d]+)f:1'),
        'menuPressDuration':press_time,'menuPressScale':press,
        'panelDuration':one('StudyMenu',r'panelOpenedAt\)/([.\d]+)f'),
        'panelMinScale':one('StudyMenu',r'Mathf.Lerp\(([.\d]+)f,1,1-Mathf.Pow'),
        'closeDuration':scene_value('closeSeconds'),'openDuration':scene_value('openSeconds'),
        'irisMax':one('StudyIrisGraphic',r'Mathf.Clamp\(value,0,([.\d]+)f\)'),
        'irisSegments':one('StudyIrisGraphic',r'const int segments=(\d+)'),
        'irisFeather':one('StudyIrisGraphic',r'float feather=([.\d]+)f/'),
        'completionHold':hold,'completionVanish':vanish,
        'completionReturn':one('PuzzleApp',r'if\(t>=([.\d]+)\) ShowLevels'),
        'completionTick':one('PuzzleApp',r'nextVanishSound=Time.unscaledTime\+([.\d]+)f'),
        'particleCount':one('StudyBackdrop',r'new SpriteRenderer\[(\d+)\]'),
        'particleSpeed':one('StudyBackdrop',r'velocity\[i\]=new Vector2\(Mathf.Cos\(phases\[i\]\),Mathf.Sin\(phases\[i\]\)\)\*([.\d]+)f'),
        'particlePhaseSpeed':one('StudyBackdrop',r'phase=Time.unscaledTime\*([.\d]+)f'),
        'sourceHashes':{name+'.cs':hashlib.sha256(text.encode()).hexdigest() for name,text in code.items()},
        'scope':'当前 Unity 复刻工程；公式演示，不是实时 Unity 调试器',
        'referenceVideo':'Studies/BoxOrVoid/Evidence/Presentation/Transitions.mp4'
    }
