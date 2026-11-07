import React,{useId} from 'react';
import DATA from './data';
import {Board,SpriteImage,asset} from './Board';
import {stackAt,goalsOf,fulfilled} from './engine';
import {pieceMotion,markerMotion,shakeOffset,menuMotion,irisMotion,irisGeometry,cellThreshold,completionMotion,particleMotion,clamp01} from './motion-model';

const prefixes=['','white','black','gray'];
const themeFor=chapter=>Object.fromEntries(DATA.themes[chapter].sprites.map(s=>[s.key,s.sprite]));

export function MotionBackground({time,params,active=1,before=active,switching=false,particles=false,menu=false,fps=60}){
  const colors=active===1?['.5','.7526','1']:['.2863','.2196','.2353'];
  const opacity=switching?clamp01(time/params.backgroundFade):1;
  return <div className="motion-background" aria-hidden="true"><img src={asset(menu?'Main Menu Background_0':before===1?'white_background_0':'black_background_0')} alt=""/><img src={asset(menu?'Main Menu Background_0':active===1?'white_background_0':'black_background_0')} alt="" style={{opacity}}/>{particles&&<svg viewBox="-8.8889 -5 17.7778 10" preserveAspectRatio="none">{Array.from({length:params.particleCount},(_,i)=>{
    const p=particleMotion(i,time,params,16/9,fps),m=DATA.spriteMeta.Particle_Box_Uncolored;
    const w=m.w*p.scale,h=m.h*p.scale;
    return <image key={i} href={asset('Particle_Box_Uncolored')} x={p.x-w*m.pivotX} y={-p.y-h*(1-m.pivotY)} width={w} height={h} opacity={p.alpha} style={{filter:active===1?'brightness(0) saturate(100%) invert(75%) sepia(40%) saturate(750%) hue-rotate(176deg)':'brightness(0) saturate(100%) invert(15%) sepia(9%) saturate(1500%) hue-rotate(290deg)'}}/>;
  })}</svg>}{!menu&&<img className="motion-vignette" src={asset('vignette')} alt=""/>}</div>;
}

function MovingPieces({context,time,params,options,complete=false}){
  const {before,after,kind,level}=context,theme=themeFor(level.chapter),height=level.definition.height;
  const fraction=complete?completionMotion(time,params).fraction:0;
  const visible=(x,y)=>fraction<=0||cellThreshold(x,y)>=fraction;
  const layers=[];
  const add=(id,key,x,y,order,scaleX=1,scaleY=1,alpha=1)=>{
    if(!visible(x,y)||alpha<=0)return;
    const cx=(x+.5)*40,cy=(height-y-.5)*40;
    layers.push({id,order,node:<g key={id} transform={`translate(${cx} ${cy}) scale(${scaleX} ${scaleY}) translate(${-cx} ${-cy})`} opacity={alpha}><SpriteImage name={theme[key]} x={x} y={y} height={height}/></g>});
  };
  for(const piece of after.pieces){
    const old=before.pieces.find(p=>p.i===piece.i),pose=pieceMotion(old,piece,after.active,kind,time,params,options);
    const prefix=prefixes[piece.tone],active=piece.tone===after.active,facing={U:'Up',R:'Right',D:'Down',L:'Left'}[piece.facing]||'Right';
    const key=prefix+(piece.kind===5?'Player'+(active?'ActiveFacing'+facing+'Sprite':'InactiveSprite'):'Box'+(active?'ActiveSprite':'InactiveSprite'));
    add('piece'+piece.i,key,pose.x,pose.y,pose.order,pose.sx,pose.sy);
    const stack=stackAt(after,piece.x,piece.y);
    if(stack.at(-2)?.i===piece.i){
      const m=markerMotion(before,after,[piece.x,piece.y],time,params,options.fade!==false);
      const shake=piece.kind===5&&active&&kind==='Covered'?shakeOffset(time,params):0;
      add('under'+piece.i,prefix+(piece.kind===5?'Player':'Box')+'UnderSprite',piece.x+shake,piece.y,80,m.scale,m.scale,m.alpha);
    }
  }
  for(const [i,goal] of goalsOf(level.definition).entries()){
    const stack=stackAt(after,goal.x,goal.y),occupied=stack.length>0,done=fulfilled(after,goal),prefix=prefixes[goal.tone]+(goal.kind===5?'Player':'Box');
    const m=markerMotion(before,after,[goal.x,goal.y],time,params,options.fade!==false);
    if(!done&&stack.length<2){const amount=occupied?m:{alpha:1,scale:1};add('goal'+i,prefix+(occupied?'GoalUnderSprite':'GoalSprite'),goal.x,goal.y,70,amount.scale,amount.scale,amount.alpha);}
    if(done)add('check'+i,'goalFulfillmentSprite',goal.x,goal.y,90,m.scale,m.scale,m.alpha);
  }
  return <>{layers.sort((a,b)=>a.order-b.order).map(l=>l.node)}</>;
}

