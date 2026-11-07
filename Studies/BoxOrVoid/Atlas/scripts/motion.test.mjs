import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
import {pieceMotion,markerMotion,shakeOffset,smoothingBlend,approach,menuMotion,irisMotion,irisGeometry,cellThreshold,completionMotion,completionTicks,particleMotion} from '../src/motion-model.js';
import {motionEffects,motionContext,motionDuration,motionMilestones,sampleMotion} from '../src/motion-content.js';
const data=JSON.parse(fs.readFileSync(new URL('../src/atlas-data.json',import.meta.url),'utf8')),p=data.motion;
const near=(a,b)=>assert.ok(Math.abs(a-b)<1e-8,`${a} != ${b}`);

test('motion snapshots and every curve anchor refer to current C# sources',()=>{
  const code=JSON.parse(fs.readFileSync(new URL('../src/atlas-code.json',import.meta.url),'utf8'));
  for(const [name,hash] of Object.entries(p.sourceHashes)){
    const source=code.find(s=>s.name===name);assert.ok(source,name);
    assert.equal(crypto.createHash('sha256').update(source.text).digest('hex'),hash);
    assert.equal(source.text,fs.readFileSync(new URL('../../../../'+source.path,import.meta.url),'utf8'));
  }
  for(const effect of motionEffects)for(const track of effect.tracks)assert.ok(code.find(s=>s.name===track.file)?.text.includes(track.anchor),effect.id+' '+track.anchor);
});
test('move interpolates exactly one cell and restores its deformation and layer after 100ms',()=>{
  const effect=motionEffects[0],c=motionContext(effect,data.levels),old=c.before.pieces.find(o=>o.i===c.focus.i);
  const mid=pieceMotion(old,c.focus,c.after.active,c.kind,p.moveDuration/2,p);
  near(mid.x,(old.x+c.focus.x)/2);near(mid.sx,1+p.moveAmplitude);near(mid.sy,1-p.moveAmplitude);assert.equal(mid.order,60);
  const end=pieceMotion(old,c.focus,c.after.active,c.kind,p.moveDuration,p);near(end.x,c.focus.x);near(end.sx,1);near(end.sy,1);assert.equal(end.order,10+c.focus.depth*5);
});
test('box translation and blocked feedback never acquire the player move squash incorrectly',()=>{
  const push=motionContext(motionEffects.find(e=>e.id==='push'),data.levels),box=push.after.pieces.find(o=>o.kind===2),old=push.before.pieces.find(o=>o.i===box.i);
  near(pieceMotion(old,box,push.after.active,push.kind,.05,p).sx,1);
  const c=motionContext(motionEffects.find(e=>e.id==='blocked'),data.levels),prior=c.before.pieces.find(o=>o.i===c.focus.i),s=pieceMotion(prior,c.focus,c.after.active,c.kind,.05,p);
  near(s.x,prior.x);near(s.sx,1+p.blockedAmplitude);
});
test('marker waits for arrival, then fades from 0 and grows from 0.65 to 1',()=>{
  const c=motionContext(motionEffects.find(e=>e.id==='marker'),data.levels);
  assert.equal(markerMotion(c.before,c.after,c.markerCell,p.moveDuration,p).visible,false);
  const mid=markerMotion(c.before,c.after,c.markerCell,p.moveDuration+p.markerDuration/2,p);near(mid.alpha,.5);near(mid.scale,(p.markerMinScale+1)/2);
  const end=markerMotion(c.before,c.after,c.markerCell,p.moveDuration+p.markerDuration,p);near(end.alpha,1);near(end.scale,1);
});
test('covered-player shake contains two complete cycles and has no movement lock',()=>{
  near(shakeOffset(p.moveDuration/8,p),p.shakeAmplitude);near(shakeOffset(3*p.moveDuration/8,p),-p.shakeAmplitude);near(shakeOffset(p.moveDuration,p),0);
  const e=motionEffects.find(e=>e.id==='covered'),c=motionContext(e,data.levels),s=sampleMotion(e,.05,c,p);assert.equal(s.locked,false);near(s.values.distance,0);
});
test('exponential smoothing reaches identical positions after the same time at 30, 60 and 120 Hz',()=>{
  for(const fps of [30,60,120]){let value=-102.5;for(let i=0;i<fps/2;i++)value+=(102.5-value)*smoothingBlend(1/fps,p.menuTau);near(value,approach(-102.5,102.5,.5,p.menuTau));}
});
test('menu panel uses independent alpha and cubic scale, and press changes target after hold time',()=>{
  const mid=menuMotion(p.panelDuration/2,p,'panel');near(mid.panelAlpha,.5);near(mid.panelScale,p.panelMinScale+(1-p.panelMinScale)*.875);
  const atRelease=menuMotion(p.menuPressDuration,p,'press'),after=menuMotion(p.menuPressDuration+.3,p,'press');assert.ok(atRelease.rightScale<p.menuHover);assert.ok(after.rightScale>atRelease.rightScale);
});
test('iris changes page only during full cover and releases input only after opening',()=>{
  for(const fps of [30,60,120]){
    const m=irisMotion(0,p,fps);near(irisMotion(p.closeDuration,p,fps).aperture,0);assert.equal(irisMotion(p.closeDuration,p,fps).newPage,false);
    const changed=irisMotion(m.switchAt,p,fps);assert.equal(changed.newPage,true);near(changed.aperture,0);assert.equal(changed.busy,true);
    assert.equal(irisMotion(m.total,p,fps).busy,false);near(m.switchAt,Math.ceil(p.closeDuration*fps)/fps+1/fps);
  }
});
test('iris circle has aspect-independent radius and the exact source mesh topology',()=>{
  for(const [w,h] of [[1280,720],[1280,960],[1280,540]]){const m=irisGeometry(.55,w,h,p);near(m.radius,Math.hypot(w,h)*.55/2);assert.equal(m.vertices,579);assert.equal(m.triangles,768);}
  assert.equal(irisGeometry(0,1280,720,p).triangles,2);assert.equal(irisGeometry(1.1,1280,720,p).triangles,0);
});
test('completion uses stable cell thresholds, overlapping marker time, and a separate return time',()=>{
  near(completionMotion(p.completionHold,p).fraction,0);near(completionMotion(p.completionHold+p.completionVanish,p).fraction,1);
  assert.ok(p.completionHold<p.moveDuration+p.markerDuration);assert.equal(completionMotion(p.completionReturn,p).phase,'发起选关转场');
  for(let x=0;x<20;x++)for(let y=0;y<20;y++){const v=cellThreshold(x,y);assert.ok(v>=0&&v<1);assert.equal(v,cellThreshold(x,y));}
});
test('completion sounds respect minimum cooldown under each frame rate',()=>{
  for(const fps of [30,60,120]){const ticks=completionTicks(p,fps);assert.ok(ticks.length>5);ticks.forEach((t,i)=>{assert.ok(t>=p.completionHold&&t<p.completionHold+p.completionVanish);if(i)assert.ok(t-ticks[i-1]+1e-8>=p.completionTick);});}
});
test('all 16 examples have finite curves, in-bounds milestones and source-aligned static snapshots',()=>{
  assert.equal(motionEffects.length,16);
  for(const e of motionEffects){const context=motionContext(e,data.levels),copy=structuredClone(context),duration=motionDuration(e,p);
    for(const t of [0,duration/2,duration]){const s=sampleMotion(e,t,context,p);for(const track of e.tracks)assert.ok(Number.isFinite(s.values[track.key]),e.id+' '+track.key);}
    for(const [time] of motionMilestones(e,p,60))assert.ok(time>=0&&time<=duration+1e-8,e.id);
    assert.deepEqual(context,copy);
  }
});
test('drifting outlines stay inside wrapping bounds with bounded alpha and scale',()=>{
  for(let i=0;i<p.particleCount;i++)for(const t of [0,10,30,60]){const m=particleMotion(i,t,p);assert.ok(Math.abs(m.x)<=5*16/9+1);assert.ok(Math.abs(m.y)<=6);assert.ok(m.alpha>=.4&&m.alpha<=1);assert.ok(m.scale>0);}
});
