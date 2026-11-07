import {apply,fromTrace,initial,goalsOf,fulfilled,stackAt} from './engine.js';
import {pieceMotion,markerMotion,shakeOffset,menuMotion,irisMotion,completionMotion,particleMotion,clamp01} from './motion-model.js';

export const motionGroups=[['board','棋盘动作'],['menu','菜单反馈'],['iris','圆形转场'],['complete','过关节奏'],['particles','动态背景']];
const position={key:'distance',label:'位移',unit:'格',range:[0,1],file:'BoardView.cs',anchor:'pieces[i].transform.localPosition=Vector3.Lerp',formula:'position = Lerp(before, after, t)'};
const sx={key:'sx',label:'X 缩放',unit:'×',range:[.78,1.35],file:'BoardView.cs',anchor:'float amount=Mathf.Sin',formula:'amount = sin(πt) × amplitude'};
const sy={...sx,key:'sy',label:'Y 缩放'};
const marker={key:'markerAlpha',label:'标记透明度',unit:'',range:[0,1],file:'BoardView.cs',anchor:'private float MarkerAmount',formula:'alpha = clamp((elapsed − duration) / fadeDuration)'};
const markerSize={key:'markerScale',label:'标记缩放',unit:'×',range:[.6,1],file:'BoardView.cs',anchor:'renderer.color=new Color(1,1,1,amount)',formula:'scale = Lerp(0.65, 1, alpha)'};
const cursor={key:'cursor',label:'光标位置',unit:'px',range:[-103,103],file:'StudyMenu.cs',anchor:'float blend=1-Mathf.Exp',formula:'blend = 1 − exp(−Δt / τ)'};
const hover={key:'hover',label:'选中卡缩放',unit:'×',range:[.92,1.06],file:'StudyMenu.cs',anchor:'var item=items[i];float target=i==selected',formula:'scale = Lerp(current, target, blend)'};
const aperture={key:'aperture',label:'圆孔开度',unit:'',range:[0,1.1],file:'StudyTransition.cs',anchor:'if(phase==Phase.Closing)',formula:'aperture = 1.1 × Ease(t)'};
export const motionEffects=[
  {id:'move',group:'board',name:'角色移动',level:'n_tut_2',step:5,command:'R',lesson:'位置匀速前进，挤压只作用于当前角色。',tracks:[position,sx,sy]},
  {id:'push',group:'board',name:'连续推箱',level:'n_box_3',step:4,command:'R',lesson:'同一次推动共用一条动画进度；箱子平移，角色挤压。',tracks:[position,sx,sy]},
  {id:'blocked',group:'board',name:'推动受阻',level:'n_box_1',step:6,command:'L',lesson:'格子位置不变，用幅度更小的挤压回应按键。',tracks:[position,sx,sy]},
  {id:'covered',group:'board',name:'被覆盖时摇晃',level:'n_box_7',step:17,seed:'S',command:'R',lesson:'被盖住的角色不能位移；摇晃的是它的下层标记。',tracks:[{key:'shake',label:'标记横移',unit:'格',range:[-.045,.045],file:'BoardView.cs',anchor:'under[i].transform.localPosition+=',formula:'offset = sin(4πt) × amplitude'},position]},
  {id:'marker',group:'board',name:'下层标记显现',level:'n_box_6',step:23,command:'U',lesson:'先完成移动，再让下层标记渐显、长大。',tracks:[position,marker,markerSize]},
  {id:'goal',group:'board',name:'目标完成勾',level:'n_tut_2',final:true,lesson:'目标在逻辑中已满足，完成勾随标记通道渐显。',tracks:[position,marker,markerSize]},
  {id:'undo',group:'board',name:'撤销',level:'n_tut_2',step:6,kind:'Undone',lesson:'先恢复完整快照，再把画面插值到恢复后的位置。',tracks:[position,sx,sy]},
  {id:'reset',group:'board',name:'重置',level:'n_tut_2',step:6,kind:'Reset',lesson:'所有动体同时回到起点，仍复用同一个 Present 通道。',tracks:[position,sx,sy]},
  {id:'switch',group:'board',name:'黑白切换',level:'n_tut_2',step:5,command:'S',lesson:'角色与墙体贴图直接换色，背景单独交叉渐变。',tracks:[{key:'backgroundAlpha',label:'新背景透明度',unit:'',range:[0,1],file:'StudyBackdrop.cs',anchor:'fade=Mathf.Min',formula:'fade += Δt / fadeDuration'},position]},
  {id:'cursor',group:'menu',name:'选择框与卡片',lesson:'每帧靠近目标；同一平滑系数驱动光标跟随和选中放大。',tracks:[cursor,hover]},
  {id:'press',group:'menu',name:'按下与回弹',lesson:'按下时追向更小尺寸，释放后再追向选中尺寸。',tracks:[{...hover,file:'StudyMenu.cs',anchor:'if(Time.unscaledTime-item.pressedAt',formula:'target = pressed ? pressScale : hoverScale'},cursor]},
  {id:'panel',group:'menu',name:'面板入场',lesson:'透明度线性变化；缩放用 EaseOutCubic，尽早接近终值。',tracks:[{key:'panelAlpha',label:'透明度',unit:'',range:[0,1],file:'StudyMenu.cs',anchor:'float t=Mathf.Clamp01((Time.unscaledTime-panelOpenedAt)',formula:'alpha = clamp(elapsed / duration)'},{key:'panelScale',label:'整体缩放',unit:'×',range:[.975,1],file:'StudyMenu.cs',anchor:'dynamicGroup.alpha=t;',formula:'scale = Lerp(minScale, 1, 1 − (1−t)³)'}]},
  {id:'iris',group:'iris',name:'页面切换',lesson:'闭合 → 完全遮住一帧 → 换页 → 打开；切换发生在圆孔为零时。',tracks:[aperture,{key:'page',label:'页面已切换',unit:'',range:[0,1],file:'StudyTransition.cs',anchor:'else if(phase==Phase.Covered',formula:'frameCount > coveredFrame → change()'}]},
  {id:'reveal',group:'iris',name:'首次揭幕',lesson:'首次进入只打开圆孔，复用同一几何与缓动函数。',tracks:[{...aperture,anchor:'public void Reveal'}]},
  {id:'complete',group:'complete',name:'过关消散',level:'n_box_6',final:true,lesson:'每格固定一个消散门限；同格图层一起隐藏，重播顺序一致。',tracks:[{key:'fraction',label:'消散门限',unit:'',range:[0,1],file:'PuzzleApp.cs',anchor:'float t=Time.unscaledTime-completedAt;',formula:'fraction = clamp((elapsed − hold) / duration)'},marker]},
  {id:'particles',group:'particles',name:'漂移与呼吸',lesson:'匀速漂移、两种频率的正弦变化叠加；越界后移到对侧。',tracks:[{key:'particleX',label:'示例粒子 X',unit:'格',range:[-10,10],file:'StudyBackdrop.cs',anchor:'var position=p.transform.localPosition+',formula:'position += velocity × Δt'},{key:'particleAlpha',label:'透明度',unit:'',range:[.4,1],file:'StudyBackdrop.cs',anchor:'color.a=.4f+',formula:'alpha = 0.7 + 0.3 × sin(phase)'},{key:'particleScale',label:'缩放',unit:'×',range:[.3,.6],file:'StudyBackdrop.cs',anchor:'float scale=scales[i]',formula:'scale = base × (0.85 + 0.15 × sin(0.4 × phase))'}]}
];
export function motionContext(effect,levels){
  if(!effect.level)return null;
  const level=levels.find(l=>l.id===effect.level);
  let index=effect.final?level.trace.length-2:effect.step;
  let before=fromTrace(level.trace[index]);
  if(effect.seed)before=apply(level.definition,before,effect.seed).state;
  let after,kind,command=effect.final?level.commands.at(-1):effect.command;
  if(effect.kind==='Undone'){after=fromTrace(level.trace[index-1]);kind=effect.kind;}
  else if(effect.kind==='Reset'){after=initial(level.definition);kind=effect.kind;}
  else {const result=apply(level.definition,before,command);after=result.state;kind=result.kind;}
  const focus=after.pieces.find(p=>p.kind===5&&p.tone===after.active);
  const goal=goalsOf(level.definition).find(g=>fulfilled(after,g)&&!fulfilled(before,g));
  const covered=after.pieces.find(p=>stackAt(after,p.x,p.y).length>1);
  const markerCell=goal?[goal.x,goal.y]:covered?[covered.x,covered.y]:[focus.x,focus.y];
  return {level,before,after,kind,command,focus,markerCell};
}
export function motionDuration(effect,p,fps=60){
  if(effect.group==='board')return ['push','undo','reset','blocked','covered','switch'].includes(effect.id)?Math.max(p.moveDuration,p.backgroundFade)+.12:p.moveDuration+p.markerDuration+.06;
  if(effect.group==='menu')return effect.id==='panel'?p.panelDuration+.1:.5;
  if(effect.group==='iris')return irisMotion(0,p,fps,effect.id==='reveal').total+.06;
  if(effect.group==='complete')return p.completionReturn;
  return 30;
}
export function sampleMotion(effect,time,context,p,fps=60,options={}){
  let values={},phase='',locked=false,mesh=null;
  if(context){
    const piece=context.focus,old=context.before.pieces.find(o=>o.i===piece.i);
    const pose=pieceMotion(old,piece,context.after.active,context.kind,time,p,options);
    const badge=markerMotion(context.before,context.after,context.markerCell,time,p,options.fade!==false);
    values={distance:Math.hypot(pose.x-old.x,pose.y-old.y),sx:pose.sx,sy:pose.sy,markerAlpha:badge.alpha,markerScale:badge.scale,shake:shakeOffset(time,p),backgroundAlpha:clamp01(time/p.backgroundFade)};
    phase=time<p.moveDuration?'位移动画':time<p.moveDuration+p.markerDuration?'标记显现':'稳定';
    if(['blocked','covered'].includes(effect.id))phase=time<p.moveDuration?'动作反馈':'稳定';
    if(['push','undo','reset'].includes(effect.id))phase=time<p.moveDuration?'位移动画':'稳定';
    if(effect.id==='switch')phase=time<p.backgroundFade?'背景渐变':'稳定';
    locked=['Moved','Pushed'].includes(context.kind)&&time<p.moveDuration;
  }
  if(effect.group==='menu'){
    const menu=menuMotion(time,p,effect.id);values={cursor:menu.cursor,hover:menu.rightScale,panelAlpha:menu.panelAlpha,panelScale:menu.panelScale};
    phase=effect.id==='panel'?(time<p.panelDuration?'面板入场':'稳定'):effect.id==='press'?(time<p.menuPressDuration?'按下':'回弹'):'跟随目标';
  }
  if(effect.group==='iris'){
    mesh=irisMotion(time,p,fps,effect.id==='reveal');values={aperture:mesh.aperture,page:mesh.newPage?1:0};phase={Closing:'闭合',Covered:'全遮盖',Opening:'打开',Idle:'完成'}[mesh.phase];locked=mesh.busy;
  }
  if(effect.group==='complete'){const complete=completionMotion(time,p);values.fraction=complete.fraction;phase=complete.phase;locked=true;}
  if(effect.group==='particles'){const particle=particleMotion(0,time,p,16/9,fps);values={particleX:particle.x,particleAlpha:particle.alpha,particleScale:particle.scale};phase='漂移 / 呼吸';}
  return {values,phase,locked,mesh};
}
export function motionMilestones(effect,p,fps){
  if(effect.group==='board')return effect.id==='switch'?[[0,'换色'],[p.backgroundFade,'背景稳定']]:['blocked','covered'].includes(effect.id)?[[0,'反馈开始'],[p.moveDuration/(effect.id==='covered'?8:2),'最大振幅'],[p.moveDuration,'归位']]:['push','undo','reset'].includes(effect.id)?[[0,'逻辑提交'],[p.moveDuration/2,'移动中点'],[p.moveDuration,'到位']]:[[0,'逻辑提交'],[p.moveDuration/2,'移动中点'],[p.moveDuration,'到位'],[p.moveDuration+p.markerDuration,'标记稳定']];
  if(effect.group==='menu')return effect.id==='panel'?[[0,'打开面板'],[p.panelDuration/2,'入场中点'],[p.panelDuration,'稳定']]:effect.id==='press'?[[0,'按下'],[p.menuPressDuration,'释放'],[p.menuPressDuration+p.menuTau*3,'回弹 95%']]:[[0,'选择变化'],[p.menuTau,'接近 63%'],[p.menuTau*3,'接近 95%']];
  if(effect.group==='iris'){const m=irisMotion(0,p,fps,effect.id==='reveal');return effect.id==='reveal'?[[0,'全遮盖'],[p.openDuration/2,'打开中点'],[p.openDuration,'完成']]:[[0,'开始闭合'],[p.closeDuration/2,'闭合中点'],[p.closeDuration,'全遮盖'],[m.switchAt,'换页'],[m.total,'打开完成']];}
  if(effect.group==='complete')return [[0,'目标满足'],[p.moveDuration,'最后一步到位'],[p.completionHold,'开始消散'],[p.completionHold+p.completionVanish,'消散结束'],[p.completionReturn,'发起转场']];
  return [[0,'初始位置'],[10,'漂移 10s'],[20,'漂移 20s'],[30,'漂移 30s']];
}
