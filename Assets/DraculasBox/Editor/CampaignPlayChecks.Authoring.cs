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
        public static void RunAuthoringTools()=>Begin(AuthoringTools(),"level-tools",true);

        private static IEnumerator WaitUntilReady(Func<bool> condition,string label)
        {
            float until=Time.realtimeSinceStartup+22;
            while(!condition())
            {
                if(Time.realtimeSinceStartup>=until)throw new TimeoutException(label);
                yield return null;
            }
        }
        private static bool CanClick(string id)=>app.UI.FindButton(id)?.interactable==true;
        private static IEnumerator AuthoringTools()
        {
            CreateFixture();Bind(fixture);
            yield return Click("creator");yield return Page(GamePage.Library);
            yield return Click("library-generate");yield return Click("close-authoring");yield return Page(GamePage.Library);
            Need(app.Library.Read(out _).Count==0&&app.Library.Recovery()==null,"cancelled preview leaves the personal library and recovery empty");
            yield return Click("library-generate");
            yield return Click("mechanic-0");yield return Click("complexity-2");
            yield return WaitUntilReady(()=>CanClick("adopt-draft"),"draft preview");
            yield return Click("adopt-draft");yield return Page(GamePage.Editing);
            var doc=app.Creator.Document;var generated=doc.Data.Copy();
            Need(!generated.sunEnabled&&DraftPresets.All.Any(p=>p.Title==generated.title),"selected allowed mechanics produce a curated room without sunlight");
            Need(LevelSolver.Proves(generated,generated.solution,out _),"adopted layout has a winning route");
            yield return Click("save");yield return Click("leave-editor");yield return Page(GamePage.Library);
            var saved=app.Library.Read(out _).Single();
            Need(saved.ContentEquals(generated),"save and reopen retain route");
            var imported=app.Library.Import(app.Library.PreviewImport(app.Library.Export(saved)));
            Need(imported.id!=saved.id&&imported.solution==saved.solution&&imported.LayoutEquals(saved),"export/import gives the same solution a new library identity");
            app.Edit(saved);yield return Page(GamePage.Editing);
            doc=app.Creator.Document;
            yield return Click("drafts");yield return WaitUntilReady(()=>CanClick("adopt-draft"),"replacement preview");
            yield return Click("adopt-draft");
            Need(doc.UndoCount==1&&doc.Data.id==saved.id&&doc.Data.title==saved.title,"replace keeps identity and creates one edit");
            yield return Click("edit-undo");Need(doc.Data.ContentEquals(saved)&&!doc.Dirty,"one undo recovers the complete original");
            yield return Click("solutions");yield return WaitUntilReady(()=>CanClick("play-solution"),"saved proof verification");
            yield return Click("solve-level");yield return WaitUntilReady(()=>CanClick("play-solution"),"shortest path search");
            var beforePlayback=doc.Data.Copy();int history=doc.UndoCount;string progress=JsonUtility.ToJson(app.Progress.Data);
            yield return Click("play-solution");yield return Click("play-pause");
            var counter=app.UI.Root.Find("Playback steps").GetComponent<TMP_Text>();
            string paused=counter.text;yield return Click("next-step");Need(counter.text!=paused,"pause and forward step");
            yield return Click("previous-step");Need(counter.text==paused,"backward step");
            yield return Click("play-pause");yield return WaitUntilReady(()=>app.Board.State.Won,"automatic playback to coffin");
            yield return Click("stop-playback");
            Need(doc.Data.ContentEquals(beforePlayback)&&doc.UndoCount==history&&JsonUtility.ToJson(app.Progress.Data)==progress,"watching a solution preserves the board, edit history and player progress");
            yield return Click("save");yield return Click("leave-editor");yield return Page(GamePage.Library);
            app.ShowTitle();yield return Page(GamePage.Title);yield return Click("campaign");yield return Chapter("chapter_1");
            yield return Click("select-level-c1_01");yield return Page(GamePage.Editing);
            doc=app.Creator.Document;
            Need(!doc.Dirty&&doc.Data.solution==new CampaignProject(fixture).Source.Plan("c1_01").route,"legacy official replay opens without a dirty marker");
            string legacy=doc.Data.solution;
            doc.Begin();doc.Paint(new Cell(0,0),Brush.Void);doc.End();
            Need(string.IsNullOrEmpty(doc.Data.solution),"layout edit invalidates the staged proof");
            yield return Click("save");
            Need(!doc.Dirty&&doc.Data.solution==legacy,"saving revalidates a still-working official route and reflects it back into the editor");
            yield return Click("drafts");yield return WaitUntilReady(()=>CanClick("adopt-draft"),"official preview");yield return Click("adopt-draft");
            doc.DiscardSolution();yield return Click("solutions");Need(app.UI.FindButton("play-solution")==null,"no playback action without a route");
            yield return Click("solve-level");yield return WaitUntilReady(()=>CanClick("play-solution"),"calculate official proof");
            var official=doc.Data.Copy();yield return Click("close-authoring");yield return Click("save");yield return Click("leave-editor");
            yield return Click("campaign-review");yield return Click("campaign-publish");
            var published=new CampaignProject(fixture).Source;var level=published.Board(official.id);var plan=published.Plan(official.id);
            Need(string.IsNullOrEmpty(level.solution)&&level.LayoutEquals(official)&&plan.route==official.solution
                &&LevelSolver.Proves(level,plan.route,out int pushes)&&pushes==plan.pushes,"official write and reimport preserve one verified route in the replay catalog");
        }
    }
}
