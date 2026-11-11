using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        public void BeginCharacterRevision(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report.runtime="Unity Editor character revision";report.width=Screen.width;report.height=Screen.height;
            folder=Path.GetFullPath("DraculasBox/Reviews/CharacterRevision-20261111/Checks");Directory.CreateDirectory(folder);
            Application.logMessageReceived+=Log;StartCoroutine(Guard(CharacterRevision()));
        }
        private SpriteRenderer[] EffectSprites(string prefix)=>app.Board.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.gameObject.activeSelf&&r.name.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
        private IEnumerator CharacterRevision()
        {
            yield return CharacterJuice();
            var data=LevelData.Blank(9,7);data.player=new Cell(1,3);data.coffin=new Cell(7,5);data.pieces=Array.Empty<EntityData>();
            app.StartEditorPlay(data);yield return Ready();string start=app.Session.State.Key();
            yield return Tap(KeyCode.RightArrow);yield return Tap(KeyCode.RightArrow);yield return Tap(KeyCode.DownArrow);string beforeReset=app.Session.State.Key();int history=app.Session.UndoCount;
            app.Act(Command.Reset);Require(app.Session.State.Key()==start&&app.Session.Moves==0&&app.Session.UndoCount==history+1,"bat reset keeps the ordinary immediate model reset and one undo record");
            bool sawBats=false,sawReform=false;
            for(int i=0;i<15;i++)
            {
                yield return new WaitForSecondsRealtime(.019f);
                sawBats|=EffectSprites("Reset bat ").Any(r=>r.enabled);
                if(i>10)sawReform|=HeroForCheck().enabled;
                Capture("reset-"+i.ToString("00"),960,540);
            }
            Require(sawBats&&sawReform&&HeroForCheck().enabled,"reset visibly disperses into bats and reforms");
            Require(EffectSprites("Reset bat ").All(r=>!r.enabled)&&Mathf.Abs(HeroForCheck().transform.localScale.x-.72f)<.001f,"reset leaves no bats or scale residue");
            yield return Tap(KeyCode.Z);Require(app.Session.State.Key()==beforeReset,"undo reverses a bat reset exactly");
            app.Act(Command.Reset);yield return new WaitForSecondsRealtime(.14f);Require(!app.Board.IsAnimating,"bat presentation adds no extra gameplay lock");
            app.Act(Command.Right);yield return null;Require(EffectSprites("Reset bat ").All(r=>!r.enabled)&&HeroForCheck().enabled,"new movement interrupts reset without ghost bats or a hidden player");yield return Ready();
            string pausedState=app.Session.State.Key();int moves=app.Session.Moves;
            app.Pause();yield return new WaitForSecondsRealtime(.2f);
            Require(app.Page==GamePage.Paused&&EffectSprites("Pause cape fold ").Count(r=>r.enabled)==2,"pause closes two front cape folds");
            Require(Mathf.Abs(HeroForCheck().transform.localScale.x-.72f)<.001f&&HeroForCheck().enabled,"cape wrap preserves face shape and visibility");
            Require(app.Session.State.Key()==pausedState&&app.Session.Moves==moves,"pause cape changes no puzzle state");Capture("pause-wrapped");
            app.Settings();yield return Ready();app.SelectTheme("eclipse");yield return Ready();
            Require(EffectSprites("Pause cape fold ").Count(r=>r.enabled)==2,"pause wrap survives a theme refresh inside settings");
            app.CloseSettings();yield return Ready();app.Resume();
            for(int i=0;i<7;i++){yield return new WaitForSecondsRealtime(.025f);Capture("resume-"+i.ToString("00"),960,540);}
            Require(EffectSprites("Pause cape fold ").All(r=>!r.enabled)&&Mathf.Abs(EffectSprites("Cape").First(r=>r.name=="Cape").transform.localScale.x-.94f)<.001f,"resume fully opens and restores the original cape");
            app.Pause();yield return new WaitForSecondsRealtime(.06f);app.ResetFromPause();
            float deadline=Time.realtimeSinceStartup+2;
            while(app.Session.State.Key()!=start&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
            Require(app.Page==GamePage.Playing&&EffectSprites("Pause cape fold ").All(r=>!r.enabled)&&EffectSprites("Reset bat ").All(r=>!r.enabled),"reset from pause clears both overlapping presentations");
            app.ReturnToEditor();yield return Ready();Require(EffectSprites("Reset bat ").Length==0&&EffectSprites("Pause cape fold ").Length==0,"editor preview has no character-effect objects");
            yield return CircuitReplyChecks();
            app.ShowTitle();yield return Ready();Capture("title-final");
        }
        private IEnumerator CircuitReplyChecks()
        {
            var audio=app.GetComponent<GameAudio>();var played=new List<string>();var times=new List<double>();
            Action<int,bool,bool> record=(channel,door,active)=>{played.Add(channel+":"+(door?"door":"plate")+":"+active);times.Add(AudioSettings.dspTime);};
            audio.CircuitPlayed+=record;
            try
            {
                var data=LevelData.Blank(9,7);data.player=new Cell(2,3);data.coffin=new Cell(7,5);data.pieces=Array.Empty<EntityData>();
                data.plates=new[]{new DeviceData{cell=new Cell(3,3),channel=0},new DeviceData{cell=new Cell(4,3),channel=0},new DeviceData{cell=new Cell(3,4),channel=1}};
                data.doors=new[]{new DeviceData{cell=new Cell(6,2),channel=0},new DeviceData{cell=new Cell(6,3),channel=0},new DeviceData{cell=new Cell(6,4),channel=1}};
                app.StartEditorPlay(data);yield return Ready();played.Clear();times.Clear();
                app.Act(Command.Right);Require(played.SequenceEqual(new[]{"0:plate:True"}),"press starts exactly one channel-matched call");
                yield return new WaitForSecondsRealtime(.13f);
                Require(played.SequenceEqual(new[]{"0:plate:True","0:door:True"})&&times[1]-times[0]>=GameAudio.CircuitReplySeconds-.01,"two doors in one channel give one delayed matching reply");Capture("circuit-open");
                played.Clear();app.Act(Command.Right);yield return new WaitForSecondsRealtime(.13f);
                Require(played.SequenceEqual(new[]{"0:plate:True"})&&app.Session.State.DoorOpen(0),"handoff between plates in one group does not falsely replay a door opening");
                played.Clear();app.Act(Command.Down);yield return new WaitForSecondsRealtime(.13f);
                Require(played.SequenceEqual(new[]{"0:plate:False","0:door:False"}),"release and actual door closure give a soft response pair");
                played.Clear();app.Act(Command.Left);yield return new WaitForSecondsRealtime(.13f);
                Require(played.SequenceEqual(new[]{"1:plate:True","1:door:True"}),"another circuit responds in its own audio channel");
                played.Clear();app.Act(Command.Up);app.Act(Command.Undo);yield return new WaitForSecondsRealtime(.13f);
                Require(!played.Any(e=>e.Contains(":door:")),"undo cancels pending circuit replies");
                played.Clear();app.Act(Command.Up);app.Pause();yield return new WaitForSecondsRealtime(.13f);
                Require(!played.Any(e=>e.Contains(":door:")),"pause cancels pending circuit replies");app.Resume();yield return Ready();
                // A door held by a player remains open even after its plate releases.
                var def=new Definition(data);var onPlate=new State(def,new Cell(3,3),Array.Empty<Cell>(),false,SunSide.Left,Command.Right);
                var inDoor=new State(def,new Cell(6,2),Array.Empty<Cell>(),false,SunSide.Left,Command.Right);
                played.Clear();audio.Circuits(new ActionResult{Kind=ActionKind.Moved,Before=onPlate,After=inDoor,Command=Command.Right});yield return new WaitForSecondsRealtime(.13f);
                Require(played.Contains("0:plate:False")&&played.Count(e=>e=="0:door:False")==1&&inDoor.DoorOpen(0),"reply follows actual changed doors while occupied doors stay open");
                var outDoor=new State(def,new Cell(5,2),Array.Empty<Cell>(),false,SunSide.Left,Command.Left);
                played.Clear();audio.Circuits(new ActionResult{Kind=ActionKind.Moved,Before=inDoor,After=outDoor,Command=Command.Left});
                Require(played.SequenceEqual(new[]{"0:door:False"}),"unoccupied door closure responds without inventing a pressure-plate call");
                for(int channel=0;channel<4;channel++)
                {
                    var plate=audio.CircuitSound(channel,false);var door=audio.CircuitSound(channel,true);
                    Require(plate!=null&&door!=null&&plate!=door&&plate.length<.2f&&door.length<.2f,"short paired audio clips exist for channel "+channel);
                }
                Require(app.GetComponentsInChildren<AudioSource>().Length==3&&app.GetComponentsInChildren<AudioSource>().Single(s=>s.name=="Effects").volume==0,"circuit feedback reuses the existing muted effect source in automated checks");
                WriteCircuitPreview(audio);
            }
            finally{audio.CircuitPlayed-=record;audio.CancelCircuitReplies();}
        }
        private void WriteCircuitPreview(GameAudio audio)
        {
            const int rate=22050;var samples=new float[rate*3];
            for(int channel=0;channel<4;channel++)for(int type=0;type<2;type++)
            {
                var clip=audio.CircuitSound(channel,type==1);var sound=new float[clip.samples];clip.GetData(sound,0);int offset=(int)((channel*.65f+(type==1?GameAudio.CircuitReplySeconds:0))*rate);
                for(int i=0;i<sound.Length;i++)samples[offset+i]+=sound[i]*.65f;
            }
            using(var file=new BinaryWriter(File.Create(Path.Combine(folder,"circuit-preview.wav"))))
            {
                int bytes=samples.Length*2;file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));file.Write(36+bytes);file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));file.Write(16);file.Write((short)1);file.Write((short)1);file.Write(rate);file.Write(rate*2);file.Write((short)2);file.Write((short)16);file.Write(System.Text.Encoding.ASCII.GetBytes("data"));file.Write(bytes);
                foreach(float sample in samples)file.Write((short)(Mathf.Clamp(sample,-1,1)*32767));
            }
        }
    }
}
