'use strict';
// Review-only UI prototype. Gameplay is intentionally delegated to the future Unity integration.
const A=window.REVIEW_ASSETS;
const $=selector=>document.querySelector(selector), $$=selector=>[...document.querySelectorAll(selector)];
const copy=value=>JSON.parse(JSON.stringify(value));
const labels={1:'墙',2:'箱子',3:'箱子目标',4:'角色目标',5:'角色'}, colors={1:'白',2:'黑',3:'灰'};
const glyph=name=>`<svg class="glyph" viewBox="0 0 32 32" aria-hidden="true"><use href="#i-${name}"/></svg>`;
let data=copy(A.levels[0]),levelName='交错',chapter=1,tone=1,kind=2,view=1,tool='place',page='edit';
let selected=-1,layer=-1,stackOpen=false,gridOn=false,zoom=1,scale=1,history=[],future=[],dirty=true;
let down=false,stroke=null,visited=new Set(),startCell=-1,hoverCell=-1,libraryIndex=0,busy=false;
let resizeW=9,resizeH=7,anchor=4,newSize=1,allowRepeat=false;
const sizeOptions=[[7,7,'小小一关'],[9,7,'刚刚好'],[13,9,'多一点空间']];
const initialBox=data.grid.findIndex(c=>c.Objects.some(o=>o.ObjectType===2));
let whitePlayer;
data.grid.forEach(c=>{const at=c.Objects.findIndex(o=>o.ObjectType===5&&o.Color===1);if(at>=0)whitePlayer=c.Objects.splice(at,1)[0];});
if(initialBox>=0&&whitePlayer){const stack=data.grid[initialBox].Objects,at=stack.findIndex(o=>o.ObjectType===2);stack[at].Color=2;stack.splice(at,0,whitePlayer);}
const initialExample=copy(data);
let savedSnapshot={data:copy(data),name:levelName,chapter},activeLevel=0;
const libraryItems=[
  {snapshot:snapshot(),saved:copy(savedSnapshot),dirty:true},
  {snapshot:{data:copy(A.levels[1]),name:'第一步',chapter:0},dirty:false},
  {snapshot:{data:copy(A.levels[2]),name:'留一个出口',chapter:1},dirty:false}
];
libraryItems.forEach(item=>{item.saved??=copy(item.snapshot);item.history=[];item.future=[];});
const escapeText=text=>String(text).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const theme=()=>Object.fromEntries(A.themes[chapter].sprites.map(s=>[s.key,s.sprite]));
const corners='linear-gradient(#76bd38,#76bd38) left top/22% 5px no-repeat,linear-gradient(#76bd38,#76bd38) left top/5px 22% no-repeat,linear-gradient(#76bd38,#76bd38) right top/22% 5px no-repeat,linear-gradient(#76bd38,#76bd38) right top/5px 22% no-repeat,linear-gradient(#76bd38,#76bd38) left bottom/22% 5px no-repeat,linear-gradient(#76bd38,#76bd38) left bottom/5px 22% no-repeat,linear-gradient(#76bd38,#76bd38) right bottom/22% 5px no-repeat,linear-gradient(#76bd38,#76bd38) right bottom/5px 22% no-repeat';
document.documentElement.style.setProperty('--corners',corners);
$$('[data-glyph]').forEach(node=>node.innerHTML=glyph(node.dataset.glyph));

