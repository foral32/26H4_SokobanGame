export const clamp01=x=>Math.max(0,Math.min(1,x));
export const lerp=(a,b,t)=>a+(b-a)*clamp01(t);
export const easeInOutCubic=t=>{t=clamp01(t);return t<.5?4*t*t*t:1-(-2*t+2)**3/2;};
export const easeOutCubic=t=>1-(1-clamp01(t))**3;
export const smoothingBlend=(dt,tau)=>1-Math.exp(-dt/tau);
export const approach=(from,to,time,tau)=>to+(from-to)*Math.exp(-Math.max(0,time)/tau);
export function pieceMotion(old,piece,active,kind,time,p,options={}){
  const t=clamp01(time/p.moveDuration),moved=old.x!==piece.x||old.y!==piece.y;
  const facing=piece.facing||'R',horizontal=facing==='R'||facing==='L';
  const amount=piece.kind===5&&piece.tone===active&&(moved||kind==='Blocked')&&t<1&&options.squash!==false?Math.sin(t*Math.PI)*(kind==='Blocked'?p.blockedAmplitude:p.moveAmplitude):0;
  return {x:lerp(old.x,piece.x,t),y:lerp(old.y,piece.y,t),sx:1+(horizontal?amount:-amount),sy:1+(horizontal?-amount:amount),order:moved&&t<1?60:10+piece.depth*5,progress:t,moved};
}
export function markerMotion(before,after,cell,time,p,enabled=true){
  const affected=after.pieces.some(piece=>{const old=before.pieces.find(o=>o.i===piece.i);return ((piece.x===cell[0]&&piece.y===cell[1])||(old.x===cell[0]&&old.y===cell[1]))&&(piece.x!==old.x||piece.y!==old.y||piece.depth!==old.depth);});
  const amount=affected&&enabled?clamp01((time-p.moveDuration)/p.markerDuration):1;
  return {alpha:amount,scale:lerp(p.markerMinScale,1,amount),visible:amount>0};
}
export const shakeOffset=(time,p)=>time<p.moveDuration?Math.sin(clamp01(time/p.moveDuration)*Math.PI*p.shakePiMultiplier)*p.shakeAmplitude:0;
export function menuMotion(time,p,kind='cursor'){
  const from=-102.5,to=102.5;
  const hover=approach(1,p.menuHover,time,p.menuTau),unhover=approach(p.menuHover,1,time,p.menuTau);
  const pressed=approach(p.menuHover,p.menuPressScale,Math.min(time,p.menuPressDuration),p.menuTau);
  const released=approach(pressed,p.menuHover,Math.max(0,time-p.menuPressDuration),p.menuTau);
  const panel=clamp01(time/p.panelDuration);
  return {cursor:kind==='press'?to:approach(from,to,time,p.menuTau),leftScale:kind==='press'?1:unhover,rightScale:kind==='press'?released:hover,panelAlpha:panel,panelScale:lerp(p.panelMinScale,1,easeOutCubic(panel)),remaining:Math.exp(-time/p.menuTau)};
}
export function irisMotion(time,p,fps=60,reveal=false){
  const closeFrame=Math.ceil(p.closeDuration*fps-1e-8),switchAt=reveal?0:(closeFrame+1)/fps;
  const total=switchAt+p.openDuration;
  if(reveal){const done=time>=p.openDuration;return {phase:done?'Idle':'Opening',aperture:p.irisMax*easeInOutCubic(time/p.openDuration),newPage:true,busy:!done,switchAt:0,openAt:0,total:p.openDuration};}
  if(time<p.closeDuration)return {phase:'Closing',aperture:p.irisMax*(1-easeInOutCubic(time/p.closeDuration)),newPage:false,busy:true,switchAt,openAt:switchAt,total};
  if(time<switchAt)return {phase:'Covered',aperture:0,newPage:false,busy:true,switchAt,openAt:switchAt,total};
  if(time<total)return {phase:'Opening',aperture:p.irisMax*easeInOutCubic((time-switchAt)/p.openDuration),newPage:true,busy:true,switchAt,openAt:switchAt,total};
  return {phase:'Idle',aperture:p.irisMax,newPage:true,busy:false,switchAt,openAt:switchAt,total};
}
export function irisGeometry(aperture,width,height,p,canvasScale=1){
  const diagonal=Math.hypot(width,height)/2,radius=diagonal*aperture;
  if(aperture>=1.0999)return {radius,inner:radius,outer:diagonal*2+10,vertices:0,triangles:0};
  if(radius<.01)return {radius,inner:0,outer:diagonal*2+10,vertices:4,triangles:2};
  return {radius,inner:Math.max(0,radius-p.irisFeather/canvasScale),outer:diagonal*2+10,vertices:(p.irisSegments+1)*3,triangles:p.irisSegments*4};
}
export function cellThreshold(x,y){return ((Math.imul(x,73856093)^Math.imul(y,19349663))&0x7fffffff)%997/997;}
export function completionMotion(time,p){
  const fraction=clamp01((time-p.completionHold)/p.completionVanish);
  return {fraction,phase:time>=p.completionReturn?'发起选关转场':time>=p.completionHold+p.completionVanish?'留白':time>=p.completionHold?'逐格消散':'完成反馈'};
}
export function completionTicks(p,fps){
  const times=[];let next=p.completionHold;
  for(let frame=0;frame/fps<p.completionHold+p.completionVanish-1e-8;frame++){
    const time=frame/fps;
    if(time+1e-8>=p.completionHold&&time+1e-8>=next){times.push(time);next=time+p.completionTick;}
  }
  return times;
}
export function particleMotion(index,time,p,aspect=16/9,fps=60){
  const phase0=index*2.399963229728653,baseScale=.5+(index%5)*.375;
  const phase=time*p.particlePhaseSpeed+phase0;
  let x=((index%5+.5)/5*2-1)*5*aspect,y=((Math.floor(index/5)+.5)/3*2-1)*5;
  const vx=Math.cos(phase0)*p.particleSpeed,vy=Math.sin(phase0)*p.particleSpeed,halfW=5*aspect+1,halfH=6;
  const whole=Math.floor(Math.max(0,time)*fps),remainder=Math.max(0,time)-whole/fps;
  for(let frame=0;frame<whole+(remainder>1e-8?1:0);frame++){
    const dt=frame===whole?remainder:1/fps;x+=vx*dt;y+=vy*dt;
    if(x>halfW)x=-halfW;if(x< -halfW)x=halfW;if(y>halfH)y=-halfH;if(y< -halfH)y=halfH;
  }
  return {x,y,scale:baseScale*(.85+.15*Math.sin(phase*.4)),alpha:.4+.6*(.5+.5*Math.sin(phase)),phase,vx,vy};
}
