import React,{useMemo,useState} from 'react';
import {ArrowRight,ArrowUpRight,ChevronLeft,ChevronRight,Play,RotateCcw,Undo2,Layers,Network,Database,Code2} from 'lucide-react';
import DATA from './data';
import {Board} from './Board';
import {SourceExcerpt} from './SourceExcerpt';
import {bootStages,components} from './program-content';
import {executionCases,executionTrace,newSession,sessionCommand} from './program-model';
import {toneName,kindName,stackAt} from './engine';

const getLevel=id=>DATA.levels.find(l=>l.id===id);
const levelName=l=>(l.chapter===0?'欢迎':'箱即是空')+' / '+l.displayId;
const phaseNames={input:'输入',eligible:'行动权',plan:'递归计划',commit:'提交',result:'返回',feedback:'反馈'};
const typeTitle=type=>type==='MonoBehaviour'?'挂在游戏对象上，由 Unity 调用生命周期':type==='ScriptableObject'?'项目里的资源资产，通过引用读取':'不挂在游戏对象上，由其他 C# 代码创建或调用';

const variableLabels={piece:'当前物体',player:'当前角色',target:'前方格',top:'顶层动体',color:'表面颜色',TopAt:'角色所在格的顶层',ActiveTone:'控制色',command:'指令',order:'移动顺序',nextCall:'下一层调用',return:'返回值',Kind:'结果类型',Changed:'计入历史？',before:'操作前',after:'操作后',Cell:'格子位置',Depth:'层序'};
function Variables({values}){return <dl className="execution-vars">{Object.entries(values||{}).map(([key,value])=><div key={key}><dt title={key}>{variableLabels[key]||key}</dt><dd>{typeof value==='boolean'?(value?'true':'false'):String(value)}</dd></div>)}</dl>;}

function Startup({onSource,onMove}){
  const [stage,setStage]=useState(0),current=bootStages[stage];
  const objects=[['PuzzleApp','挂在入口对象上的组件',0],['StudyMenu / Transition','序列化的 UI 组件引用',0],['StudyAssets / Camera','资产 / 场景相机引用',0],['BoardView','Start 添加组件',2],['PuzzleHud / PuzzleAudio','Start 添加组件',2],['ProgressStore','Start 创建普通 C# 对象',2],['PuzzleSession','选关后 new 普通 C# 对象',4]];
  return <div className="startup-workbench">
    <div className="startup-map" aria-label="Unity 启动流程">{bootStages.map((step,i)=><button key={step.name} className={'startup-node '+(stage===i?'selected':stage>i?'visited':'')} aria-pressed={stage===i} onClick={()=>setStage(i)}><span className="boot-index">{i===0?<Play size={13}/>:i+1}</span><strong>{step.name}</strong><code>{step.call}</code><ChevronRight size={14}/></button>)}</div>
    <div className="startup-detail">
      <div className="runtime-tree" aria-label="运行对象的创建时机"><div className="runtime-scene"><Layers size={15}/><strong>BoxOrVoid.unity</strong></div><div className="entry-object">入口对象 <strong>Box or Void Study</strong></div><div className="runtime-tree-rows">{objects.map(([name,note,at])=><div key={name} className={(stage>=at?'exists':'pending')+(stage===at?' just-created':'')}><i/><div><strong>{name}</strong><span>{stage>=at?note:'尚未创建'}</span></div>{stage>=at?<span className="tree-check">已存在</span>:<span className="tree-check">—</span>}</div>)}</div></div>
      <div className="startup-explanation"><div className="row-between"><code className="execution-owner">{current.call}</code><span className="page-state">{current.page}</span></div><h3>{current.name}</h3><p className="step-conclusion">{current.detail}</p><div className="io-flow"><div><span>输入</span><strong>{current.input}</strong></div><ArrowRight size={18}/><div><span>输出</span><strong>{current.output}</strong></div></div>
        {stage===4&&<div className="data-pipeline"><span>JSON</span><ArrowRight size={13}/><span>Definition</span><ArrowRight size={13}/><span>State</span><ArrowRight size={13}/><span>Sprite</span></div>}
        {stage>=5&&<button className="text-button" onClick={onMove}>跟进一次移动<ArrowUpRight size={14}/></button>}
        <SourceExcerpt file={current.file} anchor={current.anchor} onSource={onSource} lines={stage===2?14:10}/>
        <div className="step-navigation"><button className="button secondary" onClick={()=>setStage(s=>Math.max(0,s-1))} disabled={stage===0}><ChevronLeft size={15}/>上一步</button><span>{stage+1} / {bootStages.length}</span><button className="button" onClick={()=>setStage(s=>s===7?5:s+1)}>{stage===7?'下一帧':'下一步'}{stage===7?<RotateCcw size={15}/>:<ChevronRight size={15}/>}</button></div>
      </div>
    </div>
  </div>;
}

