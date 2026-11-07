import React,{useEffect,useMemo,useRef,useState} from 'react';
import {Play,Pause,RotateCcw,ChevronLeft,ChevronRight,SlidersHorizontal} from 'lucide-react';
import DATA from './data';
import {SourceExcerpt} from './SourceExcerpt';
import {MotionBoard,MotionBackground,MenuMotionPreview,IrisMotionPreview} from './MotionPreview';
import {motionEffects,motionGroups,motionContext,motionDuration,motionMilestones,sampleMotion} from './motion-content';
import {smoothingBlend,completionTicks} from './motion-model';

const source=DATA.motion;
const defaults={squash:true,fade:true,guides:false,mesh:false,zoom:false};
const fmt=(value,unit='')=>(Math.abs(value)<.00001?0:value).toFixed(3)+unit;
const timestamp=s=>s>=10?s.toFixed(1)+' s':Math.round(s*1000)+' ms';
const groupFirst=group=>motionEffects.find(effect=>effect.group===group);
function clockFormula(effect,track,time,p,fps,sample){
  const ms=n=>(n*1000).toFixed(Math.abs(n*1000-Math.round(n*1000))<.01?0:1);
  const norm=(n,d)=>Math.max(0,Math.min(1,n/d)).toFixed(3);
  if(track.key==='markerAlpha'||track.key==='markerScale')return `α = clamp((${ms(time)} − ${ms(p.moveDuration)}) / ${ms(p.markerDuration)})`;
  if(track.key==='backgroundAlpha')return `fade = clamp(${ms(time)} / ${ms(p.backgroundFade)})`;
  if(effect.group==='board')return `t = clamp(${ms(time)} / ${ms(p.moveDuration)}) = ${norm(time,p.moveDuration)}`;
  if(effect.group==='menu'&&effect.id==='panel')return `t = clamp(${ms(time)} / ${ms(p.panelDuration)}) = ${norm(time,p.panelDuration)}`;
  if(effect.group==='menu')return `Δt = ${ms(1/fps)}ms  ·  τ = ${ms(p.menuTau)}ms  ·  blend = ${smoothingBlend(1/fps,p.menuTau).toFixed(3)}`;
  if(effect.group==='iris'){
    if(sample.mesh.phase==='Covered')return `Aperture = 0  ·  等待下一帧再换页`;
    if(track.key==='page')return `frame = ${Math.floor(time*fps+1e-8)}  ·  coveredFrame = ${Math.ceil(p.closeDuration*fps-1e-8)}`;
    const elapsed=sample.mesh.phase==='Closing'?time:time-sample.mesh.openAt,span=sample.mesh.phase==='Closing'?p.closeDuration:p.openDuration;
    return `t = clamp(${ms(Math.max(0,elapsed))} / ${ms(span)}) = ${norm(elapsed,span)}`;
  }
  if(effect.group==='complete')return `fraction = clamp((${ms(time)} − ${ms(p.completionHold)}) / ${ms(p.completionVanish)})`;
  return `P0: φ = ${time.toFixed(1)} × ${p.particlePhaseSpeed} + 0`;
}

