using System.Collections;
using DraculasBox.Core;
using TMPro;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        public void BeginWorkshop(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report.runtime="Unity Editor workshop";report.width=Screen.width;report.height=Screen.height;folder=System.IO.Path.GetFullPath("DraculasBox/Evidence/EditorWorkshop");
            #if UNITY_EDITOR
            folder=UnityEditor.SessionState.GetString("DraculasBox.WorkshopEvidenceFolder",folder);
            #endif
            System.IO.Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;StartCoroutine(Guard(WorkshopOnly()));
        }
        private IEnumerator WorkshopOnly()
        {
            string root=System.IO.Directory.GetParent(app.Library.Folder).FullName;
            Require(root.StartsWith(System.IO.Path.GetFullPath(Application.temporaryCachePath)+System.IO.Path.DirectorySeparatorChar,System.StringComparison.OrdinalIgnoreCase),"workshop checks use an isolated temporary profile");report.progressIsolated=true;
            for(int i=0;i<GameUI.LibraryPageSize+2;i++){var data=LevelData.Blank();data.title="布局 "+(i+1);app.Library.Save(data);}
            System.IO.File.WriteAllText(System.IO.Path.Combine(app.Library.Folder,"broken-layout.json"),"{broken");
            app.OpenLibrary();yield return Ready();yield return EditorRegression();yield return RectangleRegression();yield return WorkshopRegression();yield return DeathMotionSmoke();yield return LightingRegression();report.editorRoundtrip=true;
        }
        private IEnumerator DeathMotionSmoke()
        {
            app.StartEditorPlay(app.assets.Level("c1_06"));yield return Ready();var backdrop=app.viewCamera.backgroundColor;var root=app.Board.transform.Find("Board");var hud=(RectTransform)app.UI.FindButton("undo").transform;var anchor=hud.anchoredPosition;float zoom=1,pulse=0;var spawn=app.Session.State.Key();
            Press(KeyCode.RightArrow);yield return null;yield return null;key=KeyCode.None;
            for(int i=0;i<42;i++){zoom=Mathf.Max(zoom,root.localScale.x);var d=app.viewCamera.backgroundColor-backdrop;pulse=Mathf.Max(pulse,d.r);Require(Mathf.Abs(d.r-d.g)<.002f&&Mathf.Abs(d.g-d.b)<.002f,"death pulse does not shift the background hue");Require(hud.anchoredPosition==anchor&&app.UI.Root.Find("Burn vignette")==null,"death keeps controls fixed without a red overlay");yield return null;}
            yield return Ready();Require(app.Page==GamePage.Dead&&app.Session.State.Dead,"light remains lethal after the visual change");Require(zoom>1.006f&&zoom<=1.019f&&pulse>0&&pulse<.029f,"A feedback applies the approved restrained zoom and pulse");Capture("workshop_death_prompt");yield return Tap(KeyCode.Z);Require(app.Session.State.Key()==spawn&&Vector4.Distance(app.viewCamera.backgroundColor,backdrop)<.002f&&Mathf.Abs(root.localScale.x-1)<.001f,"death undo restores layout, camera and background");app.ReturnToEditor();yield return Ready();
        }
        private IEnumerator WorkshopRegression()
        {
            app.Edit(LevelData.Blank());yield return Ready();var editor=app.Creator;var doc=editor.Document;var baseline=doc.Data.Copy();
            Require(!editor.SceneOpen&&app.UI.FindButton("sun")==null,"scene properties start collapsed");
            Require(Vector4.Distance(app.viewCamera.backgroundColor,GameUI.WorkshopGround)<.001f,"editor uses the selected pale theme");
            Require(app.UI.FindButton("shape-brush").GetComponentInChildren<TMP_Text>().text=="点涂","shape is named independently from paint operation");
            yield return Click("shape-rectangle");
            Require(editor.Operation==EditOperation.Paint&&editor.Selected==Brush.Box&&editor.Shape==PaintShape.Rectangle,"rectangle erasing keeps material and shape independent");
            var a=app.Board.Screen(new Cell(4,2));var b=app.Board.Screen(new Cell(6,4));editor.PointerSample(a,true,true,false,true);editor.PointerSample(b,false,true,false,true);
            Require(doc.Data.ContentEquals(baseline),"erase rectangle previews without changing the draft");Capture("workshop_erase_rectangle");editor.PointerSample(b,false,false,true,true);yield return null;
            Require(doc.Data.pieces.Length==0&&doc.UndoCount==1,"erase rectangle commits once across the covered pieces");editor.Undo();yield return null;Require(doc.Data.ContentEquals(baseline),"erase rectangle undo restores complete content");
            yield return Click("operation-paint");Require(editor.Selected==Brush.Box&&editor.Shape==PaintShape.Rectangle,"return to painting retains box material and rectangle");
            a=app.Board.Screen(new Cell(4,2));b=app.Board.Screen(new Cell(6,4));editor.PointerSample(a,true,true,false);editor.PointerSample(b,false,true,false);editor.PointerSample(b,false,false,true);yield return null;
            Require(doc.Data.pieces.Length==9&&doc.UndoCount==1,"rectangle painting fills available cells as one undo step");editor.Undo();yield return null;
            yield return Click("shape-brush");yield return Click("scene");Require(editor.SceneOpen,"scene popover opens on demand");var before=doc.Data.Copy();editor.PointerSample(app.Board.Screen(new Cell(1,1)),true,true,false);Require(!editor.SceneOpen&&doc.Data.ContentEquals(before),"outside click closes properties without painting behind them");
            yield return Click("scene");app.Back();Require(app.Page==GamePage.Editing&&!editor.SceneOpen,"escape closes scene properties before leaving the editor");
            yield return Click("palette-toggle");Require(app.UI.FindButton("brush-2")==null,"collapsed material shelf does not intercept canvas input");yield return Click("palette-toggle");Require(app.UI.FindButton("brush-2")!=null,"material shelf reopens without losing the draft");
            Require(baseline.ContentEquals(editor.Document.Data),"palette changes preserve edited content");
            Capture("workshop_editor_final");app.Creator.Play();yield return Ready();Require(app.Page==GamePage.Playing,"pale editor trial enters ordinary gameplay");app.ReturnToEditor();yield return Ready();Require(editor.Document.Data.ContentEquals(before),"trial returns to the exact draft");
            app.LeaveEditor();yield return Ready();if(app.Page==GamePage.Confirming){yield return Click("discard-leave");}Require(app.Page==GamePage.Library,"workshop returns to the pale level library");Capture("workshop_library_final");
        }
    }
}