export function MotionBoard({context,time,params,options,complete=false,fps=60}){
  const fraction=complete?completionMotion(time,params).fraction:0;
  const visible=(x,y)=>fraction<=0||cellThreshold(x,y)>=fraction;
  const old=context.before.pieces.find(p=>p.i===context.focus.i),end=context.focus,height=context.level.definition.height;
  const guides=options.guides&&<g className="motion-guides"><rect x={old.x*40} y={(height-old.y-1)*40} width="40" height="40"/><rect x={end.x*40} y={(height-end.y-1)*40} width="40" height="40"/><line x1={(old.x+.5)*40} y1={(height-old.y-.5)*40} x2={(end.x+.5)*40} y2={(height-end.y-.5)*40}/></g>;
  const focusCell=context.markerCell;
  const crop=options.zoom?`${(focusCell[0]+.5)*40-60} ${(height-focusCell[1]-.5)*40-60} 120 120`:undefined;
  return <div className="motion-board-scene"><MotionBackground time={time} params={params} active={context.after.active} before={context.before.active} switching={context.kind==='Switched'} particles={context.level.chapter===1} fps={fps}/><Board level={context.level} state={context.after} hideDynamic cellVisible={visible} viewBoxOverride={crop} overlay={<><MovingPieces context={context} time={time} params={params} options={options} complete={complete}/>{guides}</>}/></div>;
}

export function MenuMotionPreview({time,params,kind}){
  const id=useId().replace(/:/g,''),pose=menuMotion(time,params,kind),panel=kind==='panel',alpha=panel?pose.panelAlpha:1,scale=panel?pose.panelScale:1;
  const cursorX=panel?-102.5:kind==='press'?102.5:pose.cursor;
  return <div className="menu-motion-scene"><MotionBackground menu particles active={2} time={time} params={params}/><svg viewBox="0 0 1280 720" aria-label="菜单卡片与选择框的属性演示"><defs><filter id={id+'green'}><feColorMatrix type="matrix" values="0 0 0 0 .36213 0 0 0 0 .71698 0 0 0 0 .15557 0 0 0 1 0"/></filter></defs><g opacity={alpha} transform={`translate(640 360) scale(${scale}) translate(-640 -360)`}>
    <text x="1210" y="65" textAnchor="end" className="demo-progress">21/21 已解</text>
    {[['Tut World Tile-sc',-102.5,panel?params.menuHover:pose.leftScale],['Box World Tile',102.5,panel?1:pose.rightScale]].map(([name,x,s])=><g key={name} transform={`translate(${640+x} 360) scale(${s})`}><image href={asset(name)} x="-97.5" y="-97.5" width="195" height="195"/></g>)}
    <image href={asset('Cursor')} x={640+cursorX-111.15} y={360-111.15} width="222.3" height="222.3" filter={`url(#${id}green)`}/><text x="640" y="535" textAnchor="middle" className="demo-menu-name">{panel?'欢迎':'箱即是空'}</text><text x="640" y="695" textAnchor="middle" className="demo-menu-footer">方向键 / WASD 选择　空格 / Enter 确认　Esc 返回</text>
  </g></svg></div>;
}

export function IrisMotionPreview({time,params,fps,aspect=16/9,mesh=false,reveal=false}){
  const id=useId().replace(/:/g,''),state=irisMotion(time,params,fps,reveal),width=1280,height=width/aspect,cx=width/2,cy=height/2;
  const g=irisGeometry(state.aperture,width,height,params);
  const edge=g.radius>0?Math.max(0,g.inner/g.radius):0;
  const lines=[];
  if(mesh&&g.vertices>4)for(let i=0;i<params.irisSegments;i++){
    const a=i*Math.PI*2/params.irisSegments,b=(i+1)*Math.PI*2/params.irisSegments;
    const point=(angle,r)=>`${cx+Math.cos(angle)*r},${cy+Math.sin(angle)*r}`;
    lines.push(<path key={i} d={`M${point(a,g.inner)}L${point(b,g.inner)}L${point(b,g.radius)}Z M${point(a,g.inner)}L${point(b,g.radius)}L${point(a,g.radius)}Z M${point(a,g.radius)}L${point(b,g.radius)}L${point(b,g.outer)}Z M${point(a,g.radius)}L${point(b,g.outer)}L${point(a,g.outer)}Z`}/>);
  }
  return <div className="iris-motion-scene" style={{aspectRatio:aspect}} data-phase={state.phase} data-page={reveal?'initial':state.newPage?'new':'old'}><svg viewBox={`0 0 ${width} ${height}`} aria-label="以圆形孔径闭合、换页、打开的转场"><defs><radialGradient id={id+'edge'}><stop offset={edge} stopColor="black"/><stop offset="1" stopColor="white"/></radialGradient><mask id={id+'hole'} maskUnits="userSpaceOnUse" x="0" y="0" width={width} height={height}><rect width={width} height={height} fill="white"/>{g.radius>0&&<circle cx={cx} cy={cy} r={g.radius} fill={`url(#${id}edge)`}/>}</mask></defs><image href={!reveal&&state.newPage?'/media/images/motion-chapters.webp':'/media/images/motion-title.webp'} width={width} height={height} preserveAspectRatio="xMidYMid slice"/>{state.aperture<1.0999&&<rect width={width} height={height} fill="#060709" mask={`url(#${id}hole)`}/>}<g className="iris-wireframe">{lines}{mesh&&g.radius>0&&<circle cx={cx} cy={cy} r={g.radius}/>}</g></svg>{mesh&&<span className="mesh-count">{g.vertices} 顶点 · {g.triangles} 三角形</span>}</div>;
}
