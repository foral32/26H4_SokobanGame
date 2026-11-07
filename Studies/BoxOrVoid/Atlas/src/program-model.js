import {apply,initial,explain,fromTrace,goalsOf,fulfilled} from './engine.js';

export const executionCases=[
  {id:'chain',name:'连续推箱',level:'n_box_3',step:4,command:'R'},
  {id:'blocked',name:'链尾受阻',level:'n_box_1',step:6,command:'L'},
  {id:'bridge',name:'异色作路',level:'n_tut_2',step:5,command:'R'},
  {id:'covered',name:'角色被覆盖',level:'n_box_7',step:17,seed:'S',command:'R'},
  {id:'goal',name:'目标失效',level:'n_box_4',step:16,seed:'S',command:'R'}
];

export function executionTrace(example,level){
  let before=fromTrace(level.trace[example.step]);
  if(example.seed)before=apply(level.definition,before,example.seed).state;
  const result=explain(level.definition,before,example.command);
  const base={state:before,frames:[],order:[],vars:{},phase:'input'};
  const after={...base,state:result.state,order:result.steps.at(-1).order,phase:'feedback'};
  const goals=goalsOf(level.definition);
  const completed=state=>goals.filter(g=>fulfilled(state,g)).length;
  return {before,result,steps:[
    {...base,event:'poll',title:'按键变成 Command',detail:'Update 中的输入轮询得到一个方向指令。',file:'PuzzleApp.cs',anchor:'var action=input.Poll',vars:{Page:'Playing',command:example.command}},
    ...result.steps,
    {...after,event:'history',title:result.changed?'保存操作前的完整局面':'不增加撤销记录',detail:result.changed?'Session 将 before 压入历史栈，再替换当前 State。':'碰墙可能改变朝向，但不占撤销步数。',file:'PuzzleSession.cs',anchor:'var result = PuzzleRules.Apply',vars:{Changed:result.changed,'本次新增历史':result.changed?1:0}},
    {...after,event:'present',title:'把 Before / After 交给画面',detail:'BoardView 在两份位置之间插值，动画不再修改棋盘逻辑。',file:'BoardView.cs',anchor:'public void Present(ActionResult',vars:{'动画时长':'0.10 s',result:result.kind}},
    {...after,event:'goals',title:`目标完成数 ${completed(before)} → ${completed(result.state)}`,detail:'目标格必须恰好有一个动体，且类型、颜色都匹配。',file:'PuzzleState.cs',anchor:'public bool GoalFulfilled',vars:{before:`${completed(before)} / ${goals.length}`,after:`${completed(result.state)} / ${goals.length}`}},
    {...after,event:'frame',title:'回到下一帧',detail:'App 继续轮询输入；BoardView.Update 推进动画。',file:'PuzzleApp.cs',anchor:'private void Update()',vars:{'下一次移动':'动画结束后','撤销 / 重置':'先进入 HistoryActionQueue'}}
  ]};
}

export function newSession(definition){return {state:initial(definition),history:[],checkpoint:null,last:'Initial'};}
export function sessionCommand(definition,before,command){
  const next=structuredClone(before);
  if(command==='Z'){
    if(next.history.length)next.state=next.history.pop();
    else if(next.checkpoint){next.state=next.checkpoint.state;next.history=next.checkpoint.history;next.checkpoint=null;}
    else return {...next,last:'NoChange'};
    return {...next,last:'Undone'};
  }
  if(command==='RESET')return {...next,checkpoint:{state:next.state,history:next.history},state:initial(definition),history:[],last:'Reset'};
  const result=apply(definition,next.state,command);
  if(result.changed)next.history.push(next.state);
  return {...next,state:result.state,last:result.kind};
}