function CurveTrack({track,effect,context,params,fps,options,time,duration,sample,selected,onSelect}){
  const chart=useMemo(()=>{
    const samples=Array.from({length:97},(_,i)=>sampleMotion(effect,duration*i/96,context,params,fps,options).values[track.key]);
    let [min,max]=track.range;
    if(track.key==='distance')max=Math.max(max,...samples);
    if(track.key==='sx'||track.key==='sy'){const amplitude=Math.max(params.moveAmplitude,params.blockedAmplitude);min=1-amplitude-.03;max=1+amplitude+.03;}
    const sampleMin=Math.min(...samples),sampleMax=Math.max(...samples);
    if(sampleMin<min)min=sampleMin-(max-sampleMin)*.03;
    if(sampleMax>max)max=sampleMax+(sampleMax-min)*.03;
    const y=value=>46-(value-min)/(max-min)*38;
    return {path:samples.map((value,i)=>(i?'L':'M')+(i/96*354+3).toFixed(2)+','+y(value).toFixed(2)).join(' '),y,min,max};
  },[track,effect,context,params,fps,options,duration]);
  const x=3+time/duration*354,value=sample.values[track.key];
  let formula=track.formula;
  if(track.key==='aperture')formula=sample.mesh.phase==='Closing'?'aperture = 1.1 × (1 − Ease(t))':sample.mesh.phase==='Covered'?'aperture = 0':'aperture = 1.1 × Ease(t)';
  if((track.key==='sx'||track.key==='sy')&&!options.squash)formula='scale = 1（挤压已关闭）';
  return <button className={'motion-curve '+(selected?'selected':'')} aria-label={'动效曲线：'+track.label} aria-pressed={selected} onClick={onSelect} data-track={track.key} data-value={value}><div><span>{track.label}</span><strong className="motion-track-value">{fmt(value,track.unit)}</strong></div><svg viewBox="0 0 360 54" preserveAspectRatio="none" aria-hidden="true"><line className="curve-axis" x1="3" x2="357" y1="46" y2="46"/><line className="curve-grid" x1="3" x2="357" y1="27" y2="27"/><path d={chart.path}/><line className="curve-playhead" x1={x} x2={x} y1="3" y2="51"/><circle cx={x} cy={chart.y(value)} r="3"/></svg><div className="curve-scale"><span>{chart.min.toFixed(2)}</span><code>{formula}</code><span>{chart.max.toFixed(2)}</span></div></button>;
}

function parameterFields(effect){
  const ms=(key,label,min,max)=>({key,label,min,max,step:key==='menuTau'?1:10,mult:1000,unit:'ms'});
  const percent=(key,label,min,max)=>({key,label,min,max,step:.5,mult:100,unit:'%'});
  if(effect.group==='board'){
    if(effect.id==='switch')return [ms('backgroundFade','背景渐变',50,400)];
    if(effect.id==='covered')return [ms('moveDuration','反馈时长',60,400),{key:'shakeAmplitude',label:'摇晃幅度',min:0,max:.1,step:.005,mult:1,unit:'格'}];
    if(effect.id==='marker'||effect.id==='goal')return [ms('moveDuration','移动时长',50,400),ms('markerDuration','标记渐显',100,700)];
    return [ms('moveDuration','动作时长',50,400),percent(effect.id==='blocked'?'blockedAmplitude':'moveAmplitude','挤压幅度',0,35)];
  }
  if(effect.group==='menu')return effect.id==='panel'?[ms('panelDuration','面板入场',60,500),percent('panelMinScale','起始尺寸',80,100)]:[ms('menuTau','平滑时间 τ',20,200),percent(effect.id==='press'?'menuPressScale':'menuHover',effect.id==='press'?'按下尺寸':'选中尺寸',effect.id==='press'?80:100,effect.id==='press'?100:115)];
  if(effect.group==='iris')return [...(effect.id==='reveal'?[]:[ms('closeDuration','闭合时长',100,700)]),ms('openDuration','打开时长',100,700)];
  if(effect.group==='complete')return [ms('completionHold','消散前等待',100,350),ms('completionVanish','消散时长',150,600)];
  return [{key:'particleSpeed',label:'漂移速度',min:.1,max:1.5,step:.1,mult:1,unit:'格/s'},{key:'particlePhaseSpeed',label:'呼吸相位速度',min:.1,max:.7,step:.05,mult:1,unit:'rad/s'}];
}

