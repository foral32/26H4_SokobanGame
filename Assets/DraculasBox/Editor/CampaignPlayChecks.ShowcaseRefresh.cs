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
        // Refresh only the takes whose subject changed, using the existing isolated profile.
        public static void RunShowcaseMediaRefreshCapture()=>Begin(RefreshShowcaseMedia(),"showcase-media-refresh",false);
        private static IEnumerator RefreshShowcaseMedia()
        {
            yield return RecordMenuShowcase();yield return Ready();yield return RefreshEditorMedia();
        }
        public static void RunShowcaseEditorRefreshCapture()=>Begin(RefreshEditorMedia(),"showcase-editor-refresh",false);
        private static IEnumerator RefreshEditorMedia()
        {
            recorder=app.gameObject.AddComponent<ShowcaseRecorder>();recorder.app=app;
            try
            {
                var room=LevelData.Blank(11,7);room.title="我的关卡";room.player=new Cell(2,5);room.coffin=new Cell(8,5);
                room.pieces=new[]{new EntityData{id="upper",cell=new Cell(3,2)},new EntityData{id="lower",cell=new Cell(3,4)},new EntityData{id="between",cell=new Cell(5,3)}};
                app.Edit(room);yield return Page(GamePage.Editing);yield return Hold(.7f);
                recorder.Begin("td01");yield return Hold(.8f);
                yield return FilmClick("shape-rectangle");yield return Stroke(new Cell(6,1),new Cell(7,2));
                Need(app.Creator.Document.Data.pieces.Length==7,"recording paints with the left button");
                yield return Stroke(new Cell(6,1),new Cell(7,1),true);
                Need(app.Creator.Document.Data.pieces.Length==5&&app.Creator.Operation==EditOperation.Paint,"recording erases with the right button in the same mode");
                yield return FilmClick("edit-undo",.4f);yield return FilmClick("edit-undo",.5f);
                yield return FilmClick("operation-select",.5f);
                yield return Stroke(new Cell(2,1),new Cell(3,4));yield return Stroke(new Cell(3,2),new Cell(5,2));
                Need(app.Creator.Document.Data.pieces[0].cell==new Cell(5,2)&&app.Creator.Document.Data.pieces[1].cell==new Cell(5,4)&&app.Creator.Document.Data.pieces[2].cell==new Cell(5,3),"selection film preserves the object in its empty gap");
                yield return Hold(.5f);yield return FilmClick("edit-undo",.4f);
                yield return FilmClick("brush-1",.4f);yield return FilmClick("shape-brush",.4f);
                yield return Stroke(new Cell(11,2),new Cell(11,2));
                Need(app.Creator.Document.Data.rows[0].Length==12&&app.Creator.Document.Data.rows[0][11]=='.',"outside placement extends with floor padding");
                app.Creator.ShowLineControls(true,6);app.Board.LinePreview(true,7,true);yield return Hold(.5f);
                yield return FilmClick("line-insert",.7f);
                app.Creator.ShowLineControls(false,4);app.Board.LinePreview(false,4,false);yield return Hold(.5f);
                yield return FilmClick("line-delete",.6f);yield return FilmClick("edit-undo",.5f);yield return FilmClick("edit-undo",.5f);
                recorder.HidePointer();yield return Hold(.8f);recorder.End();
                app.ShowTitle();yield return Page(GamePage.Title);
            }
            finally{if(recorder!=null){recorder.End();UnityEngine.Object.Destroy(recorder);recorder=null;}}
        }
        public static void RunShowcaseReadmeRefresh()=>Begin(RefreshShowcaseReadme(),"showcase-readme-refresh",false);
        private static IEnumerator RefreshShowcaseReadme()
        {
            app.ShowTitle();yield return Page(GamePage.Title);yield return Hold(.5f);
            string folder="Document/Images/Readme/";Directory.CreateDirectory(folder);
            File.Copy(driver.Capture("readme-main-menu-refreshed"),folder+"main-menu.png",true);
            yield return Click("showcase");app.UI.ShowcaseSelectTab(0);yield return Hold(.4f);ShowcaseTextFits();
            File.Copy(driver.Capture("readme-showcase-intro-refreshed"),folder+"showcase-intro.png",true);
            yield return DemoClick("showcase-tab-2");yield return DemoClick("showcase-dot-0");yield return Hold(.8f);ShowcaseTextFits();
            File.Copy(driver.Capture("readme-showcase-refreshed"),folder+"showcase.png",true);
            Need(app.UI.ShowcasePageId=="td01","README captures the updated editor demonstration page");
            yield return DemoClick("showcase-close");yield return Hold(.3f);
        }
    }
}
