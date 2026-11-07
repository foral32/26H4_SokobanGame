import React from 'react';
import {ArrowUpRight,Code2} from 'lucide-react';
import DATA from './data';

export function SourceExcerpt({file,anchor,lines=10,onSource,open=false,label='这一步的源码'}){
  const source=DATA.code.find(s=>s.name===file);
  if(!source)return null;
  const all=source.text.split(/\r?\n/),match=all.findIndex(line=>line.includes(anchor));
  if(match<0)return <button className="text-button" onClick={()=>onSource(file)}>查看 {file}<ArrowUpRight size={13}/></button>;
  const start=Math.max(0,match);
  return <details className="code-excerpt" open={open||undefined}>
    <summary><Code2 size={14}/><span>{label}</span><code>{file}:{start+1}</code></summary>
    <div className="excerpt-scroll"><pre>{all.slice(start,start+lines).map((line,i)=><span className={i===0?'code-focus':''} key={i}><i>{start+i+1}</i><code>{line.replace(/^\s{8}/,'')}</code></span>)}</pre></div>
    <button className="text-button" onClick={()=>onSource(file)}>完整文件<ArrowUpRight size={13}/></button>
  </details>;
}