export function MotionExplorer({onSource}){
  const [effectId,setEffectId]=useState('move'),[time,setTime]=useState(0),[running,setRunning]=useState(false),[speed,setSpeed]=useState(.25),[fps,setFps]=useState(60),[loop,setLoop]=useState(true),[visible,setVisible]=useState(false);
  const [params,setParams]=useState(source),[options,setOptions]=useState(defaults),[mode,setMode]=useState('interactive'),[selectedKey,setSelectedKey]=useState('distance'),[aspect,setAspect]=useState(16/9);
  const root=useRef(null),clock=useRef(0);
  const effect=motionEffects.find(e=>e.id===effectId),context=useMemo(()=>motionContext(effect,DATA.levels),[effect]);
  const duration=motionDuration(effect,params,fps),currentTime=Math.min(time,duration),sample=sampleMotion(effect,currentTime,context,params,fps,options);
  const selected=effect.tracks.find(track=>track.key===selectedKey)||effect.tracks[0];
  const milestones=motionMilestones(effect,params,fps),fields=parameterFields(effect);
  const edited=Object.keys(source).some(key=>typeof source[key]==='number'&&source[key]!==params[key])||!options.squash||!options.fade;
  const ticks=useMemo(()=>effect.group==='complete'?completionTicks(params,fps):[],[effect.group,params,fps]);
  const seek=next=>{setRunning(false);clock.current=Math.max(0,Math.min(duration,next));setTime(clock.current);};
  const stepFrame=direction=>seek((direction>0?Math.floor(currentTime*fps+1e-8)+1:Math.ceil(currentTime*fps-1e-8)-1)/fps);
  const choose=id=>{const next=motionEffects.find(e=>e.id===id);setEffectId(id);setRunning(false);setTime(0);clock.current=0;setParams(source);setOptions({...defaults,zoom:['covered','marker','goal'].includes(next.id)});setSelectedKey(next.tracks[0].key);setMode('interactive');setSpeed(next.group==='particles'?4:.25);};
  useEffect(()=>{const observer=new IntersectionObserver(([entry])=>setVisible(entry.isIntersecting),{rootMargin:'80px'});observer.observe(root.current);return()=>observer.disconnect();},[]);
  useEffect(()=>{
    if(!running||!visible||mode!=='interactive')return;
    let frame,previous;
    const advance=now=>{
      if(previous!==undefined)clock.current+=(now-previous)/1000*speed;
      previous=now;
      if(clock.current>=duration){setTime(duration);if(!loop){setRunning(false);return;}if(clock.current>=duration+.28)clock.current=0;}
      else setTime(Math.floor(clock.current*fps+1e-8)/fps);
      frame=requestAnimationFrame(advance);
    };
    frame=requestAnimationFrame(advance);return()=>cancelAnimationFrame(frame);
  },[running,visible,mode,speed,fps,duration,loop]);
  useEffect(()=>{if(time>duration){clock.current=duration;setTime(duration);}},[time,duration]);
  const togglePlayback=()=>{if(!running&&currentTime>=duration){clock.current=0;setTime(0);}setRunning(s=>!s);};
  const keyPlayback=e=>{if(e.code==='Space'&&e.target===e.currentTarget){e.preventDefault();togglePlayback();}};
  return <section id="motion" className="section motion-section" ref={root}>
    <div className="section-heading"><h2 title={source.scope}>动画与动效</h2></div>
    <div className="motion-workbench" data-effect={effectId} data-time={currentTime} data-playing={running} data-phase={sample.phase} data-edited={edited}>
      <div className="motion-groups" role="tablist" aria-label="动效分类">{motionGroups.map(([id,name])=><button key={id} role="tab" aria-selected={effect.group===id} onClick={()=>choose(groupFirst(id).id)}>{name}</button>)}</div>
      <div className="motion-toolbar"><label><select aria-label="动效案例" value={effectId} onChange={e=>choose(e.target.value)}>{motionEffects.filter(e=>e.group===effect.group).map(e=><option value={e.id} key={e.id}>{e.name}</option>)}</select></label><div className="segmented" aria-label="动效视图"><button aria-pressed={mode==='interactive'} onClick={()=>setMode('interactive')}>交互拆解</button><button aria-pressed={mode==='reference'} onClick={()=>{setMode('reference');setRunning(false);}}>Unity 实机</button></div></div>
      {mode==='reference'?<div className="motion-reference"><video key={effect.group} controls preload="metadata" poster={effect.group==='board'||effect.group==='complete'?'/media/images/motion-board.webp':'/media/images/motion-title.webp'} src={effect.group==='board'||effect.group==='complete'?'/media/gameplay.mp4':'/media/transitions.mp4'}/><span title="来自已归档的 Unity Camera 与 AudioListener 采集；采集帧率不代表游戏性能。">Unity 实机归档 · {effect.group==='board'||effect.group==='complete'?'Box06_gameplay.mp4':'Presentation / Transitions.mp4'}</span></div>:<>
        <div className="motion-layout"><div className="motion-preview-panel"><div className="motion-preview-header"><strong>{effect.name}</strong><span>{sample.phase}</span>{edited&&<b>实验参数</b>}</div>
          <div className={'motion-preview-stage group-'+effect.group} tabIndex={0} aria-label="动效预览，空格播放或暂停" onKeyDown={keyPlayback}>
            {context?<MotionBoard context={context} time={currentTime} params={params} options={options} complete={effect.group==='complete'} fps={fps}/>:effect.group==='menu'?<MenuMotionPreview time={currentTime} params={params} kind={effect.id}/>:effect.group==='iris'?<IrisMotionPreview time={currentTime} params={params} fps={fps} aspect={aspect} mesh={options.mesh} reveal={effect.id==='reveal'}/>:<div className="particle-motion-scene"><MotionBackground time={currentTime} params={params} active={1} particles fps={fps}/><span>固定教学初相位 · 15 个描边粒子</span></div>}
          </div>
          <div className="motion-timeline"><div className="motion-transport"><button className="button motion-play" aria-label={running?'暂停动效':'播放动效'} onClick={togglePlayback}>{running?<Pause size={15}/>:<Play size={15}/>}<span>{running?'暂停':'播放'}</span></button><button className="icon-button" aria-label="动效上一帧" onClick={()=>stepFrame(-1)} disabled={currentTime<=0}><ChevronLeft size={17}/></button><button className="icon-button" aria-label="动效下一帧" onClick={()=>stepFrame(1)} disabled={currentTime>=duration}><ChevronRight size={17}/></button><output aria-label="动效当前时间">{timestamp(currentTime)}<span>/ {timestamp(duration)}</span></output><label className="motion-speed"><select aria-label="动效播放速度" value={speed} onChange={e=>setSpeed(Number(e.target.value))}><option value="0.1">0.1×</option><option value="0.25">0.25×</option><option value="1">1×</option><option value="4">4×</option></select></label><label className="motion-loop"><input aria-label="循环播放动效" type="checkbox" checked={loop} onChange={e=>setLoop(e.target.checked)}/>循环</label><label className="motion-fps">采样<select aria-label="动效采样率" value={fps} onChange={e=>{setRunning(false);setFps(Number(e.target.value));}}><option value="30">30 Hz</option><option value="60">60 Hz</option><option value="120">120 Hz</option></select></label></div><div className="motion-milestones">{milestones.map(([t,name])=><button key={name} aria-label={'动效关键帧：'+name} onClick={()=>seek(t)} className={Math.abs(currentTime-t)<.0001?'current':''}><span>{name}</span><code>{timestamp(t)}</code></button>)}</div><input aria-label="动效时间轴" type="range" min="0" max={duration} step="0.001" value={currentTime} onChange={e=>seek(Number(e.target.value))}/></div>
          <div className="motion-layer-controls">{effect.group==='board'&&<>{!['covered','switch','marker','goal'].includes(effect.id)&&<label><input aria-label="叠加角色挤压" type="checkbox" checked={options.squash} onChange={e=>{setRunning(false);setOptions(s=>({...s,squash:e.target.checked}));}}/>角色挤压</label>}{['move','marker','goal'].includes(effect.id)&&<label><input aria-label="叠加标记渐显" type="checkbox" checked={options.fade} onChange={e=>{setRunning(false);setOptions(s=>({...s,fade:e.target.checked}));}}/>标记渐显</label>}<label><input aria-label="显示运动轨迹" type="checkbox" checked={options.guides} onChange={e=>setOptions(s=>({...s,guides:e.target.checked}))}/>轨迹</label><label><input aria-label="局部放大动效" type="checkbox" checked={options.zoom} onChange={e=>setOptions(s=>({...s,zoom:e.target.checked}))}/>局部放大</label></>}{effect.group==='iris'&&<><label><input aria-label="显示圆孔网格" type="checkbox" checked={options.mesh} onChange={e=>setOptions(s=>({...s,mesh:e.target.checked}))}/>显示网格</label><label>比例<select aria-label="转场画面比例" value={aspect} onChange={e=>setAspect(Number(e.target.value))}><option value={16/9}>16 : 9</option><option value={4/3}>4 : 3</option><option value={64/27}>64 : 27</option></select></label></>}{effect.group==='particles'&&<span>时间使用 unscaledTime，漂移使用 unscaledDeltaTime。</span>}
          </div><p className="motion-lesson">{effect.lesson}</p>
        </div><div className="motion-property-panel"><div className="motion-property-title"><strong>属性曲线</strong><span>点曲线，定位源码</span></div><code className="motion-clock-formula">{clockFormula(effect,selected,currentTime,params,fps,sample)}</code>{effect.tracks.map(track=><CurveTrack key={track.key} track={track} effect={effect} context={context} params={params} fps={fps} options={options} time={currentTime} duration={duration} sample={sample} selected={track.key===selected.key} onSelect={()=>setSelectedKey(track.key)}/>)}
          {ticks.length>0&&<div className="motion-sound-ticks"><div><span>消散音效</span><strong>{ticks.filter(t=>t<=currentTime).length} 次触发</strong></div><svg viewBox="0 0 360 24" aria-label="按采样帧检查最少40毫秒冷却的音效触点">{ticks.map(t=><line key={t} x1={t/duration*354+3} x2={t/duration*354+3} y1="3" y2="20" className={t<=currentTime?'played':''}/>)}</svg><small>冷却 ≥ {Math.round(params.completionTick*1000)}ms · 按帧检查</small></div>}
          <SourceExcerpt file={selected.file} anchor={selected.anchor} label="这条曲线的源码" lines={selected.key==='aperture'?14:8} onSource={onSource}/>
          {effect.group==='iris'&&options.mesh&&<SourceExcerpt file="StudyIrisGraphic.cs" anchor="protected override void OnPopulateMesh" label="圆孔几何的源码" lines={22} onSource={onSource}/>}
          {effect.group==='complete'&&<SourceExcerpt file="BoardView.cs" anchor="int hash=((x*73856093)" label="按格子消散的源码" lines={5} onSource={onSource}/>}
        </div></div>

        <details className="motion-parameter-editor"><summary><SlidersHorizontal size={14}/>参数实验<span>{edited?'已调整':'工程默认值'}</span></summary><div className="motion-parameter-fields">{fields.map(field=><label key={field.key}><span>{field.label}<code>{(params[field.key]*field.mult).toFixed(field.mult===1?3:field.mult===100?1:0)} {field.unit}</code></span><input aria-label={'动效参数：'+field.label} type="range" min={field.min} max={field.max} step={field.step} value={params[field.key]*field.mult} onChange={e=>{setRunning(false);setParams(s=>({...s,[field.key]:Number(e.target.value)/field.mult}));}}/></label>)}<button className="text-button" onClick={()=>{setParams(source);setOptions(defaults);seek(0);}}><RotateCcw size={13}/>恢复工程值</button><span>仅影响当前网页演示</span></div></details>
      </>}
    </div>
  </section>;
}
