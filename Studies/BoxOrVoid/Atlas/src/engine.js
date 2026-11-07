export const sameCell=(a,b)=>a.x===b.x&&a.y===b.y;
export const toneName=t=>t===1?'白色':t===2?'黑色':t===3?'灰色':'无色';
export const kindName=k=>k===5?'角色':k===2?'箱子':k===4?'角色目标':k===3?'箱目标':'墙';
export function initial(definition){
  const pieces=[];
  definition.grid.forEach((cell,index)=>{
    let depth=0;
    for(const o of cell.Objects||[]) if(o.ObjectType===2||o.ObjectType===5) pieces.push({i:pieces.length,kind:o.ObjectType,tone:o.Color,x:index%definition.width,y:Math.floor(index/definition.width),depth:++depth});
  });
  return {active:1,pieces};
}
export function fromTrace(step){return {active:step.active==='White'?1:2,pieces:step.pieces.map(p=>({...p,kind:p.kind==='Player'?5:2,tone:p.tone==='White'?1:2}))};}
export function stackAt(state,x,y){return state.pieces.filter(p=>p.x===x&&p.y===y).sort((a,b)=>a.depth-b.depth);}
export function wallAt(def,x,y){if(x<0||y<0||x>=def.width||y>=def.height)return 0;return def.grid[y*def.width+x].Objects?.find(o=>o.ObjectType===1)?.Color||0;}
export function goalsOf(def){return def.grid.flatMap((c,i)=>(c.Objects||[]).filter(o=>o.ObjectType===3||o.ObjectType===4).map(o=>({kind:o.ObjectType===3?2:5,tone:o.Color,x:i%def.width,y:Math.floor(i/def.width)})));}
export function fulfilled(state,goal){const stack=stackAt(state,goal.x,goal.y);return stack.length===1&&stack[0].kind===goal.kind&&stack[0].tone===goal.tone;}
export const pieceName=p=>p?`${toneName(p.tone)}${kindName(p.kind)} #${p.i}`:'无';
export const apply=(def,before,command)=>execute(def,before,command,false);
export const explain=(def,before,command)=>execute(def,before,command,true);

