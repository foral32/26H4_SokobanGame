import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {apply,explain,fromTrace,initial,stackAt} from '../src/engine.js';
import {missingMask,cornerFlags,maskNames} from '../src/wall-tiles.js';
import {newSession,sessionCommand} from '../src/program-model.js';
import {bootStages,components} from '../src/program-content.js';
import {executionCases,executionTrace} from '../src/program-model.js';

const {levels}=JSON.parse(fs.readFileSync(new URL('../src/atlas-data.json',import.meta.url),'utf8'));
const level=id=>levels.find(l=>l.id===id);
const position=s=>JSON.stringify({active:s.active,pieces:s.pieces.map(p=>[p.i,p.x,p.y,p.depth])});

test('startup, component and execution excerpts resolve to real current source lines',()=>{
  const code=JSON.parse(fs.readFileSync(new URL('../src/atlas-code.json',import.meta.url),'utf8'));
  const points=[...bootStages,...components,...executionCases.flatMap(c=>executionTrace(c,level(c.level)).steps)];
  for(const point of points){
    const source=code.find(s=>s.name===point.file);
    assert.ok(source,point.file);
    assert.ok(source.text.includes(point.anchor),point.file+': '+point.anchor);
    assert.equal(source.text,fs.readFileSync(new URL('../../../../'+source.path,import.meta.url),'utf8'));
  }
});

test('all 16 tile masks round-trip from missing edge bits',()=>{
  for(let mask=0;mask<16;mask++){
    const connected=Array.from({length:4},(_,i)=>!(mask&(1<<i)));
    assert.equal(missingMask(connected),mask);
    assert.ok(maskNames[mask]);
  }
  assert.equal(missingMask([false,false,true,true]),3);
});

test('concave corners depend on both cardinal neighbors and diagonal, for every combination',()=>{
  for(let a=0;a<16;a++)for(let b=0;b<16;b++){
    const cardinal=Array.from({length:4},(_,i)=>Boolean(a&(1<<i)));
    const diagonal=Array.from({length:4},(_,i)=>Boolean(b&(1<<i)));
    const flags=cornerFlags(cardinal,diagonal);
    flags.forEach((v,i)=>assert.equal(v,cardinal[i]&&cardinal[(i+1)%4]&&!diagonal[i]));
    assert.deepEqual(cornerFlags(cardinal,diagonal,false),[false,false,false,false]);
  }
  assert.equal(missingMask([true,true,true,true]),0);
  assert.deepEqual(cornerFlags([true,true,true,true],[false,true,true,true]),[true,false,false,false]);
});

test('instrumented execution equals ordinary execution throughout all 21 solution traces',()=>{
  for(const l of levels)l.commands.split('').forEach((c,i)=>{
    const before=fromTrace(l.trace[i]);
    const result=apply(l.definition,before,c),detail=explain(l.definition,before,c);
    assert.deepEqual(detail.state,result.state);
    assert.equal(detail.kind,result.kind);
    assert.equal(detail.changed,result.changed);
    assert.ok(detail.steps.length>0);
    assert.deepEqual(detail.steps.at(-1).state,result.state);
  });
});

test('successful push plans both boxes before committing any piece, then commits furthest first',()=>{
  const l=level('n_box_3'),before=fromTrace(l.trace[4]),copy=structuredClone(before);
  const r=explain(l.definition,before,'R');
  const commits=r.steps.filter(s=>s.event==='commit');
  assert.equal(commits.length,3);
  assert.ok(commits[0].piece.x>commits[1].piece.x);
  assert.ok(commits[1].piece.x>commits[2].piece.x);
  for(const step of r.steps.slice(0,r.steps.indexOf(commits[0])))assert.equal(position(step.state),position(before));
  assert.deepEqual(before,copy);
});

test('a blocked dependency produces no commits and preserves all positions and depths',()=>{
  const l=level('n_box_1'),before=fromTrace(l.trace[6]);
  const r=explain(l.definition,before,'L');
  assert.equal(r.kind,'Blocked');assert.equal(r.changed,false);
  assert.equal(r.steps.filter(s=>s.event==='commit').length,0);
  assert.ok(r.steps.some(s=>s.event==='dependency'));
  assert.ok(r.steps.some(s=>s.event==='reject'));
  assert.equal(position(r.state),position(before));
});

test('a covered player exits before entering recursive planning',()=>{
  const l=level('n_box_7'),before=apply(l.definition,fromTrace(l.trace[17]),'S').state;
  const r=explain(l.definition,before,'R');
  assert.equal(r.kind,'Covered');
  assert.equal(r.steps.some(s=>s.event==='inspect'),false);
  assert.equal(r.steps.some(s=>s.event==='commit'),false);
});

test('session restores control, position and layer order, including reset checkpoint history',()=>{
  const l=level('n_tut_2');let session=newSession(l.definition);
  for(const c of 'RRSDSR')session=sessionCommand(l.definition,session,c);
  const beforeReset=structuredClone(session);
  session=sessionCommand(l.definition,session,'RESET');
  assert.equal(position(session.state),position(initial(l.definition)));
  assert.equal(session.history.length,0);
  session=sessionCommand(l.definition,session,'Z');
  assert.equal(position(session.state),position(beforeReset.state));
  assert.equal(session.history.length,beforeReset.history.length);
  assert.equal(stackAt(session.state,5,2).length,2);
  session=sessionCommand(l.definition,session,'Z');
  assert.equal(position(session.state),position(beforeReset.history.at(-1)));
});

test('blocked input does not create an undo entry and repeated reset replaces its checkpoint',()=>{
  const l=level('n_tut_2');let s=newSession(l.definition);
  const denied=['U','R','D','L'].find(c=>!apply(l.definition,s.state,c).changed);
  if(denied){s=sessionCommand(l.definition,s,denied);assert.equal(s.history.length,0);}
  s=sessionCommand(l.definition,s,'R');
  s=sessionCommand(l.definition,s,'RESET');
  s=sessionCommand(l.definition,s,'RESET');
  s=sessionCommand(l.definition,s,'Z');
  assert.equal(position(s.state),position(initial(l.definition)));
  assert.equal(s.history.length,0);
});
