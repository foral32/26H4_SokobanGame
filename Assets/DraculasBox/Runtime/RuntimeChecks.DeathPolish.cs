using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DraculasBox.Core;
using TMPro;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private static readonly BindingFlags DeathPrivate=BindingFlags.Instance|BindingFlags.NonPublic;
        private object DeathMember(object owner,string name)=>owner.GetType().GetField(name,DeathPrivate).GetValue(owner);
        public void BeginDeathPolish(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report.runtime="Unity Editor death polish";report.width=Screen.width;report.height=Screen.height;
            folder=Path.GetFullPath("DraculasBox/Reviews/DeathPolish-20261111/Play");Directory.CreateDirectory(folder);
            Application.logMessageReceived+=Log;StartCoroutine(Guard(DeathPolishChecks()));
        }
        private SpriteRenderer DeathHero()=>app.Board.GetComponentsInChildren<SpriteRenderer>().First(s=>s.name=="Vampire");
        private SpriteRenderer[] LiveAsh()=>app.Board.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name.StartsWith("Burn ash ",StringComparison.Ordinal)&&s.enabled).ToArray();
        private void SampleDeath(float age)
        {
            typeof(BoardView).GetField("deathAt",DeathPrivate).SetValue(app.Board,Time.unscaledTime-age);
            typeof(DraculaApp).GetField("diedAt",DeathPrivate).SetValue(app,Time.unscaledTime-age);
            typeof(BoardView).GetField("elapsed",DeathPrivate).SetValue(app.Board,age);
            typeof(BoardView).GetMethod("ApplyFrame",DeathPrivate).Invoke(app.Board,null);
            typeof(BoardView).GetMethod("Refresh",DeathPrivate).Invoke(app.Board,new object[]{Mathf.Clamp01(age/BoardView.DeathImpactSeconds)});
        }
        private IEnumerator DeathPolishChecks()
        {
            yield return Ready();Require(app.Automated,"death checks run with isolated input and muted output");
            Require(Path.GetFullPath(app.Library.Folder).StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase),"death checks preserve the real player profile");report.progressIsolated=true;
            app.Progress.UnlockCampaign();yield return LightTheme("wine");app.LoadLevel("c1_06");yield return Ready();
            var sun=(SpriteRenderer)DeathMember(app.Board,"sunlightIcon");var camera=app.viewCamera;
            var point=camera.WorldToScreenPoint(sun.transform.position);float uiScale=Mathf.Min(camera.pixelWidth/1280f,camera.pixelHeight/720f);
            Require(Mathf.Abs(point.x/uiScale-48)<1,"left sunlight icon anchors near the screen edge");
            float width=sun.bounds.size.x/(camera.orthographicSize*2)*camera.pixelHeight/uiScale;
            Require(Mathf.Abs(width-40)<1,"sunlight icon is enlarged from 32 to 40 UI units");Capture("sun-edge");
            var audio=app.GetComponent<GameAudio>();var burn=(AudioSource)DeathMember(audio,"burn");var effects=(AudioSource)DeathMember(audio,"effects");
            Require(burn.volume==0&&effects.volume==0,"background death tests keep both effect sources muted");
            string initial=app.Session.State.Key();yield return new WaitForSecondsRealtime(1.1f);
            for(int i=0;i<18;i++){Capture("movie-"+i.ToString("000"),960,540);yield return null;}
            app.Act(Command.Right);Require(app.Session.State.Dead&&app.Page==GamePage.Dying,"death still begins by entering the actual lit cell");
            int peakAsh=0;bool partlyBurned=false;
            for(int i=0;i<66;i++)
            {
                float age=i/60f;SampleDeath(age);peakAsh=Mathf.Max(peakAsh,LiveAsh().Length);
                if(i==21){var hero=DeathHero();partlyBurned=hero.enabled&&hero.color.a>.98f&&hero.sharedMaterial.shader.name=="DraculasBox/Vampire Burn";Require(partlyBurned,"body remains visible with spatial breakup instead of disappearing through whole-body alpha fade");}
                Capture("movie-"+(i+18).ToString("000"),960,540);yield return null;
            }
            Require(partlyBurned&&peakAsh>=10,"death has readable breakup and a bounded ash burst");
            Require(!DeathHero().enabled&&LiveAsh().Length==0&&app.Page==GamePage.Dead&&app.UI.FindButton("recover")!=null,"effect settles before the ordinary undo prompt");
            app.Act(Command.Undo);Require(app.Session.State.Key()==initial&&!burn.isPlaying,"undo restores state and stops the old burn tail");
            for(int i=0;i<24;i++)
            {
                typeof(BoardView).GetMethod("Refresh",DeathPrivate).Invoke(app.Board,new object[]{Mathf.Clamp01(i/6f)});
                Capture("movie-"+(i+84).ToString("000"),960,540);yield return null;
            }
            Require(DeathHero().enabled&&DeathHero().sharedMaterial==app.assets.material&&LiveAsh().Length==0,"undo restores the original hero material without residual ash");
            foreach(string theme in ThemeIds)
            {
                yield return LightTheme(theme);app.StartEditorPlay(app.assets.Level("c1_06"));yield return Ready();
                app.Act(Command.Right);yield return new WaitForSecondsRealtime(.28f);
                Require(DeathHero().enabled&&LiveAsh().Length>=4,"sun death remains visible in palette "+theme);Capture("death-"+theme);
                Require(burn.isPlaying&&burn.clip==app.assets.Clip("burn")&&burn.volume==effects.volume,"burn cue plays at the effect setting "+theme);
                app.Act(Command.Undo);yield return Ready();Require(!app.Session.State.Dead&&!burn.isPlaying&&DeathHero().sharedMaterial==app.assets.material,"undo during burn restores body and cancels sound "+theme);
                app.Act(Command.Right);yield return new WaitForSecondsRealtime(.24f);app.Act(Command.Reset);yield return Ready();
                Require(!app.Session.State.Dead&&!burn.isPlaying&&LiveAsh().Length==0,"reset during burn clears the old visual and audio "+theme);
            }
            yield return LightTheme("wine");
            var lamp=LevelData.Blank(9,7);lamp.player=new Cell(3,2);lamp.coffin=new Cell(7,5);lamp.pieces=new[]{new EntityData{id="lamp",kind=PieceKind.Lamp,cell=new Cell(4,4)}};
            app.StartEditorPlay(lamp);yield return Ready();app.Act(Command.Right);yield return new WaitForSecondsRealtime(.28f);
            Require(app.Session.State.Dead&&DeathHero().enabled&&LiveAsh().Length>=4,"night lamp also produces the same readable burn");Capture("lamp-burn");
            Require(Vector3.Dot((Vector3)DeathMember(app.Board,"deathLight"),Vector3.up)>.99f,"lamp burn follows the incoming vertical ray");
            app.RequestHistory(Command.Undo);yield return new WaitForSecondsRealtime(.65f);yield return Ready();
            Require(app.Page==GamePage.Playing&&!app.Session.State.Dead&&DeathHero().sharedMaterial==app.assets.material,"ordinary queued undo recovers after the death animation");
            for(int side=0;side<4;side++)
            {
                var data=LevelData.Blank(9,7);data.sunEnabled=true;data.sunFrom=(SunSide)side;data.pieces=new[]{new EntityData{id="box",cell=new Cell(4,3)}};data.player=data.pieces[0].cell+LightField.SunTravel[side];data.coffin=new Cell(7,5);
                app.StartEditorPlay(data);yield return Ready();sun=(SpriteRenderer)DeathMember(app.Board,"sunlightIcon");
                var screen=(Vector2)camera.WorldToScreenPoint(sun.transform.position);var sourceBounds=new Rect(screen-Vector2.one*20*uiScale,Vector2.one*40*uiScale);
                foreach(string id in new[]{"undo","reset","pause"})
                {
                    var button=app.UI.FindButton(id);var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    var a=(Vector2)camera.WorldToScreenPoint(corners[0]);var b=(Vector2)camera.WorldToScreenPoint(corners[2]);Require(!sourceBounds.Overlaps(Rect.MinMaxRect(a.x,a.y,b.x,b.y)),"sun marker avoids HUD hit areas "+side+" "+id);
                }
                Capture("edge-"+side);Capture("edge-4x3-"+side,1024,768);
            }
            yield return LightingRegression();yield return DeathMotionSmoke();
        }
    }
}
