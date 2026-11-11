using System;
using System.Collections;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private bool LightingMatches()
        {
            var state=app.Session.State;var field=LightField.Compute(state);
            var texture=(Texture2D)typeof(BoardView).GetField("fieldAfter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(app.Board);
            if(texture==null||app.Board.GetComponentsInChildren<MeshRenderer>().Length!=2)return false;
            for(int y=0;y<state.Level.Height;y++)for(int x=0;x<state.Level.Width;x++)
            {
                int i=y*state.Level.Width+x;var p=new Cell(x,y);var pixel=texture.GetPixel(x,state.Level.Height-1-y);
                if((pixel.r>.5f)!=state.Level.Floor(p)||(pixel.g>.5f)!=field.Lamp[i]||(pixel.b>.5f)!=((field.LampAxes[i]&1)!=0)||(pixel.a>.5f)!=((field.LampAxes[i]&2)!=0))return false;
            }
            return true;
        }
        private IEnumerator LightingRegression()
        {
            LevelData Room()=>new LevelData{id="light-check-"+Guid.NewGuid().ToString("N"),title="",rows=Enumerable.Repeat(".......",7).ToArray()};
            int completed=app.Progress.Data.completed.Count;
            foreach(SunSide side in Enum.GetValues(typeof(SunSide)))
            {
                var data=Room();var travel=LightField.SunTravel[(int)side];data.sunEnabled=true;data.sunFrom=side;
                data.pieces=new[]{new EntityData{id="box",cell=new Cell(3,3)}};data.player=data.pieces[0].cell+travel;data.coffin=data.player+new Cell(-travel.y,travel.x);
                app.Edit(data);yield return Ready();app.StartEditorPlay(data);yield return Ready();
                var source=app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Incoming sunlight");
                var box=app.Board.Screen(new Cell(3,3));var point=app.Board.Camera.WorldToScreenPoint(source.transform.position);
                Require((point.x-box.x)*travel.x-(point.y-box.y)*travel.y<0,"sun source stays on incoming side "+side);
                Require(app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Coffin illumination").color.a>.3f,"lit coffin visibly receives illumination "+side);
                Capture("lighting_sun_"+side);
                yield return Tap(Key((char)"URDL"[Array.IndexOf(LightField.Directions,new Cell(-travel.y,travel.x))]));
                Require(app.Page==GamePage.Dead&&app.Session.State.Player==data.coffin&&!app.Session.State.Won,"sunlit coffin kills without completing "+side);
                Require(app.Board.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name=="Vampire"||r.name=="Cape"||r.name=="Fangs").All(r=>!r.enabled),"all character layers disappear on death "+side);
                yield return Tap(KeyCode.Z);Require(!app.Session.State.Dead&&app.Session.State.Player==data.player,"coffin death undo restores vampire "+side);
                Require(app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Cape").enabled,"undo restores cape "+side);
            }
            var lamp=Room();lamp.player=new Cell(3,2);lamp.coffin=new Cell(4,2);lamp.pieces=new[]{new EntityData{id="light",kind=PieceKind.Lamp,cell=new Cell(4,4)}};
            app.Edit(lamp);yield return Ready();app.StartEditorPlay(lamp);yield return Ready();Capture("lighting_lit_coffin");yield return Tap(KeyCode.RightArrow);
            Require(app.Page==GamePage.Dead&&!app.Session.State.Won,"cardinal lamp prevents completion at coffin");Capture("lighting_coffin_death");
            yield return Tap(KeyCode.Z);Require(!app.Session.State.Dead&&app.Session.State.Player==lamp.player,"lamp coffin death is undoable");
            lamp.pieces=lamp.pieces.Concat(new[]{new EntityData{id="shade",cell=new Cell(4,3)}}).ToArray();
            app.Edit(lamp);yield return Ready();app.StartEditorPlay(lamp);yield return Ready();
            Require(app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Coffin illumination").color.a<.001f,"shaded coffin retains dark interior");Capture("lighting_safe_coffin");
            bool won=false;Action<ActionResult> listener=a=>won|=a.After.Won;app.ActionApplied+=listener;yield return Tap(KeyCode.RightArrow);yield return Page(GamePage.Editing);app.ActionApplied-=listener;
            Require(won&&app.Page==GamePage.Editing,"unlit coffin completes the trial normally");
            Require(app.Progress.Data.completed.Count==completed,"light regression trials never alter campaign progress");
            app.ShowTitle();yield return Ready();
        }
    }
}
