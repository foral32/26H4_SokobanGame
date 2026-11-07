import React from 'react';
import DATA from './data';
import {stackAt,wallAt,goalsOf,fulfilled,toneName,kindName} from './engine';
import {maskNames as names,directions,missingMask,cornerFlags,cornerNames} from './wall-tiles';
export function asset(name){return '/media/sprites/'+encodeURIComponent(name)+'.png';}
export function SpriteImage({name,x,y,height,unit=40,...extra}){
  if(!name)return null;
  if(!DATA.spriteMeta[name])name+='_0';
  const m=DATA.spriteMeta[name];if(!m)return null;
  return <image href={asset(name)} x={(x+.5-m.w*m.pivotX)*unit} y={(height-y-.5-m.h*(1-m.pivotY))*unit} width={m.w*unit} height={m.h*unit} {...extra}/>;
}
export function Board({level,state,selected,onSelect,flat=false,markers=true,compact=false,annotations=[],hideDynamic=false,overlay=null,cellVisible,viewBoxOverride}){
  const def=level.definition,unit=40,w=def.width*unit,h=def.height*unit;
  const theme=Object.fromEntries(DATA.themes[level.chapter].sprites.map(s=>[s.key,s.sprite]));
  const prefix=t=>t===1?'white':t===2?'black':'gray';
  function sprite(key,x,y,extra={},identity=''){
    return <SpriteImage key={identity||key+':'+x+':'+y} name={theme[key]} x={x} y={y} height={def.height} unit={unit} {...extra}/>;
  }
  const walls=[],corners=[];
  for(let y=0;y<def.height;y++)for(let x=0;x<def.width;x++){
    const tone=wallAt(def,x,y);if(!tone)continue;
    if(cellVisible&&!cellVisible(x,y))continue;
    const connected=(dx,dy)=>wallAt(def,x+dx,y+dy)===tone&&!stackAt(state,x+dx,y+dy).length;
    const cardinal=directions.map(([dx,dy])=>connected(dx,dy));
    const diagonal=directions.map(([dx,dy],i)=>{const [bx,by]=directions[(i+1)%4];return connected(dx+bx,dy+by);});
    const mask=missingMask(cardinal);
    const active=tone===3||tone===state.active;
    if(flat)walls.push(<rect key={x+','+y} x={x*unit} y={(def.height-y-1)*unit} width={unit+.2} height={unit+.2} fill={tone===3?'#70797e':tone===1?'#dededf':'#212121'}/>);
    else{
      walls.push(sprite(prefix(tone)+(active?'WallActiveVariantSet.'+names[mask]:'WallInactiveSprite'),x,y));
      cornerFlags(cardinal,diagonal,active).forEach((visible,i)=>{if(visible)corners.push(sprite(prefix(tone)+'WallCornerOverlays.'+cornerNames[i],x,y));});
    }
  }
  const goals=goalsOf(def),goalViews=goals.map(g=>{
    if(stackAt(state,g.x,g.y).length)return null;
    return sprite(prefix(g.tone)+(g.kind===5?'Player':'Box')+'GoalSprite',g.x,g.y);
  });
  const pieces=[...state.pieces].sort((a,b)=>a.depth-b.depth).map(p=>sprite(prefix(p.tone)+(p.kind===5?'Player'+(p.tone===state.active?'ActiveFacingRightSprite':'InactiveSprite'):'Box'+(p.tone===state.active?'ActiveSprite':'InactiveSprite')),p.x,p.y,{},'piece-'+p.i));
  const badges=[];
  const cells=new Set(state.pieces.map(p=>p.x+','+p.y));
  if(markers)for(const cell of cells){
    const [x,y]=cell.split(',').map(Number),stack=stackAt(state,x,y),goal=goals.find(g=>g.x===x&&g.y===y);
    if(goal&&fulfilled(state,goal))badges.push(sprite('goalFulfillmentSprite',x,y));
    else if(stack.length>1){const p=stack.at(-2);badges.push(sprite(prefix(p.tone)+(p.kind===5?'Player':'Box')+'UnderSprite',x,y));}
    else if(goal)badges.push(sprite(prefix(goal.tone)+(goal.kind===5?'Player':'Box')+'GoalUnderSprite',x,y));
  }
  return <svg className={'puzzle-board '+(compact?'compact':'')} viewBox={viewBoxOverride||`-5 -5 ${w+10} ${h+10}`} role="img" aria-label={`${level.chapter===0?'欢迎':'箱即是空'} ${level.displayId} 棋盘，${def.width} 列 ${def.height} 行`}>
    <g>{walls}{corners}{!hideDynamic&&<>{goalViews}{pieces}{badges}</>}{overlay}</g>
    {annotations.map((a,i)=><g key={i} className="board-annotation"><rect x={a.x*unit+2} y={(def.height-a.y-1)*unit+2} width={unit-4} height={unit-4} rx="2"/><rect className="annotation-label" x={a.x*unit+1} y={(def.height-a.y-1)*unit-1} width="22" height="12" rx="2"/><text x={a.x*unit+12} y={(def.height-a.y-1)*unit+8} textAnchor="middle">{a.label}</text></g>)}
    {selected&&<rect className="cell-selection" x={selected[0]*unit-1} y={(def.height-selected[1]-1)*unit-1} width={unit+2} height={unit+2} rx="3"/>}
    {onSelect&&Array.from({length:def.width*def.height},(_,i)=>{
      const x=i%def.width,y=Math.floor(i/def.width),stack=stackAt(state,x,y);
      return <rect key={i} className="cell-hit" tabIndex={0} role="button" aria-label={`格子 ${x},${y}，${stack.map(p=>toneName(p.tone)+kindName(p.kind)).join('，')||'无动体'}`} x={x*unit} y={(def.height-y-1)*unit} width={unit} height={unit} onClick={()=>onSelect([x,y])} onKeyDown={e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();onSelect([x,y]);}}}/>;
    })}
  </svg>;
}
