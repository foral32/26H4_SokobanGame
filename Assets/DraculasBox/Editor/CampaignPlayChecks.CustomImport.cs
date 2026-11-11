using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEngine;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunCustomImport()=>Begin(CustomImport(),"custom-import",true);
        private static IEnumerator CustomImport()
        {
            CreateFixture();Bind(fixture);
            var source=new CampaignProject(fixture).Source;
            string originalJson=JsonUtility.ToJson(source);
            yield return Click("campaign");yield return Chapter("chapter_1");
            yield return Click("campaign-import-custom");
            Need(!CanClick("custom-import-confirm")&&!CanClick("custom-import-item-0"),"empty library has no selectable entry or enabled confirm");
            yield return Click("custom-import-cancel");
            var levels=DraftPresets.All.Take(5).Select(p=>p.Create()).ToArray();
            for(int i=0;i<levels.Length;i++){levels[i].title="草稿 "+(i+1)+" · "+levels[i].title;app.Library.Save(levels[i]);}
            string personal=JsonUtility.ToJson(levels[0]);
            yield return Click("campaign-import-custom");
            yield return Click("custom-import-next");
            Need(app.UI.FindButton("custom-import-item-0").gameObject.activeSelf&&!CanClick("custom-import-item-1"),"last page only exposes existing saved levels");
            yield return Click("custom-import-prev");yield return Click("custom-import-item-0");
            Need(CanClick("custom-import-confirm"),"selection enables explicit import");
            Capture("custom-import-picker");
            yield return Click("custom-import-cancel");
            Need(JsonUtility.ToJson(Draft()?.working??new CampaignProject(fixture).Source)==originalJson,"browsing and cancelling preserve campaign");
            yield return Click("campaign-import-custom");yield return Click("custom-import-item-0");
            yield return Click("custom-import-confirm");
            var data=(Draft()?.working??new CampaignProject(fixture).Source);string imported=data.boards.Single(b=>!source.boards.Any(s=>s.id==b.id)).id;
            var board=data.Board(imported);var plan=data.Plan(imported);
            Need(imported!=levels[0].id&&board.title==levels[0].title&&board.LayoutEquals(levels[0]),"import copies selected layout and title under fresh official identity");
            Need(string.IsNullOrEmpty(board.solution)&&plan!=null&&LevelSolver.Proves(board,plan.route,out int pushes)&&pushes==plan.pushes,"valid personal proof becomes verified campaign replay");
            Need(CampaignDocument.Main(data.Chapter("chapter_1"))[1].id==imported,"import inserts after selected mainline node");
            Need(JsonUtility.ToJson(app.Library.Read(out _).First())==personal,"source custom file remains unchanged");
            Capture("custom-import-map");
            yield return Click("campaign-undo");Need((Draft()?.working??new CampaignProject(fixture).Source).boards.Length==source.boards.Length,"one undo removes board, link and proof");
            yield return Click("campaign-redo");Need((Draft()?.working??new CampaignProject(fixture).Source).Plan(imported)!=null,"redo restores imported identity and proof");
            yield return Click("select-level-"+imported);yield return Page(GamePage.Editing);
            Need(app.Creator.Document.Data.solution==plan.route&&!app.Creator.Document.Dirty,"import opens as saved editable board with solution");
            yield return Click("leave-editor");
            yield return Click("campaign-review");yield return Click("campaign-publish");
            var published=new CampaignProject(fixture).Source;
            Need(published.Board(imported).LayoutEquals(levels[0])&&published.Plan(imported).route==plan.route,"writeback imports copied asset and replay into fixture project");
            // Same source can be copied twice; duplicate names never alias campaign identity.
            yield return Click("campaign-import-custom");yield return Click("custom-import-item-0");yield return Click("custom-import-confirm");
            Need((Draft()?.working??new CampaignProject(fixture).Source).boards.Count(b=>b.title==levels[0].title)==2,"second import preserves first copy and creates independent identity");
            yield return Click("campaign-undo");
            // File changed while the preview was open: require a fresh selection.
            yield return Click("campaign-import-custom");yield return Click("custom-import-item-0");
            levels[0].title+=" 更新";app.Library.Save(levels[0]);
            yield return Click("custom-import-confirm");
            Need(!CanClick("custom-import-confirm")&&(Draft()?.working??published).boards.Length==published.boards.Length,"changed source clears stale selection without mutating campaign");
            yield return Click("custom-import-item-0");app.Library.Archive(levels[0].id);
            yield return Click("custom-import-confirm");
            Need(!CanClick("custom-import-confirm")&&(Draft()?.working??published).boards.Length==published.boards.Length,"removed source is not imported from stale preview");
            yield return Click("custom-import-cancel");
            levels[1].solution="UUUU";app.Library.Save(levels[1]);
            yield return Click("campaign-import-custom");yield return Click("custom-import-item-0");yield return Click("custom-import-confirm");
            data=(Draft()?.working??new CampaignProject(fixture).Source);var unfinished=data.boards.Single(b=>!published.boards.Any(s=>s.id==b.id));
            Need(data.Plan(unfinished.id)==null&&CampaignProject.PublicationIssues(data).Any(i=>i.id==unfinished.id),"stale solution does not block staging but fails publication validation");
            yield return Click("campaign-undo");
            // Transaction failure after insertion must not leak an orphan board or undo entry.
            var isolated=new CampaignDocument(JsonUtility.FromJson<CampaignContent>(JsonUtility.ToJson(published)));
            try{isolated.Change(()=>{isolated.ImportLevel(isolated.Data.catalog.chapters[0],null,levels[2]);throw new IOException("simulated persistence failure");});}catch(IOException){}
            Need(JsonUtility.ToJson(isolated.Data)==JsonUtility.ToJson(published)&&!isolated.CanUndo,"failed import transaction restores complete document");
            Need(app.Library.Read(out _).Count==4,"campaign operations neither move nor overwrite custom library files");
        }
    }
}