function MoveExecution({onSource}){
  const [caseIndex,setCaseIndex]=useState(0),[cursor,setCursor]=useState(3),[manualCell,setManualCell]=useState(null);
  const example=executionCases[caseIndex],level=getLevel(example.level);
  const trace=useMemo(()=>executionTrace(example,level),[example,level]);
  const step=trace.steps[Math.min(cursor,trace.steps.length-1)],selected=manualCell||step.target;
  const phases=Object.keys(phaseNames).filter(p=>trace.steps.some(s=>s.phase===p));
  const selectStep=index=>{setCursor(index);setManualCell(null);};
  const displayPieces=step.state.pieces;
  const annotations=displayPieces.filter(p=>trace.result.steps.at(-1).order.includes(p.i)||p.i===step.piece?.i).map(p=>({x:p.x,y:p.y,label:'#'+p.i}));
  const commitsStarted=trace.steps.slice(0,cursor+1).some(s=>s.event==='commit');
  const selectedStack=selected?stackAt(step.state,...selected):[];
  const branch=step.event==='dependency'?'push':step.event==='pass'?'pass':step.event==='reject'?'stop':null;
  return <div className="move-execution">
    <div className="case-strip" role="group" aria-label="程序执行案例">{executionCases.map((c,i)=><button key={c.id} aria-pressed={i===caseIndex} onClick={()=>{setCaseIndex(i);selectStep(0);}}>{c.name}</button>)}</div>
    <div className="execution-phases" aria-label="本次操作的阶段">{phases.map(p=><button className={step.phase===p?'active':''} aria-pressed={step.phase===p} key={p} onClick={()=>selectStep(trace.steps.findIndex(s=>s.phase===p))}>{phaseNames[p]}<ChevronRight size={12}/></button>)}</div>
    <div className="execution-workspace">
      <div className="execution-board-panel"><div className="board-caption"><strong>{levelName(level)}</strong><span>第 {example.step} 步局面 · {example.command==='R'?'向右':'向左'}</span></div><div className="board-surface execution-board"><Board level={level} state={step.state} selected={selected} onSelect={setManualCell} annotations={annotations}/></div><div className="execution-board-note"><span className="tone-dot" data-tone={step.state.active}/>{toneName(step.state.active)}控制<span>{commitsStarted?'提交内部步骤':'位置尚未改变'}</span></div>
        {manualCell&&<div className="cell-peek"><strong>({manualCell.join(', ')})</strong><span>{selectedStack.length?[...selectedStack].reverse().map(p=>`${toneName(p.tone)}${kindName(p.kind)} #${p.i}`).join(' ↓ '):'无动体'}</span></div>}
        <details className="piece-data"><summary>对象数据</summary><table><thead><tr><th>对象</th><th>格子</th><th>Depth</th></tr></thead><tbody>{displayPieces.map(p=><tr key={p.i}><td>{toneName(p.tone)}{kindName(p.kind)} #{p.i}</td><td>({p.x}, {p.y})</td><td>{p.depth}</td></tr>)}</tbody></table></details>
      </div>
      <div className="execution-decision" aria-live="polite"><div className="row-between"><code className="execution-owner">{step.event==='poll'?'PuzzleApp.Update':step.phase==='feedback'?step.file.replace('.cs',''):step.phase==='plan'?'PuzzleRules.PlanMove':'PuzzleRules.Apply'}</code><span className="step-counter">{cursor+1} / {trace.steps.length}</span></div><h3>{step.title}</h3><p className="step-conclusion">{step.detail}</p><Variables values={step.vars}/>
        {branch&&<div className="branch-map" aria-label="当前规则分支"><span className={branch==='pass'?'chosen':''}>空格 / 异色<ArrowRight size={12}/>进入</span><span className={branch==='push'?'chosen':''}>同色箱<ArrowRight size={12}/>递归</span><span className={branch==='stop'?'chosen':''}>灰墙 / 同色障碍<ArrowRight size={12}/>拒绝</span></div>}
        {step.frames.length>0&&<div className="call-stack"><span>调用栈</span><div>{step.frames.map((id,i)=><React.Fragment key={id}>{i>0&&<ChevronRight size={12}/>}<code className={i===step.frames.length-1?'current':''}>PlanMove(#{id})</code></React.Fragment>)}</div></div>}
        <div className="move-plan"><span>移动计划</span><div>{step.order.length?step.order.map((id,i)=><React.Fragment key={id}>{i>0&&<ArrowRight size={12}/>}<span className="plan-piece" data-committed={trace.steps.slice(0,cursor+1).some(s=>s.event==='commit'&&s.piece.i===id)}>#{id}</span></React.Fragment>):<span className="plan-empty">空</span>}</div></div>
        <SourceExcerpt file={step.file} anchor={step.anchor} onSource={onSource} lines={9}/>
      </div>
    </div>
    <div className="execution-controls"><button className="icon-button" aria-label="重新单步执行" onClick={()=>selectStep(0)}><RotateCcw size={16}/></button><input aria-label="单步执行进度" type="range" min="0" max={trace.steps.length-1} value={cursor} onChange={e=>selectStep(Number(e.target.value))}/><button className="button secondary" aria-label="执行上一步" disabled={cursor===0} onClick={()=>selectStep(cursor-1)}><ChevronLeft size={15}/></button><button className="button" aria-label="执行下一步" disabled={cursor===trace.steps.length-1} onClick={()=>selectStep(cursor+1)}>下一步<ChevronRight size={15}/></button></div>
  </div>;
}

const stateSummary=state=>`${toneName(state.active)}控制 · `+state.pieces.filter(p=>p.kind===5).map(p=>`${p.tone===1?'白':'黑'}(${p.x},${p.y})`).join(' / ');
function HistoryLab({onSource}){
  const level=getLevel('n_tut_2');
  const [session,setSession]=useState(()=>newSession(level.definition)),[preview,setPreview]=useState(null);
  const act=command=>{setSession(s=>sessionCommand(level.definition,s,command));setPreview(null);};
  const messages={Initial:'移动一次，看看历史栈里存了什么。',Moved:'保存操作前的局面，再替换当前局面。',Pushed:'整次推箱，只保存一份操作前局面。',Switched:'控制色属于局面，切换也保存历史。',Blocked:'位置未变，不增加历史。',Covered:'角色被覆盖，不增加历史。',Undone:'从历史恢复整份局面。',Reset:'清空普通历史，同时保存重置前的检查点。',NoChange:'没有可撤销的记录。'};
  return <div className="history-lab"><div className="history-instruction"><h3>{messages[session.last]}</h3><button className="text-button" onClick={()=>{setSession(newSession(level.definition));setPreview(null);}}>清空演示<RotateCcw size={13}/></button></div>
    <div className="history-workspace"><div><div className="board-caption"><strong>{preview?'历史预览':'当前局面'}</strong><span>{levelName(level)}</span>{preview&&<button className="text-button" onClick={()=>setPreview(null)}>返回当前</button>}</div><div className="board-surface history-board"><Board level={level} state={preview||session.state}/></div><div className="history-controls">{[['U','↑'],['L','←'],['D','↓'],['R','→'],['S','切换']].map(([c,label])=><button className="button secondary" key={c} aria-label={'历史演示：'+label} onClick={()=>act(c)}>{label}</button>)}<button className="button" aria-label="历史演示：撤销" onClick={()=>act('Z')}><Undo2 size={14}/>撤销</button><button className="button secondary" aria-label="历史演示：重置" onClick={()=>act('RESET')}>重置</button></div></div>
      <div className="history-state-panels"><div className="state-slot current-slot"><div><Database size={16}/><strong>State</strong><span>当前局面</span></div><code>{stateSummary(session.state)}</code></div><div className="state-slot history-slot"><div><Layers size={16}/><strong>history</strong><span data-testid="history-count">{session.history.length} 份快照</span></div><div className="snapshot-stack">{session.history.length?[...session.history].reverse().slice(0,4).map((s,i)=><button key={session.history.length-i} onClick={()=>setPreview(s)}><span>{i===0?'栈顶':'−'+i}</span><code>{stateSummary(s)}</code><ChevronRight size={12}/></button>):<span className="empty-snapshot">空栈</span>}</div></div><div className={'state-slot checkpoint-slot '+(session.checkpoint?'saved':'')}><div><RotateCcw size={16}/><strong>resetCheckpoint</strong><span>{session.checkpoint?'已保存':'空'}</span></div>{session.checkpoint?<button onClick={()=>setPreview(session.checkpoint.state)}><code>{stateSummary(session.checkpoint.state)}</code><span>原历史 {session.checkpoint.history.length} 份</span></button>:<span className="empty-snapshot">重置时才保存</span>}</div><SourceExcerpt file="PuzzleSession.cs" anchor={session.last==='Reset'?'if (command == Command.Reset)':session.last==='Undone'?'if (command == Command.Undo)':'var result = PuzzleRules.Apply'} onSource={onSource}/></div>
    </div>
  </div>;
}

function ComponentMap({onSource}){
  const [name,setName]=useState('PuzzleApp');const current=components.find(c=>c.name===name);
  return <div className="component-map"><div className="component-columns">{[['data','数据与输入'],['logic','流程与规则'],['view','表现']].map(([group,title])=><div className="component-column" key={group}><h4>{title}</h4>{components.filter(c=>c.group===group).map(c=><button key={c.name} className={name===c.name?'selected':''} aria-pressed={name===c.name} onClick={()=>setName(c.name)}><strong>{c.name}</strong><span>{c.role}</span><small title={typeTitle(c.type)}>{c.type}</small></button>)}</div>)}</div><div className="support-components">{components.filter(c=>c.group==='support').map(c=><button key={c.name} aria-pressed={name===c.name} onClick={()=>setName(c.name)}>{c.name}<span>{c.role}</span></button>)}</div><div className="component-detail"><div className="row-between"><h3>{current.name}</h3><span title={typeTitle(current.type)} className="page-state">{current.type}</span></div><div className="component-contract"><div><span>输入</span><strong>{current.input}</strong></div><ArrowRight size={17}/><div><span>输出</span><strong>{current.output}</strong></div><div><span>持有的数据</span><strong>{current.owns}</strong></div></div><SourceExcerpt file={current.file} anchor={current.anchor} onSource={onSource}/></div></div>;
}

export function ProgramExplorer({onSource}){
  const [tab,setTab]=useState('startup');
  return <div className="program-explorer"><div className="program-tabs" role="tablist" aria-label="程序拆解"><button role="tab" aria-selected={tab==='startup'} aria-controls="program-panel" onClick={()=>setTab('startup')}><Play size={15}/>从运行开始</button><button role="tab" aria-selected={tab==='move'} aria-controls="program-panel" onClick={()=>setTab('move')}><Network size={16}/>一步怎么计算</button><button role="tab" aria-selected={tab==='history'} aria-controls="program-panel" onClick={()=>setTab('history')}><Undo2 size={15}/>状态与撤销</button><button role="tab" aria-selected={tab==='components'} aria-controls="program-panel" onClick={()=>setTab('components')}><Code2 size={15}/>关键组件</button></div><div id="program-panel" role="tabpanel">{tab==='startup'?<Startup onSource={onSource} onMove={()=>setTab('move')}/>:tab==='move'?<MoveExecution onSource={onSource}/>:tab==='history'?<HistoryLab onSource={onSource}/>:<ComponentMap onSource={onSource}/>}</div></div>;
}
