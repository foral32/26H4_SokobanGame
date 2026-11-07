import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {initial,fromTrace,apply,stackAt,goalsOf,fulfilled} from '../src/engine.js';
const {levels}=JSON.parse(fs.readFileSync(new URL('../src/atlas-data.json',import.meta.url),'utf8'));
function key(s){return s.active+'|'+s.pieces.map(p=>`${p.i},${p.kind},${p.tone},${p.x},${p.y},${stackAt(s,p.x,p.y).findIndex(x=>x.i===p.i)}`).join(';');}
for(const level of levels)test(level.id+' matches every verified Unity-core trace state',()=>{
  let state=initial(level.definition);assert.equal(key(state),key(fromTrace(level.trace[0])));
  [...level.commands].forEach((c,i)=>{const r=apply(level.definition,state,c);assert.ok(r.changed);state=r.state;assert.equal(key(state),key(fromTrace(level.trace[i+1])),`step ${i+1}`);});
  assert.ok(goalsOf(level.definition).every(g=>fulfilled(state,g)));
});
test('a covered player cannot leave a three-layer cell',()=>{const l=levels.find(l=>l.id==='n_box_7');let s=fromTrace(l.trace[17]);s=apply(l.definition,s,'S').state;assert.equal(apply(l.definition,s,'R').kind,'Covered');});
test('a fulfilled box goal becomes unfulfilled when a player steps over it',()=>{const l=levels.find(l=>l.id==='n_box_4');let s=fromTrace(l.trace[16]);const g=goalsOf(l.definition).find(g=>g.kind===2);assert.ok(fulfilled(s,g));s=apply(l.definition,s,'S').state;s=apply(l.definition,s,'R').state;assert.equal(fulfilled(s,g),false);});