function execute(def,before,command,tracing){
  const state=structuredClone(before),checks=[],steps=[],order=[],frames=[];
  function record(event,title,detail,extra={}){
    if(tracing)steps.push({event,title,detail,phase:'plan',file:'PuzzleSession.cs',anchor:'private static bool PlanMove',state:structuredClone(state),order:[...order],frames:[...frames],...structuredClone(extra)});
  }
  function finish(kind,changed){
    const anchor=kind==='Covered'?'if (state.TopAt(player.Cell) != playerIndex)':kind==='Blocked'?'if (!PlanMove(state, playerIndex':kind==='Switched'?'if (command == Command.Switch)':'int pushed = order.Count - 1;';
    record('result',`返回 ${kind}`,changed?'计算结束，交给会话保存结果。':'位置与层序不变，本次不增加撤销记录。',{phase:'result',kind,changed,anchor,vars:{Kind:kind,Changed:changed}});
    return {state,kind,changed,checks,...(tracing?{steps}:{})};
  }
  record('command',`收到 ${commandName[command]||command}`,`当前控制${toneName(before.active)}。`,{phase:'input',anchor:'public static ActionResult Apply',vars:{command,ActiveTone:toneName(before.active)}});
  if(command==='S'){
    state.active=before.active===1?2:1;
    checks.push('保留全部对象与位置','切换当前控制色','重新投影活动 / 非活动表现');
    record('switch','只切换控制色','位置与层序保持不变。',{phase:'input',anchor:'if (command == Command.Switch)',vars:{ActiveTone:`${toneName(before.active)} → ${toneName(state.active)}`}});
    return finish('Switched',true);
  }
  const delta={U:[0,1],R:[1,0],D:[0,-1],L:[-1,0]}[command];
  if(!delta)throw new Error('Unknown movement command: '+command);
  const player=before.pieces.find(p=>p.kind===5&&p.tone===before.active);
  const playerTop=stackAt(before,player.x,player.y).at(-1);
  const uncovered=playerTop.i===player.i;
  record('eligible',uncovered?'当前角色在最上层':'当前角色被盖住',uncovered?'允许继续检查前方格。':'直接返回 Covered，不进入推箱计算。',{phase:'eligible',piece:player,target:[player.x,player.y],anchor:'int playerIndex = state.PlayerIndex',vars:{player:pieceName(player),TopAt:pieceName(playerTop),'TopAt == player':uncovered}});
  if(!uncovered){checks.push('检查当前角色是否位于顶层','角色上方仍有其他物体','拒绝移动；仍可切换控制色');return finish('Covered',false);}
  checks.push('当前角色位于顶层');
  state.pieces[player.i].facing=command;
  function plan(index){
    frames.push(index);
    const p=before.pieces[index],x=p.x+delta[0],y=p.y+delta[1];
    const top=stackAt(before,x,y).at(-1),tone=top?top.tone:wallAt(def,x,y);
    const surface=top?pieceName(top):tone?toneName(tone)+'墙':'空格';
    record('inspect',`${pieceName(p)} 看前方 (${x}, ${y})`,`读到的表面：${surface}。`,{piece:p,target:[x,y],anchor:'var target = piece.Cell + direction;',vars:{piece:pieceName(p),target:`(${x}, ${y})`,top:top?'#'+top.i:'无动体',color:toneName(tone)}});
    if(tone===3){
      checks.push(`(${x},${y}) 为灰墙 → 拒绝`);
      record('reject','灰墙，拒绝整次移动','这一层返回 false。',{piece:p,target:[x,y],anchor:'if (color == Tone.Gray)',vars:{'color == Gray':true,return:false}});
      frames.pop();return false;
    }
    if(tone===p.tone){
      if(!top||top.kind!==2){
        checks.push(`(${x},${y}) 为同色实体 → 拒绝`);
        record('reject','同色表面不能直接进入','同色墙或角色不属于可推动箱子。',{piece:p,target:[x,y],anchor:'if (color == piece.Tone)',vars:{'color == piece.Tone':true,'top is Box':false,return:false}});
        frames.pop();return false;
      }
      checks.push(`(${x},${y}) 为同色箱 → 检查后方依赖`);
      record('dependency',`同色箱 #${top.i}：先问它能不能动`,`递归调用 PlanMove(#${top.i})，当前物体先等待。`,{piece:p,target:[x,y],anchor:'if (!PlanMove(state, top, direction, order))',vars:{'color == piece.Tone':true,'top is Box':true,nextCall:`PlanMove(#${top.i})`}});
      if(!plan(top.i)){
        record('cancel','后方失败，前方也返回 false','放弃整个计划，不逐个试推。',{piece:p,target:[x,y],anchor:'if (!PlanMove(state, top, direction, order))',vars:{dependency:false,return:false}});
        frames.pop();return false;
      }
    }else{
      checks.push(`(${x},${y}) ${tone===0?'为空格':'顶面为异色'} → 可进入`);
      record('pass',tone===0?'空格，可以进入':'异色表面，可以进入',top?'下层对象保留，进入者叠在它上方。':'无需推动前方物体。',{piece:p,target:[x,y],anchor:'if (color == piece.Tone)',vars:{'color == Gray':false,'color == piece.Tone':false,return:true}});
    }
    order.push(index);
    record('planned',`#${index} 加入移动计划`,'这里只记录编号，尚未改变位置。',{piece:p,target:[x,y],anchor:'order.Add(index);',vars:{order:order.map(i=>'#'+i).join(' → '),return:true}});
    frames.pop();return true;
  }
  if(!plan(player.i))return finish('Blocked',false);
  record('ready','整条依赖都通过，开始提交','从最远处提交；全部提交完成后，才对外返回新局面。',{phase:'commit',anchor:'foreach (int index in order)',vars:{order:order.map(i=>'#'+i).join(' → ')}});
  for(const i of order){
    const p=state.pieces[i],previous=structuredClone(p);
    p.x+=delta[0];p.y+=delta[1];
    p.depth=Math.max(0,...state.pieces.filter(q=>q.i!==i&&sameCell(q,p)).map(q=>q.depth))+1;
    record('commit',`提交 ${pieceName(p)}`,`(${previous.x}, ${previous.y}) → (${p.x}, ${p.y})`,{phase:'commit',piece:previous,target:[p.x,p.y],anchor:'var piece = next.Pieces[index];',vars:{piece:'#'+i,Cell:`(${previous.x},${previous.y}) → (${p.x},${p.y})`,Depth:`${previous.depth} → ${p.depth}`}});
  }
  checks.push(`从远到近提交 ${order.length} 件物体`);
  return finish(order.length>1?'Pushed':'Moved',true);
}
export const actionLabel={Initial:'初始状态',Moved:'移动成功',Pushed:'推动成功',Switched:'控制色切换',Blocked:'同色实体或灰墙阻挡',Covered:'角色被覆盖，不能移动',Undone:'已撤回一步',Reset:'已恢复起点'};
export const commandName={U:'上',R:'右',D:'下',L:'左',S:'切换'};
