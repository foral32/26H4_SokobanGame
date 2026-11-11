using System;
using System.Collections;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunPresets()=>Begin(CuratedPresetTools(),"draft-presets",false);
        private static IEnumerator CuratedPresetTools()
        {
            string progress=JsonUtility.ToJson(app.Progress.Data);
            yield return Click("creator");yield return Page(GamePage.Library);
            yield return Click("library-generate");yield return WaitUntilReady(()=>CanClick("adopt-draft"),"initial preset");
            var status=app.UI.Root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Draft status");
            string previous=status.text;
            Capture("preset-picker");
            for(int i=0;i<4;i++)
            {
                yield return Click("reroll");yield return WaitUntilReady(()=>CanClick("adopt-draft"),"different preset");
                Need(status.text!=previous,"reroll changes authored room, not just orientation: "+i);previous=status.text;
            }
            yield return Click("close-authoring");yield return Page(GamePage.Library);
            Need(app.Library.Read(out _).Count==0,"browsing and cancelling create no files");
            yield return Click("library-generate");yield return WaitUntilReady(()=>CanClick("adopt-draft"),"adoptable preset");
            yield return Click("adopt-draft");yield return Page(GamePage.Editing);
            var created=app.Creator.Document.Data.Copy();
            Need(DraftPresets.All.Any(p=>p.Title==created.title)&&LevelSolver.Proves(created,created.solution,out _),"adopt preserves authored title and proof");
            yield return Click("save");yield return Click("leave-editor");yield return Page(GamePage.Library);
            var saved=app.Library.Read(out _).Single();Need(saved.ContentEquals(created),"preset and route persist to library");
            var imported=app.Library.Import(app.Library.PreviewImport(app.Library.Export(saved)));
            Need(imported.id!=saved.id&&imported.LayoutEquals(saved)&&LevelSolver.Proves(imported,imported.solution,out _),"export/import preserves solution with new identity");
            app.Edit(saved);yield return Page(GamePage.Editing);
            var doc=app.Creator.Document;yield return Click("drafts");yield return WaitUntilReady(()=>CanClick("adopt-draft"),"replacement preset");
            yield return Click("adopt-draft");
            Need(doc.UndoCount==1&&doc.Data.id==saved.id&&doc.Data.title==saved.title,"replacement is a single edit and preserves current identity");
            yield return Click("edit-undo");Need(doc.Data.ContentEquals(saved),"undo restores full board and proof");
            yield return Click("solutions");yield return WaitUntilReady(()=>CanClick("play-solution"),"stored solution");
            yield return Click("play-solution");yield return WaitUntilReady(()=>app.Board.State.Won,"preset solution playback");
            yield return Click("stop-playback");Need(doc.Data.ContentEquals(saved),"solution playback leaves source unchanged");
            foreach(var preset in DraftPresets.All)
            {
                var data=preset.Create();app.Edit(data);yield return Page(GamePage.Editing);
                yield return Click("test");yield return Page(GamePage.Playing);
                Capture("preset-"+preset.Id);
                foreach(char move in data.solution)
                {
                    yield return Ready();int moves=app.Session.Moves;
                    app.Act((Command)"URDL".IndexOf(move));
                    Need(app.Session.Moves==moves+1&&!app.Session.State.Dead,"animated play "+preset.Id+" step "+(moves+1));
                }
                Need(app.Session.State.Won&&app.Page==GamePage.Completing,"trial reaches unlit coffin: "+preset.Title);
                yield return Page(GamePage.Editing);
                Need(app.Creator.Document.Data.LayoutEquals(data)&&LevelSolver.Proves(app.Creator.Document.Data,app.Creator.Document.Data.solution,out _),"trial returns with valid draft: "+preset.Title);
            }
            Need(JsonUtility.ToJson(app.Progress.Data)==progress,"all trials preserve campaign progress");
            app.ShowTitle();yield return Page(GamePage.Title);
            app.UI.Clear();app.UI.WorkshopTheme();
            app.UI.Text("Preset review title","草稿预设",new Vector2(0,320),new Vector2(600,44),30,GameUI.WorkshopInk);
            for(int i=0;i<DraftPresets.All.Count;i++)
            {
                var preset=DraftPresets.All[i];float x=-480+i%5*240,y=205-i/5*205;
                app.UI.Thumbnail("preset-"+i,preset.Create(),new Vector2(x,y),new Vector2(216,164));
                app.UI.Text("preset-name-"+i,preset.Title+" · "+preset.Difficulty,new Vector2(x,y-93),new Vector2(230,32),20,GameUI.WorkshopInk);
            }
            yield return Ready();Capture("preset-catalogue");
            app.ShowTitle();yield return Page(GamePage.Title);
        }
    }
}
