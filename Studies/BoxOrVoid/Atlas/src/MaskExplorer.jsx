import React,{useState} from 'react';
import {ArrowRight,Plus,Equal} from 'lucide-react';
import DATA from './data';
import {asset} from './Board';
import {SourceExcerpt} from './SourceExcerpt';
import {missingMask,cornerFlags,maskNames,directionNames,cornerNames} from './wall-tiles';

const theme=Object.fromEntries(DATA.themes[1].sprites.map(s=>[s.key,s.sprite]));
const tileName=(prefix,mask)=>theme[prefix+'WallActiveVariantSet.'+maskNames[mask]];
function SpriteTile({name,overlays=[],className=''}){
  const sprite=(spriteName,i)=>{
    if(!spriteName)return null;
    const actual=DATA.spriteMeta[spriteName]?spriteName:spriteName+'_0',m=DATA.spriteMeta[actual];
    if(!m)return null;
    return <image key={i} href={asset(actual)} x={(.5-m.w*m.pivotX)*100} y={(.5-m.h*(1-m.pivotY))*100} width={m.w*100} height={m.h*100}/>;
  };
  return <svg className={'mask-sprite '+className} viewBox="-6 -6 112 112" role="img" aria-label={name||'空格'}>{sprite(name,0)}{overlays.map((s,i)=>sprite(s,i+1))}</svg>;
}
const cells=[{label:'左上',diagonal:3},{label:'上',cardinal:0},{label:'右上',diagonal:0},{label:'左',cardinal:3},{center:true},{label:'右',cardinal:1},{label:'左下',diagonal:2},{label:'下',cardinal:2},{label:'右下',diagonal:1}];