function sprite(o,palette=false){
  const prefix=o.Color===1?'white':o.Color===2?'black':'gray',active=palette||o.Color===view;
  let key;
  if(o.ObjectType===1)key=prefix+(palette?'WallActiveVariantSet.Uprightdownleft':'WallInactiveSprite');
  else if(o.ObjectType===2)key=prefix+'Box'+(active?'Active':'Inactive')+'Sprite';
  else if(o.ObjectType===5)key=prefix+'Player'+(active?'ActiveFacingRight':'Inactive')+'Sprite';
  else key=prefix+(o.ObjectType===3?'Box':'Player')+'GoalSprite';
  let name=theme()[key];
  if(!name&&o.ObjectType===3)name=o.Color===1?'Goal_box_coloredwhite':'Goal_box_black';
  if(!name&&o.ObjectType===4)name=o.Color===1?'Goal_player_coloredwhite':'Goal_player_black';
  return A.sprites[name]||'';
}
const objectImage=(o,cls='',palette=false)=>`<img src="${sprite(o,palette)}" class="${cls}" alt="">`;
function wallImage(o,index,level){
  if(index===undefined||o.Color!==3&&o.Color!==view)return objectImage(o);
  const x=index%level.width,y=Math.floor(index/level.width);
  const names=['None','Up','Right','UpRight','Down','UpDown','RightDown','UpRightDown','Left','UpLeft','RightLeft','UpRightLeft','DownLeft','UpDownLeft','RightDownLeft','Uprightdownleft'];
  let mask=0;
  [[0,1],[1,0],[0,-1],[-1,0]].forEach(([dx,dy],n)=>{
    const nx=x+dx,ny=y+dy,connected=nx>=0&&ny>=0&&nx<level.width&&ny<level.height&&level.grid[ny*level.width+nx].Objects.some(v=>v.ObjectType===1&&(v.Color===o.Color||v.Color===3));
    if(!connected)mask|=1<<n;
  });
  const prefix=o.Color===1?'white':o.Color===2?'black':'gray';
  return `<img src="${A.sprites[theme()[prefix+'WallActiveVariantSet.'+names[mask]]]||sprite(o)}" alt="">`;
}
function cellArt(cell,index,level){
  const objects=cell.Objects,pieces=objects.filter(o=>[2,5].includes(o.ObjectType));
  let html=objects.filter(o=>o.ObjectType===1).map(o=>wallImage(o,index,level)).join('');
  if(pieces.length)html+=objectImage(pieces.at(-1));
  html+=objects.filter(o=>[3,4].includes(o.ObjectType)).map(o=>objectImage(o)).join('');
  if(pieces.length>1)html+=objectImage(pieces.at(-2),'covered');
  return html;
}
function boardContent(level,interactive=true){
  let html='';
  for(let y=level.height-1;y>=0;y--)for(let x=0;x<level.width;x++){
    const i=y*level.width+x;
    html+=`<${interactive?'button':'div'} class="cell ${interactive&&stackOpen&&i===selected?'selected':''}" ${interactive?`data-cell="${i}" aria-label="格子 ${x+1}, ${y+1}"`:''}>${cellArt(level.grid[i],i,level)}</${interactive?'button':'div'}>`;
  }
  return html;
}
function miniBoard(level,max=156,bounds=null){
  const size=Math.min(max/level.width,max/level.height);let cells='';
  for(let y=level.height-1;y>=0;y--)for(let x=0;x<level.width;x++){
    const index=y*level.width+x,cut=bounds&&(x+bounds.dx<0||x+bounds.dx>=bounds.width||y+bounds.dy<0||y+bounds.dy>=bounds.height);
    cells+=`<div class="mini-cell">${cellArt(level.grid[index],index,level)}${cut?'<i class="crop"></i>':''}</div>`;
  }
  return `<div class="mini-board" style="--mini:${size}px;grid-template-columns:repeat(${level.width},${size}px)">${cells}</div>`;
}
function fitViewport(){
  scale=Math.min(innerWidth/1280,(innerHeight-38)/720);
  $('#game').style.transform=`scale(${scale})`;
}
function renderBoard(){
  const maxH=page==='play'?465:382;
  const size=Math.max(12,Math.min(page==='play'?66:58,880/data.width,maxH/data.height))*zoom;
  const board=page==='play'?$('#play-board'):$('#board');
  board.style.setProperty('--cell',size+'px');board.style.gridTemplateColumns=`repeat(${data.width},${size}px)`;
  board.innerHTML=boardContent(data,page!=='play');board.classList.toggle('grid-visible',gridOn);
  $('#hover-object').style.setProperty('--cell',size+'px');
  $('#game').classList.toggle('stack-open',stackOpen&&page==='edit');
  $('#stack-popover').hidden=!stackOpen;
  if(stackOpen&&page==='edit')renderStack();
}
function renderPalette(){
  const entries=[{kind:1,label:'墙'},{kind:2,label:'箱子'},{kind:5,label:'角色'},{kind:3,label:'箱子目标'},{kind:4,label:'角色目标'},{kind:'gray',label:'灰墙'}];
  $('#palette').innerHTML=entries.map(e=>`<button class="object-slot ${kind===e.kind?'selected':''}" data-kind="${e.kind}" aria-label="拿起${e.kind==='gray'?'灰墙':colors[tone]+e.label}"><span class="slot-art">${objectImage({ObjectType:e.kind==='gray'?1:e.kind,Color:e.kind==='gray'?3:tone},'',true)}</span><span>${e.label}</span></button>`).join('');
  $$('[data-tone]').forEach(b=>b.classList.toggle('chosen',+b.dataset.tone===tone));
  $$('[data-tool]').forEach(b=>b.classList.toggle('selected',b.dataset.tool===tool));
  $('#view-label').innerHTML=(view===1?'白色视角':'黑色视角')+' '+glyph('switch');
  $('#grid-toggle').classList.toggle('on',gridOn);
  const objectName=kind==='gray'?'灰墙':colors[tone]+labels[kind];
  const messages={place:['拿起'+objectName,'点一下放下，按住连续摆放'],look:['点一格，拆开看看','被盖住的角色和箱子，也能单独选中'],erase:['擦掉'+objectName,'只移除这一种物件，其他内容留在原处'],line:['拉出一条'+(kind==='gray'?'灰墙':labels[kind]),'按下起点，拖到终点后松开'],rect:['围出一个空间','拖出矩形，松开后摆好边框'],fill:['铺满这片地方','填充相连的同类地形，物体保留'],eyedrop:['借用一个物件','点击棋盘上的物件，拿起同样的笔刷']};
  const message=messages[tool]||messages.place;
  $('#making-hint').innerHTML=`<strong>${message[0]}</strong><span>${message[1]}</span>`;
}
function renderHistory(){
  $('#undo').disabled=history.length===0;$('#redo').disabled=future.length===0;
  $('#save-state').textContent=dirty?'草稿 · 未保存':'已保存';$('#save-state').classList.toggle('saved',!dirty);
  $('#level-name').textContent=levelName;$('#play-name').textContent=levelName;
}
function snapshot(){return {data:copy(data),name:levelName,chapter};}
function restore(s){data=copy(s.data);levelName=s.name;chapter=s.chapter;selected=Math.min(selected,data.grid.length-1);layer=-1;dirty=true;renderHistory();renderBoard();renderPalette();}
function remember(before=snapshot()){history.push(before);future=[];dirty=true;renderHistory();}
function changed(before){return JSON.stringify(before)!==JSON.stringify(snapshot());}
function toast(message){$('#toast').textContent=message;$('#toast').classList.add('visible');clearTimeout(toast.timer);toast.timer=setTimeout(()=>$('#toast').classList.remove('visible'),2000);}
function closeStack(){stackOpen=false;renderBoard();}
function openStack(index){
  selected=index;layer=data.grid[index].Objects.findLastIndex(o=>[2,5].includes(o.ObjectType));
  if(layer<0)layer=data.grid[index].Objects.length-1;
  tool='look';stackOpen=true;$('#hover-object').style.display='none';renderBoard();renderPalette();
}
function renderStack(){
  if(selected<0||!data.grid[selected])return;
  const objects=data.grid[selected].Objects,pieces=objects.map((o,i)=>({o,i})).filter(v=>[2,5].includes(v.o.ObjectType)).reverse();
  $('#stack-objects').innerHTML=pieces.length?pieces.map(({o,i},n)=>`<button class="stack-piece ${i===layer?'selected':''}" data-layer="${i}">${objectImage(o,'',true)}<div><strong>${colors[o.Color]+labels[o.ObjectType]}</strong><small>${n===0?'上面':'下面 · 被盖住'}</small></div></button>`).join(''):'<p class="stack-note">这里还没有箱子或角色</p>';
  $('#stack-base').innerHTML=objects.map((o,i)=>!([2,5].includes(o.ObjectType))?`<button data-layer="${i}" title="选中${colors[o.Color]+labels[o.ObjectType]}">${objectImage(o,'',true)} ${o.ObjectType===1?'底下是':'这里的'}${colors[o.Color]+labels[o.ObjectType]}</button>`:'').join('');
  if(!$('#stack-base').innerHTML)$('#stack-base').textContent='底下是空地';
  $('#stack-note').textContent=layer>=0?`选中了${colors[objects[layer].Color]+labels[objects[layer].ObjectType]}。拿走后仍可撤回。`:'这一格是空的，拿起物件就能继续摆放。';
  $('[data-action="remove-layer"]').disabled=layer<0;
  $('[data-action="lift"]').disabled=layer<0||!([2,5].includes(objects[layer].ObjectType))||pieces[0]?.i===layer;
}
function applyObject(index){
  const actualKind=kind==='gray'?1:kind,actualTone=kind==='gray'?3:tone;
  const objects=data.grid[index].Objects;
  if(tool==='erase'){
    const at=objects.findLastIndex(o=>o.ObjectType===actualKind&&o.Color===actualTone);if(at>=0)objects.splice(at,1);return;
  }
  if(actualTone===3&&objects.some(o=>o.ObjectType!==1)){toast('这里有物件，先拿走再铺灰墙');return;}
  if(actualKind!==1&&objects.some(o=>o.ObjectType===1&&o.Color===3)){toast('灰墙占着这里，换个位置吧');return;}
  if(!allowRepeat&&objects.some(o=>o.ObjectType===actualKind&&o.Color===actualTone))return;
  if(actualKind===5)data.grid.forEach(c=>{c.Objects=c.Objects.filter(o=>!(o.ObjectType===5&&o.Color===actualTone));});
  if(actualKind===1){data.grid[index].Objects=data.grid[index].Objects.filter(o=>o.ObjectType!==1);data.grid[index].Objects.unshift({ObjectType:1,Color:actualTone});}
  else data.grid[index].Objects.push({ObjectType:actualKind,Color:actualTone});
}
function paintCell(index){if(visited.has(index))return;visited.add(index);applyObject(index);renderBoard();}
function fillTerrain(index){
  if(kind!==1&&kind!=='gray'){toast('填充用来铺墙，先选择一种墙');return;}
  const wall=c=>data.grid[c].Objects.find(o=>o.ObjectType===1)?.Color||0,target=wall(index);
  const queue=[index],seen=new Set();while(queue.length){const n=queue.pop();if(seen.has(n)||wall(n)!==target)continue;seen.add(n);const x=n%data.width,y=Math.floor(n/data.width);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const nx=x+dx,ny=y+dy;if(nx>=0&&ny>=0&&nx<data.width&&ny<data.height)queue.push(ny*data.width+nx);}}
  seen.forEach(applyObject);
}
function drawShape(a,b){
  const x0=a%data.width,y0=Math.floor(a/data.width),x1=b%data.width,y1=Math.floor(b/data.width);
  if(tool==='rect'){
    for(let x=Math.min(x0,x1);x<=Math.max(x0,x1);x++){applyObject(y0*data.width+x);if(y0!==y1)applyObject(y1*data.width+x);}
    for(let y=Math.min(y0,y1)+1;y<Math.max(y0,y1);y++){applyObject(y*data.width+x0);if(x0!==x1)applyObject(y*data.width+x1);}
  }else{const steps=Math.max(Math.abs(x1-x0),Math.abs(y1-y0));for(let n=0;n<=steps;n++){const t=steps?n/steps:0;applyObject(Math.round(y0+(y1-y0)*t)*data.width+Math.round(x0+(x1-x0)*t));}}
}
function updateHover(event){
  if(page!=='edit'||stackOpen||busy)return;
  const cell=event.target.closest('[data-cell]');const ghost=$('#hover-object');
  if(!cell||tool==='look'){ghost.style.display='none';return;}
  hoverCell=+cell.dataset.cell;
  const box=cell.getBoundingClientRect(),space=$('#board-space').getBoundingClientRect();
  ghost.style.left=(box.left-space.left)/scale+'px';ghost.style.top=(box.top-space.top)/scale+'px';
  ghost.classList.toggle('erase',tool==='erase');ghost.style.display='block';
  ghost.innerHTML=tool==='erase'?'':objectImage({ObjectType:kind==='gray'?1:kind,Color:kind==='gray'?3:tone},'',true);
}
$('#board').addEventListener('pointerdown',event=>{
  const cell=event.target.closest('[data-cell]');if(!cell||busy||event.button!==0)return;
  const index=+cell.dataset.cell;
  if(tool==='look'){openStack(index);return;}
  if(tool==='eyedrop'){const object=data.grid[index].Objects.at(-1);if(object){kind=object.Color===3?'gray':object.ObjectType;tone=object.Color===3?tone:object.Color;tool='place';renderPalette();toast('拿起了'+(kind==='gray'?'灰墙':colors[tone]+labels[kind]));}return;}
  if(stackOpen){stackOpen=false;renderBoard();}
  down=true;stroke=snapshot();visited.clear();startCell=hoverCell=index;
  if(tool==='fill'){fillTerrain(index);down=false;if(changed(stroke))remember(stroke);renderBoard();return;}
  if(tool==='line'||tool==='rect')return;
  paintCell(index);updateHover(event);
});
$('#board').addEventListener('pointermove',event=>{const cell=event.target.closest('[data-cell]');if(cell)hoverCell=+cell.dataset.cell;if(down&&cell&&['place','erase'].includes(tool))paintCell(+cell.dataset.cell);updateHover(event);});
$('#board-space').addEventListener('pointerleave',()=>{$('#hover-object').style.display='none';});
window.addEventListener('pointerup',()=>{if(!down)return;down=false;if(['line','rect'].includes(tool))drawShape(startCell,hoverCell);if(changed(stroke))remember(stroke);renderBoard();});

