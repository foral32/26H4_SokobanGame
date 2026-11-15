using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Runtime;
using UnityEngine;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunToolEntryChecks()=>Begin(ToolEntries(),"tool-entries",false);
        private static IEnumerator ToolEntries()
        {
            yield return Ready();
            Need(!app.toolsOnlyInEditor&&app.ToolEntriesVisible,"tool policy defaults to Editor and player");
            Need(DraculaApp.CreateCampaignWorkshop!=null,"runtime authoring provider registered");
            Need(app.UI.FindButton("campaign")!=null&&app.UI.FindButton("showcase")!=null,"both tool entries available");
            Need(!app.UI.FindButton("campaign").CompareTag("EditorOnly")&&!app.UI.FindButton("showcase").CompareTag("EditorOnly"),"entries survive player scene stripping");
            string source=app.assets.catalog.text,proofs=app.assets.replays.text;
            var clone=UnityEngine.Object.Instantiate(app.assets);
            string folder=Path.Combine(Application.temporaryCachePath,"DraculasBoxToolChecks",Guid.NewGuid().ToString("N"));
            var local=new CampaignProject(clone,folder);
            Need(local.Source.boards.Length==app.assets.levels.Length,"local storage seeds the complete embedded campaign");
            var changed=CampaignPublication.Copy(local.Source);string id=changed.boards[0].id;
            changed.boards[0].note="local publication check";
            local.Publish(changed,local.Revision);
            Need(clone.Level(id).note=="local publication check","publication updates the live player catalogue");
            var restart=UnityEngine.Object.Instantiate(app.assets);var reopened=new CampaignProject(restart,folder);
            Need(restart.Level(id).note=="local publication check","player catalogue persists across reloads");
            string before=File.ReadAllText(Path.Combine(folder,"Levels",id+".json"));
            var failing=CampaignPublication.Copy(reopened.Source);failing.Board(id).note="rollback check";
            bool rejected=false;
            try{reopened.Publish(failing,reopened.Revision,()=>throw new IOException("expected interruption"));}
            catch(IOException){rejected=true;}
            Need(rejected&&File.ReadAllText(Path.Combine(folder,"Levels",id+".json"))==before&&restart.Level(id).note=="local publication check","local publication rolls back interrupted writes");
            Need(app.assets.catalog.text==source&&app.assets.replays.text==proofs,"local editing leaves embedded project assets untouched");
            UnityEngine.Object.Destroy(clone);UnityEngine.Object.Destroy(restart);
            yield return Click("showcase");yield return Hold(.3f);
            Need(app.UI.ShowcaseOpen&&app.UI.ShowcasePageId=="intro","shipped showcase JSON opens from StreamingAssets");
            yield return Press(KeyCode.RightArrow);
            yield return WaitUntilReady(()=>app.UI.ShowcaseVideoReady,"shipped showcase video prepares");
            Need(app.UI.ShowcasePageId=="ld01","shipped media supports cross-section navigation");
            yield return Press(KeyCode.Escape);yield return Hold(.3f);
            Need(!app.UI.ShowcaseOpen,"shipped showcase closes and releases the modal");
        }
    }
}