export function MaskExplorer({onSource}){
  const [neighbors,setNeighbors]=useState([false,false,true,true]),[diagonal,setDiagonal]=useState([true,true,true,true]),[covered,setCovered]=useState(false),[cornersOn,setCornersOn]=useState(true),[prefix,setPrefix]=useState('gray');
  const connected=neighbors.map((v,i)=>v&&!(i===0&&covered)),mask=missingMask(connected),flags=cornerFlags(connected,diagonal),overlays=flags.flatMap((v,i)=>v?[theme[prefix+'WallCornerOverlays.'+cornerNames[i]]]:[]);
  const equation=connected.map((v,i)=>!v?1<<i:0).filter(Boolean).join(' + ')||'0';
  const binary=mask.toString(2).padStart(4,'0');
  const activePreset=covered?'covered':neighbors.every(v=>!v)?'single':neighbors.every(Boolean)&&!diagonal[0]&&diagonal.slice(1).every(Boolean)?'corner':!neighbors[0]&&!neighbors[1]&&neighbors[2]&&neighbors[3]?'edge':null;
  function preset(kind){
    setCovered(kind==='covered');setCornersOn(true);setPrefix(kind==='covered'?'white':'gray');
    setNeighbors(kind==='edge'?[false,false,true,true]:kind==='single'?[false,false,false,false]:[true,true,true,true]);
    setDiagonal(kind==='corner'?[false,true,true,true]:[true,true,true,true]);
  }
  function chooseMask(value){setNeighbors(Array.from({length:4},(_,i)=>!(value&(1<<i))));setDiagonal([true,true,true,true]);setCovered(false);}
  return <div id="wall-mask" className="mask-explorer">
    <div className="mask-title"><h3>墙边，怎样拼出来？</h3><span>mask = 选择贴图的 4 位整数</span></div>
    <div className="case-strip" role="group" aria-label="墙块示例"><button aria-pressed={activePreset==='edge'} onClick={()=>preset('edge')}>上右露边</button><button aria-pressed={activePreset==='single'} onClick={()=>preset('single')}>独立一格</button><button aria-pressed={activePreset==='corner'} onClick={()=>preset('corner')}>内部缺角</button><button aria-pressed={activePreset==='covered'} onClick={()=>preset('covered')}>邻墙被覆盖</button></div>
    <div className="mask-workspace"><div className="neighbor-stage"><div className="neighbor-stage-caption"><strong>点击周围格子</strong><span>中心格是计算对象</span></div><div className="neighborhood" aria-label="墙块的 3×3 邻格">{cells.map((cell,index)=>{
      if(cell.center)return <div key={index} className="neighbor-center"><span>当前墙块</span><SpriteTile name={tileName(prefix,mask)} overlays={cornersOn?overlays:[]}/><svg className="exposed-edges" viewBox="0 0 100 100" aria-hidden="true">{!connected[0]&&<path d="M6 5 H94"/>}{!connected[1]&&<path d="M95 6 V94"/>}{!connected[2]&&<path d="M6 95 H94"/>}{!connected[3]&&<path d="M5 6 V94"/>}</svg></div>;
      const isCardinal=cell.cardinal!==undefined,i=isCardinal?cell.cardinal:cell.diagonal,present=isCardinal?neighbors[i]:diagonal[i],isCovered=isCardinal&&i===0&&covered;
      return <button key={index} className={'neighbor-cell '+(isCardinal?'cardinal':'diagonal')} data-present={present} aria-label={`邻格${cell.label}：${isCovered?'墙被覆盖':present?'有同色墙':'空'}`} aria-pressed={present} onClick={()=>{if(isCardinal){setNeighbors(values=>values.map((v,n)=>n===i?!v:v));if(i===0)setCovered(false);}else setDiagonal(values=>values.map((v,n)=>n===i?!v:v));}}><span>{cell.label}</span><div className={'neighbor-fill '+prefix+(isCovered?' covered':'')}>{isCovered&&<SpriteTile name={theme.blackBoxInactiveSprite}/>}</div><small>{isCovered?'墙被覆盖':present?'同色墙':'空'}</small></button>;
    })}</div><div className="mask-legend"><i/>露出的边<span>同色且未被覆盖，才算相连</span></div></div>
      <div className="mask-calculation"><table className="mask-bit-table"><thead><tr><th>方向</th><th>相连？</th><th>缺边位</th><th>权重</th><th>贡献</th></tr></thead><tbody>{directionNames.map((name,i)=><tr key={name} className={!connected[i]?'missing':''}><th>{name}</th><td>{connected[i]?'是':'否'}</td><td><b>{connected[i]?0:1}</b></td><td><span className="bit-power">2<sup>{i}</sup> = </span>{1<<i}</td><td>{connected[i]?0:1<<i}</td></tr>)}</tbody></table><div className="binary-result"><div className="binary-digits">{['左','下','右','上'].map((name,i)=><div key={name} className={binary[i]==='1'?'set':''}><span>{name}</span><strong>{binary[i]}</strong></div>)}</div><Equal size={20}/><strong data-testid="mask-number" className="mask-number">{mask}</strong></div><div className="mask-equation"><code>{equation} = {mask}</code><ArrowRight size={15}/><strong data-testid="mask-sprite-name">{maskNames[mask]}</strong></div>
        {overlays.length>0&&<div className="corner-composition"><div><SpriteTile name={tileName(prefix,mask)}/><Plus size={14}/><SpriteTile name={overlays[0]}/><Equal size={14}/><SpriteTile name={tileName(prefix,mask)} overlays={overlays}/></div><span>对角缺口由独立角片补上，mask 仍为 {mask}。</span><label><input type="checkbox" checked={cornersOn} onChange={e=>setCornersOn(e.target.checked)}/>叠加角片{overlays.length>1?`（${overlays.length} 片）`:''}</label></div>}
        {covered&&<div className="mask-covered-note">上方虽有同色墙，但顶面被箱子覆盖：上边记为 1。</div>}
      </div></div>
    <div className="mask-more"><details className="mask-atlas"><summary>16 种组合</summary><div className="mask-atlas-grid">{maskNames.map((name,i)=><button key={name} className={mask===i?'selected':''} aria-label={`选择墙块 ${i} ${name}`} aria-pressed={mask===i} onClick={()=>chooseMask(i)}><SpriteTile name={tileName(prefix,i)}/><div><strong>{i}</strong><code>{i.toString(2).padStart(4,'0')}</code></div><span>{name}</span></button>)}</div></details><SourceExcerpt file="BoardView.cs" anchor={covered?'private bool ConnectedWall':overlays.length?'bool visible=active':'int missing=0;'} lines={overlays.length?5:4} onSource={onSource}/></div>
  </div>;
}