function popup(html){$('#popup').innerHTML=`<button class="close-small" data-action="close-popup" aria-label="关闭">×</button>`+html;$('#popup-shade').hidden=false;$('#hover-object').style.display='none';$('#toast').classList.remove('visible');}
function closePopup(){$('#popup-shade').hidden=true;}
function switchScreen(target){
  page=target;$$('.screen').forEach(s=>s.classList.toggle('active',s.id===target));closePopup();
  $$('[data-preview]').forEach(b=>b.classList.toggle('current',b.dataset.preview===(target==='edit'?'edit':target)));
  $('#game').classList.toggle('stack-open',stackOpen&&target==='edit');
  if(target==='library')renderLibrary();else renderBoard();
  renderHistory();
}
async function transition(action){
  if(busy)return;busy=true;$('#hover-object').style.display='none';
  const iris=$('#iris'),circle=$('#iris-circle');iris.style.display='block';
  const max=810,outer=2600,duration=matchMedia('(prefers-reduced-motion: reduce)').matches?1:300;
  const radius=r=>{circle.setAttribute('r',(outer+r)/2);circle.setAttribute('stroke-width',outer-r);};
  const ease=t=>t<.5?4*t*t*t:1-(-2*t+2)**3/2;
  const animate=open=>new Promise(resolve=>{const start=performance.now();function tick(time){const t=Math.min(1,(time-start)/duration);radius(max*(open?ease(t):1-ease(t)));if(t<1)requestAnimationFrame(tick);else resolve();}requestAnimationFrame(tick);});
  await animate(false);action();await new Promise(r=>requestAnimationFrame(r));await animate(true);iris.style.display='none';busy=false;
}
function renderLibrary(){
  libraryItems[activeLevel]={snapshot:snapshot(),saved:copy(savedSnapshot),dirty,history:copy(history),future:copy(future)};
  const start=Math.max(0,Math.min(libraryIndex-1,libraryItems.length-3)),isNew=libraryIndex===libraryItems.length;
  $('#library-cards').innerHTML=libraryItems.slice(start,start+3).map((item,n)=>{const i=start+n;return `<button class="level-tile ${i===libraryIndex?'selected':''}" data-level="${i}" aria-label="选择关卡 ${escapeText(item.snapshot.name)}">${miniBoard(item.snapshot.data,147)}</button>`;}).join('')+`<button class="level-tile new-tile ${isNew?'selected':''}" data-level="${libraryItems.length}" aria-label="做一关">${glyph('plus')}<span>做一关</span></button>`;
  $('#selected-name').textContent=isNew?'你来出一道题':libraryItems[libraryIndex].snapshot.name;
  $('#selected-description').textContent=isNew?'从一张空棋盘开始':libraryItems[libraryIndex].dirty?'还在制作':'可以继续制作';
  $('[data-action="continue"]').innerHTML=isNew?'开始制作 '+glyph('pencil'):'继续制作 '+glyph('pencil');
  $('[data-action="level-options"]').hidden=isNew;
}
function loadLibraryLevel(){
  if(libraryIndex===activeLevel)return;
  const item=libraryItems[libraryIndex];activeLevel=libraryIndex;data=copy(item.snapshot.data);levelName=item.snapshot.name;chapter=item.snapshot.chapter;
  savedSnapshot=copy(item.saved);dirty=item.dirty;history=copy(item.history);future=copy(item.future);selected=-1;layer=-1;stackOpen=false;zoom=1;renderPalette();
}
function validate(){
  const problems=[];
  for(const color of [1,2]){
    const count=data.grid.reduce((sum,cell)=>sum+cell.Objects.filter(o=>o.ObjectType===5&&o.Color===color).length,0);
    if(count!==1)problems.push({title:count===0?`还少一个${colors[color]}色角色`:`${colors[color]}色角色有点多`,text:'白色和黑色角色各一位，才能轮流行动。',object:{ObjectType:5,Color:color},fix:'去放一个',tone:color,kind:5});
  }
  if(!data.grid.some(c=>c.Objects.some(o=>[3,4].includes(o.ObjectType))))problems.push({title:'还没有要到达的地方',text:'摆一个目标，让玩家知道这一关要去哪儿。',object:{ObjectType:4,Color:1},fix:'去放目标',tone:1,kind:4});
  data.grid.forEach((cell,i)=>{if(cell.Objects.some(o=>o.ObjectType===1&&o.Color===3)&&cell.Objects.length>1)problems.push({title:'灰墙占住了这个位置',text:'灰墙上不能放角色、箱子或目标，先把它们分开。',object:{ObjectType:1,Color:3},fix:'去看看',cell:i});});
  return problems;
}
let pendingFix=null;
function tryPlay(){
  const problems=validate();
  if(problems.length){pendingFix=problems[0];popup(`<h2>${pendingFix.title}</h2><div class="problem-illustration">${objectImage(pendingFix.object,'',true)}<div class="missing-place">?</div></div><p class="problem-text">${pendingFix.text}</p><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">继续制作</button><button class="game-button prominent" data-action="fix-problem">${pendingFix.fix}</button></div>`);return;}
  transition(()=>switchScreen('play'));
}
function toolsMenu(){
  const list=[['line','line','拉一条'],['rect','rect','围一圈'],['fill','fill','铺满'],['eyedrop','eye','取物件'],['repeat','copy','再叠一层'],['selection','select','选一片']];
  popup(`<h2>换种摆法</h2><p>常用的摆放方式都在这里</p><div class="tool-grid">${list.map(([id,icon,title])=>`<button class="tool-choice ${tool===id?'active':''}" data-extra-tool="${id}">${glyph(icon)}<span>${title}</span></button>`).join('')}</div><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">继续摆放</button></div>`);
}
function moreMenu(){
  popup(`<h2>这一关</h2><div class="menu-choices"><button class="menu-choice" data-action="save">${glyph('save')}保存</button><button class="menu-choice" data-action="rename">${glyph('pencil')}取个名字</button><button class="menu-choice" data-action="resize">${glyph('resize')}棋盘大小</button><button class="menu-choice" data-action="theme">${glyph('switch')}换个主题</button><button class="menu-choice" data-action="share">${glyph('import')}分享这一关</button><button class="menu-choice" data-action="back-library">${glyph('back')}我的关卡</button></div><p><button data-action="restore-draft" style="font-size:13px;color:#c4d4dd">找回上次保存的草稿</button></p>`);
}
function saveDraft(){
  try{localStorage.setItem('box-or-void-player-ui-draft',JSON.stringify(snapshot()));savedSnapshot=snapshot();dirty=false;renderHistory();toast('草稿已保留');return true;}
  catch{popup('<h2>这次没能保存</h2><p>你的修改还在。请保留这个页面，或导出关卡文件。</p><div class="popup-buttons"><button class="game-button prominent" data-action="export">导出文件</button><button class="game-button secondary" data-action="close-popup">继续制作</button></div>');return false;}
}
function returnLibrary(){
  if(!dirty){transition(()=>switchScreen('library'));return;}
  popup('<h2>先把这一关收好？</h2><p>保存这次修改，下次回来接着做。</p><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">再改一会儿</button><button class="game-button prominent" data-action="save-leave">保存并返回</button></div><p><button data-action="discard-leave" style="font-size:12px;color:#bdc8cd">放弃这次修改</button></p>');
}
function newLevel(){
  popup(`<h2>做一关</h2><p>先选一张棋盘，之后还能改大小。</p><div class="size-choices">${sizeOptions.map((s,i)=>`<button class="size-choice ${i===newSize?'chosen':''}" data-new-size="${i}"><strong>${s[0]} × ${s[1]}</strong><span>${s[2]}</span></button>`).join('')}</div><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">返回</button><button class="game-button prominent" data-action="create-level">开始摆放</button></div>`);
}
function resizeMenu(){
  resizeW=data.width;resizeH=data.height;showResize();
}
function resized(){
  const dx=anchor%3===0?0:anchor%3===2?resizeW-data.width:Math.floor((resizeW-data.width)/2);
  const ay=2-Math.floor(anchor/3),dy=ay===0?0:ay===2?resizeH-data.height:Math.floor((resizeH-data.height)/2);
  const result={width:resizeW,height:resizeH,grid:Array.from({length:resizeW*resizeH},()=>({Objects:[]}))};let cropped=0;
  data.grid.forEach((c,i)=>{const x=i%data.width+dx,y=Math.floor(i/data.width)+dy;if(x<0||y<0||x>=resizeW||y>=resizeH)cropped+=c.Objects.length;else result.grid[y*resizeW+x]=copy(c);});
  return {result,cropped,bounds:{dx,dy,width:resizeW,height:resizeH}};
}
function showResize(){
  const proposal=resized();
  popup(`<h2>棋盘大小</h2><div class="resize-steppers"><div class="stepper"><label>横着</label><button data-size-step="w,-1" aria-label="减少宽度">−</button><output>${resizeW}</output><button data-size-step="w,1" aria-label="增加宽度">+</button></div><div class="stepper"><label>竖着</label><button data-size-step="h,-1" aria-label="减少高度">−</button><output>${resizeH}</output><button data-size-step="h,1" aria-label="增加高度">+</button></div></div><div class="anchor-row"><span>保留哪一边</span><div class="anchor-grid">${Array.from({length:9},(_,i)=>`<button data-anchor="${i}" class="${i===anchor?'chosen':''}" aria-label="保留位置 ${i+1}"></button>`).join('')}</div><span>${['左上','上方','右上','左边','中间','右边','左下','下方','右下'][anchor]}</span></div><div class="crop-previews"><div class="preview">${miniBoard(data,130,proposal.bounds)}<span>现在 · ${data.width} × ${data.height}</span></div><span>→</span><div class="preview after">${miniBoard(proposal.result,130)}<span>改好后 · ${resizeW} × ${resizeH}</span></div></div><p class="crop-warning">${proposal.cropped?`橙色范围内的 ${proposal.cropped} 个物件会被裁掉`:'所有物件都会留下'}</p><p class="fine">棋盘边缘不是墙。想封住路口，可以自己铺墙。</p><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">先不改</button><button class="game-button prominent" data-action="apply-size">就这么大</button></div>`);
}
function applySize(){const {result}=resized();remember();data=result;selected=-1;layer=-1;stackOpen=false;zoom=1;closePopup();renderBoard();toast('棋盘改好了，撤回就能恢复');}
function action(name){
  if(busy)return;
  switch(name){
    case'more':moreMenu();break;
    case'tools':toolsMenu();break;
    case'play':tryPlay();break;
    case'return-edit':transition(()=>switchScreen('edit'));break;
    case'close-popup':closePopup();break;
    case'close-stack':closeStack();break;
    case'undo':if(history.length){future.push(snapshot());restore(history.pop());toast('撤回了一步');}break;
    case'redo':if(future.length){history.push(snapshot());restore(future.pop());toast('重做了一步');}break;
    case'save':closePopup();saveDraft();break;
    case'save-leave':if(saveDraft())transition(()=>switchScreen('library'));break;
    case'discard-leave':restore(savedSnapshot);history=[];future=[];dirty=false;transition(()=>switchScreen('library'));break;
    case'restore-draft':try{const draft=JSON.parse(localStorage.getItem('box-or-void-player-ui-draft'));if(!draft?.data?.grid){toast('还没有保存过草稿');break;}remember();restore(draft);savedSnapshot=copy(draft);dirty=false;closePopup();renderHistory();toast('上次的草稿回来了，也可以撤回');}catch{toast('这份草稿暂时打不开，当前修改仍然保留');}break;
    case'back-library':returnLibrary();break;
    case'continue':if(libraryIndex===libraryItems.length)newLevel();else transition(()=>{loadLibraryLevel();switchScreen('edit');});break;
    case'view':view=view===1?2:1;renderBoard();renderPalette();break;
    case'grid':gridOn=!gridOn;renderBoard();renderPalette();break;
    case'fit':zoom=1;renderBoard();break;
    case'remove-layer':if(selected>=0&&layer>=0){remember();data.grid[selected].Objects.splice(layer,1);layer=data.grid[selected].Objects.findLastIndex(o=>[2,5].includes(o.ObjectType));renderBoard();toast('拿走了，还能撤回');}break;
    case'lift':if(selected>=0&&layer>=0){remember();const object=data.grid[selected].Objects.splice(layer,1)[0];data.grid[selected].Objects.push(object);layer=data.grid[selected].Objects.length-1;renderBoard();}break;
    case'fix-problem':closePopup();if(pendingFix.cell!==undefined)openStack(pendingFix.cell);else{kind=pendingFix.kind;tone=pendingFix.tone;tool='place';stackOpen=false;renderBoard();renderPalette();}break;
    case'rename':popup(`<h2>给这一关取个名字</h2><p>好记一点，也可以晚点再想。</p><input id="name-input" type="text" maxlength="30" aria-label="关卡名称"><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">返回</button><button class="game-button prominent" data-action="confirm-name">就叫这个</button></div>`);$('#name-input').value=levelName;break;
    case'confirm-name':if($('#name-input').value.trim()){remember();levelName=$('#name-input').value.trim();renderHistory();closePopup();if(page==='library')renderLibrary();}break;
    case'resize':resizeMenu();break;
    case'apply-size':{const proposal=resized();if(proposal.cropped)popup(`<h2>要拿掉这些物件吗？</h2><div class="crop-previews"><div class="preview">${miniBoard(data,168,proposal.bounds)}</div></div><p>橙色区域里有 ${proposal.cropped} 个物件。<br>改好后，仍然可以撤回。</p><div class="popup-buttons"><button class="game-button secondary" data-action="size-back">再看看</button><button class="game-button danger" data-action="confirm-size">确认改小</button></div>`);else applySize();break;}
    case'size-back':showResize();break;
    case'confirm-size':applySize();break;
    case'create-level':{const [w,h]=sizeOptions[newSize];data={width:w,height:h,grid:Array.from({length:w*h},()=>({Objects:[]}))};levelName='新的一关';selected=-1;layer=-1;history=[];future=[];stackOpen=false;kind=5;tone=1;tool='place';dirty=true;zoom=1;savedSnapshot=snapshot();activeLevel=libraryIndex=libraryItems.length;libraryItems.push({snapshot:snapshot(),saved:copy(savedSnapshot),dirty:true,history:[],future:[]});renderPalette();transition(()=>switchScreen('edit'));break;}
    case'theme':popup(`<h2>换个主题</h2><p>物件还是这些，只换个样子。</p><div class="size-choices"><button class="size-choice ${chapter===0?'chosen':''}" data-theme="0"><img src="${A.sprites['Tut World Tile-sc']}" width="100" height="100" alt="欢迎"></button><button class="size-choice ${chapter===1?'chosen':''}" data-theme="1"><img src="${A.sprites['Box World Tile']}" width="100" height="100" alt="箱即是空"></button></div>`);break;
    case'level-options':loadLibraryLevel();popup('<h2>关卡选项</h2><div class="menu-choices"><button class="menu-choice" data-action="rename">取个名字</button><button class="menu-choice" data-action="duplicate">做个副本</button><button class="menu-choice" data-action="share">分享这一关</button><button class="menu-choice muted" data-action="delete-level">收进回收箱</button></div>');break;
    case'duplicate':closePopup();toast('设计流程：保留原关，生成独立副本');break;
    case'delete-level':popup('<h2>先收进回收箱？</h2><p>之后还能找回来，不会立即删除。</p><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">留下</button><button class="game-button" data-action="demo-recycle">收起来</button></div>');break;
    case'demo-recycle':closePopup();toast('回收反馈示意，样例关卡仍保留');break;
    case'share':popup(`<h2>把这一关送给朋友</h2><div class="share-preview">${miniBoard(data,118)}<div><strong id="share-name"></strong><p>对方导入文件，就能打开。</p></div></div><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">返回</button><button class="game-button prominent" data-action="export">保存成文件</button></div>`);$('#share-name').textContent=levelName;break;
    case'export':{const url=URL.createObjectURL(new Blob([JSON.stringify({schemaVersion:1,name:levelName,theme:chapter,level:data},null,2)],{type:'application/json'}));const a=document.createElement('a');a.href=url;a.download=levelName+'-设计样例.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);break;}
    case'import':popup('<h2>打开朋友的关卡</h2><p>选好文件后，先看看棋盘，再放进我的关卡。</p><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">返回</button><button class="game-button prominent" data-action="import-preview">查看导入示例</button></div>');break;
    case'import-preview':popup(`<h2>找到一关：交错</h2><div class="share-preview">${miniBoard(initialExample,156)}<p>已有同名关卡<br>这次另存一份</p></div><div class="popup-buttons"><button class="game-button secondary" data-action="close-popup">返回</button><button class="game-button prominent" data-action="demo-import">收进我的关卡</button></div>`);break;
    case'demo-import':closePopup();toast('导入反馈示意：保留原关卡，新增副本');break;
    case'help':popup(`<h2>拿起来，摆一摆</h2><div class="problem-illustration">${objectImage({ObjectType:2,Color:1},'',true)}${glyph('play')}${objectImage({ObjectType:4,Color:1},'',true)}</div><p>点底部物件，拿到手上；再点棋盘放下。<br>想看看叠着什么，就选“查看”再点那一格。<br>摆错了，点“撤回”就好。</p><div class="popup-buttons"><button class="game-button prominent" data-action="close-popup">我来试试</button></div><p class="fine">B 摆放　V 查看　E 擦除　Ctrl+Z 撤回　F5 玩一下</p>`);break;
    case'notes':popup('<h2>这一稿改了什么</h2><p class="notes">玩家直接拿起物件，再到棋盘上放下。<br>取消常驻工具侧栏、属性栏和技术状态表。<br>把叠层内容拆开看，把问题提示接回具体操作。<br>关卡库、按钮、选框、文字和转场沿用游戏内风格。</p><p class="notes">本页是可点击的界面样机。试玩只演示界面往返；“保存”仅保留在本浏览器。选区变换、完整文件管理、真实推箱子玩法将在审核后接入 Unity。其余功能边界见 TD 文档。</p><div class="popup-buttons"><button class="game-button prominent" data-action="close-popup">回到设计稿</button></div>');break;
  }
}

document.addEventListener('click',event=>{
  if(busy)return;
  const preview=event.target.closest('[data-preview]');
  if(preview){const target=preview.dataset.preview;if(target==='notes')action('notes');else if(target==='resize'){switchScreen('edit');resizeMenu();}else if(target==='stack'){switchScreen('edit');openStack(Math.min(initialBox,data.grid.length-1));}else switchScreen(target==='library'?'library':'edit');return;}
  const button=event.target.closest('[data-action]');if(button){action(button.dataset.action);return;}
  const item=event.target.closest('[data-kind]');if(item){kind=item.dataset.kind==='gray'?'gray':+item.dataset.kind;tool='place';allowRepeat=false;stackOpen=false;renderBoard();renderPalette();return;}
  const color=event.target.closest('[data-tone]');if(color){tone=+color.dataset.tone;renderPalette();return;}
  const toolButton=event.target.closest('[data-tool]');if(toolButton){tool=toolButton.dataset.tool;allowRepeat=false;if(tool!=='look')closeStack();renderPalette();return;}
  const stack=event.target.closest('[data-layer]');if(stack){layer=+stack.dataset.layer;renderStack();return;}
  const level=event.target.closest('[data-level]');if(level){libraryIndex=+level.dataset.level;renderLibrary();return;}
  const extra=event.target.closest('[data-extra-tool]');if(extra){const choice=extra.dataset.extraTool;if(choice==='selection'){popup('<h2>选中一片，再一起改</h2><p>拖框选择后，物件上方会出现复制、移动、旋转和镜像。<br>每次整体操作都可以一步撤回。</p><div class="popup-buttons"><button class="game-button prominent" data-action="close-popup">继续制作</button></div><p class="fine">此界面稿暂未接入选区变换，交互契约见 TD 文档。</p>');return;}if(choice==='repeat'){tool='place';allowRepeat=true;closePopup();renderPalette();toast('再放一次，会叠在原有物件上面');return;}tool=choice;closePopup();closeStack();renderPalette();return;}
  const newChoice=event.target.closest('[data-new-size]');if(newChoice){newSize=+newChoice.dataset.newSize;newLevel();return;}
  const step=event.target.closest('[data-size-step]');if(step){const [axis,amount]=step.dataset.sizeStep.split(',');if(axis==='w')resizeW=Math.max(3,Math.min(32,resizeW+Number(amount)));else resizeH=Math.max(3,Math.min(32,resizeH+Number(amount)));showResize();return;}
  const anchorButton=event.target.closest('[data-anchor]');if(anchorButton){anchor=+anchorButton.dataset.anchor;showResize();return;}
  const themeButton=event.target.closest('[data-theme]');if(themeButton){remember();chapter=+themeButton.dataset.theme;closePopup();renderBoard();renderPalette();}
});
$('#library-cards').addEventListener('dblclick',event=>{if(event.target.closest('[data-level]'))action('continue');});
$('#board-space').addEventListener('wheel',event=>{if(stackOpen)return;event.preventDefault();zoom=Math.max(.7,Math.min(1.2,zoom+(event.deltaY>0?-.1:.1)));renderBoard();},{passive:false});
document.addEventListener('keydown',event=>{
  if(event.target.matches('input,textarea,select')){if(event.key==='Enter'&&event.target.id==='name-input')action('confirm-name');return;}
  if(busy)return;
  if(event.key==='Escape'){if(!$('#popup-shade').hidden)closePopup();else if(page==='play')action('return-edit');else if(stackOpen)closeStack();else if(page==='edit')action('back-library');return;}
  if(!$('#popup-shade').hidden)return;
  if(page==='library'){if(['ArrowLeft','ArrowRight','a','d'].includes(event.key)){libraryIndex=(libraryIndex+(event.key==='ArrowLeft'||event.key==='a'?-1:1)+libraryItems.length+1)%(libraryItems.length+1);renderLibrary();event.preventDefault();}if(event.code==='Space'||event.key==='Enter'){event.preventDefault();action('continue');}return;}
  if(page==='play')return;
  if(event.key==='F5'){event.preventDefault();action('play');return;}
  if(event.ctrlKey){if(event.key.toLowerCase()==='z'){event.preventDefault();action(event.shiftKey?'redo':'undo');}if(event.key.toLowerCase()==='y'){event.preventDefault();action('redo');}if(event.key.toLowerCase()==='s'){event.preventDefault();action('save');}return;}
  const map={b:'place',v:'look',e:'erase',i:'eyedrop',l:'line',u:'rect',f:'fill'};
  if(map[event.key.toLowerCase()]){tool=map[event.key.toLowerCase()];if(tool!=='look')closeStack();renderPalette();}
});
window.addEventListener('resize',fitViewport);
fitViewport();renderBoard();renderPalette();renderHistory();
document.fonts.ready.then(fitViewport);
const query=new URLSearchParams(location.search).get('screen');
if(query==='library')switchScreen('library');else if(query==='stack')openStack(initialBox);else if(query==='resize')resizeMenu();else if(query==='play')switchScreen('play');
