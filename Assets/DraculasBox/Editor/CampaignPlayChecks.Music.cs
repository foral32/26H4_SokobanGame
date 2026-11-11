using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEngine;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunMusic()=>Begin(MusicChecks(),"music",true);
        [Serializable] private sealed class MusicTrace
        {public List<MusicFrame> frames=new();}
        [Serializable] private sealed class MusicFrame
        {public float seconds,main,editor;public int mainSample,editorSample;public string page;}
        private static IEnumerator MusicChecks()
        {
            CreateFixture();Bind(fixture);
            var music=app.GetComponent<GameAudio>();
            var sounds=music.GetComponentsInChildren<AudioSource>();
            var main=sounds.Single(s=>s.clip==fixture.mainMusic);
            var editor=sounds.Single(s=>s.clip==fixture.editorMusic);
            var effects=sounds.Single(s=>s.gameObject.name=="Effects");
            var burn=sounds.Single(s=>s.gameObject.name=="Burn");
            // Test source gains while keeping this background session inaudible.
            foreach(var source in sounds)source.mute=true;
            music.Volume(.4f,.55f);yield return new WaitForSecondsRealtime(.5f);
            Need(fixture.mainMusic.name=="HypnoticJewels"&&fixture.editorMusic.name=="PuzzleTune1B","the selected main track and exact B variation are wired into GameAssets");
            Need(fixture.audio.All(c=>c!=fixture.mainMusic&&c!=fixture.editorMusic&&c.name!="Underclocked"&&c.name!="Firmament_loopable"&&c.name!="Covert Affair_Loop"),"reserve and retired music are absent from the player's sound-effect references");
            Need(Mathf.Abs(fixture.editorMusic.length-48)<.001f&&main.loop&&editor.loop,"the editor loop has no MP3 padding and both tracks use native looping");
            Need(music.Context==MusicContext.Main&&main.isPlaying&&editor.isPlaying&&Mathf.Abs(main.volume-.4f)<.002f&&editor.volume<.001f,"title starts the main loop while the editor loop advances silently");
            Need(Mathf.Abs(effects.volume-.55f)<.001f&&effects.spatialBlend==0,"sound-effect volume and two-dimensional playback are preserved");
            Need(Mathf.Abs(burn.volume-.55f)<.001f&&burn.spatialBlend==0&&burn.mute,"burn has the same effects gain and stays inaudible during background music checks");
            int mainBefore=main.timeSamples,editorBefore=editor.timeSamples;
            yield return Click("play");yield return Click("chapter-chapter_1");yield return Click("level-c1_01");yield return Page(GamePage.Playing);
            Need(music.Context==MusicContext.Main&&main.timeSamples>mainBefore&&editor.timeSamples>editorBefore,"normal menus and level entry keep both playheads advancing");
            app.Pause();yield return Click("settings");yield return Click("music-credits");
            Need(music.Context==MusicContext.Main&&app.Page==GamePage.Confirming&&app.UI.FindButton("music-source-main")!=null,"pause, settings and music attribution keep the current track");
            yield return new WaitForSecondsRealtime(.22f);Capture("music-credits");
            yield return Click("music-credits-back");yield return Click("back");yield return Click("resume");
            Need(music.Context==MusicContext.Main&&main.volume>.399f&&editor.volume<.001f,"returning from attribution and pause causes no music switch");
            foreach(char command in new CampaignProject(fixture).Source.Plan("c1_01").route)app.Act((Command)"URDL".IndexOf(command));
            yield return new WaitForSecondsRealtime(1.1f);yield return Ready();
            Need(music.Context==MusicContext.Main&&main.timeSamples>mainBefore,"automatic level completion keeps the same main-music playhead");
            app.ShowTitle();yield return Page(GamePage.Title);

            mainBefore=main.timeSamples;editorBefore=editor.timeSamples;
            app.OpenLibrary();float start=Time.unscaledTime;bool overlap=false,power=true;
            var trace=new MusicTrace();
            while(Time.unscaledTime-start<1.4f)
            {
                overlap|=main.volume>.02f&&editor.volume>.02f;
                power&=Mathf.Abs(main.volume*main.volume+editor.volume*editor.volume-.16f)<.003f;
                trace.frames.Add(new MusicFrame{seconds=Time.unscaledTime-start,main=main.volume,editor=editor.volume,mainSample=main.timeSamples,editorSample=editor.timeSamples,page=app.Page.ToString()});
                yield return null;
            }
            File.WriteAllText(Evidence+"/music-crossfade.json",JsonUtility.ToJson(trace,true));
            Need(overlap&&power,"switching to creation overlaps both tracks without a gain jump or a dip in combined power");
            Need(app.Page==GamePage.Library&&music.Context==MusicContext.Editor&&main.volume<.001f&&Mathf.Abs(editor.volume-.4f)<.002f,"the crossfade settles within 0.9 seconds of changing context");
            Need(main.timeSamples>mainBefore&&editor.timeSamples>editorBefore&&editor.timeSamples>fixture.editorMusic.frequency,"the incoming editor music joins its existing playhead instead of restarting");
            yield return Click("template");yield return Click("copy-level-0");yield return Click("copy-confirm");yield return Page(GamePage.Editing);editorBefore=editor.timeSamples;
            app.Settings();yield return Ready();yield return Click("music-credits");yield return Click("music-credits-back");yield return Click("back");
            Need(app.Page==GamePage.Editing&&music.Context==MusicContext.Editor&&editor.timeSamples>editorBefore,"editor settings and credits return to the original editing music");
            yield return Click("test");yield return Page(GamePage.Playing);yield return new WaitForSecondsRealtime(.14f);
            Need(music.Context==MusicContext.Editor&&editor.volume>.399f&&main.volume<.001f,"F5 editor trials retain editor music without an unnecessary crossfade");
            app.ReturnToEditor();yield return Page(GamePage.Editing);
            Need(editor.timeSamples>editorBefore,"returning from a trial does not restart the editor loop");

            music.Music(MusicContext.Main);yield return new WaitForSecondsRealtime(.16f);
            float beforeMain=main.volume,beforeEditor=editor.volume;music.Music(MusicContext.Editor);
            Need(Mathf.Abs(main.volume-beforeMain)<.001f&&Mathf.Abs(editor.volume-beforeEditor)<.001f,"reversing an in-progress crossfade preserves the instantaneous gains");
            yield return new WaitForSecondsRealtime(1f);
            Need(editor.volume>.399f&&main.volume<.001f,"a rapid reversal settles on the final requested context");
            mainBefore=main.timeSamples;editorBefore=editor.timeSamples;music.Volume(0,.22f);yield return null;yield return null;
            Need(main.volume==0&&editor.volume==0&&Mathf.Abs(effects.volume-.22f)<.001f&&Mathf.Abs(burn.volume-.22f)<.001f,"muting music immediately silences both legs independently of effects");
            yield return new WaitForSecondsRealtime(.14f);
            Need(main.timeSamples>mainBefore&&editor.timeSamples>editorBefore,"muting keeps the musical timelines running");
            music.Volume(.4f,.55f);yield return new WaitForSecondsRealtime(.4f);
            Need(editor.volume>.399f&&main.volume<.001f,"unmuting ramps back into the current musical position");
            main.timeSamples=main.clip.samples-main.clip.frequency/10;
            editor.timeSamples=editor.clip.samples-editor.clip.frequency/10;
            yield return new WaitForSecondsRealtime(.4f);
            Need(main.isPlaying&&editor.isPlaying&&main.time<1&&editor.time<1,"both compressed clips wrap through their native loop boundaries");
            music.enabled=false;int held=editor.timeSamples;yield return new WaitForSecondsRealtime(.12f);
            Need(!main.isPlaying&&!editor.isPlaying&&Mathf.Abs(editor.timeSamples-held)<64,"disabling the audio component pauses its owned tracks");
            music.enabled=true;yield return new WaitForSecondsRealtime(.18f);
            Need(editor.isPlaying&&editor.timeSamples>held,"reenabling resumes the previous playhead");
            editorBefore=editor.timeSamples;music.Initialize(original);yield return new WaitForSecondsRealtime(.15f);
            Need(music.GetComponentsInChildren<AudioSource>().Length==4&&editor.timeSamples>editorBefore,"repeated initialization neither duplicates sources nor restarts identical music");
            music.Cue("not-a-real-sound");
            var incomplete=ScriptableObject.CreateInstance<GameAssets>();incomplete.mainMusic=original.mainMusic;incomplete.audio=Array.Empty<AudioClip>();
            var probe=new GameObject("Music fallback check");var fallback=probe.AddComponent<GameAudio>();fallback.Initialize(incomplete);fallback.Music(MusicContext.Editor);
            bool preserved=fallback.Context==MusicContext.Main;
            UnityEngine.Object.Destroy(probe);UnityEngine.Object.Destroy(incomplete);
            Need(preserved,"a missing editor clip falls back to the available main music");
            app.ShowTitle();yield return Page(GamePage.Title);yield return new WaitForSecondsRealtime(1f);
            Need(music.Context==MusicContext.Main&&main.volume>.399f&&editor.volume<.001f,"leaving the editing workflow crossfades back to main music");
            yield return Click("campaign");yield return Page(GamePage.Campaign);yield return new WaitForSecondsRealtime(1f);
            Need(music.Context==MusicContext.Editor&&editor.volume>.399f,"official chapter authoring uses the editor music as well");
        }
    }
}
