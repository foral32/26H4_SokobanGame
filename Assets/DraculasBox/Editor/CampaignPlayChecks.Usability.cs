using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunUsability()=>Begin(Usability(),"usability",true);
        private static CampaignContent UsabilityWorking()=>Draft()?.working??new CampaignProject(fixture).Source;
        private static RectTransform FindRect(string name)=>app.UI.Root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r=>r.name==name);
        private static IEnumerator UsabilityDrag(string source,string target,Vector2 offset,Vector2 grabOffset,string shot=null,bool cancel=false,bool outside=false)
        {
            yield return Ready();app.UI.FocusButton("select-level-"+source);
            var from=app.UI.FindButton("select-level-"+source).gameObject;
            var sourceRect=(RectTransform)from.transform;
            var destination=(RectTransform)app.UI.FindButton("select-level-"+target).transform;
            Vector3 originalCenter=sourceRect.position;
            var start=RectTransformUtility.WorldToScreenPoint(app.viewCamera,sourceRect.TransformPoint(grabOffset));
            var screenGrab=start-RectTransformUtility.WorldToScreenPoint(app.viewCamera,originalCenter);
            var actualGrab=(Vector2)sourceRect.parent.InverseTransformVector(sourceRect.TransformVector(grabOffset));
            var finish=RectTransformUtility.WorldToScreenPoint(app.viewCamera,destination.position+destination.parent.TransformVector(offset+actualGrab));
            if(outside)finish=RectTransformUtility.WorldToScreenPoint(app.viewCamera,sourceRect.parent.TransformPoint(new Vector2(500,-500)));
            string before=JsonUtility.ToJson(Draft()?.working);
            var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pressPosition=start,position=start,eligibleForClick=true};
            ExecuteEvents.Execute(from,p,ExecuteEvents.beginDragHandler);p.position=finish;ExecuteEvents.Execute(from,p,ExecuteEvents.dragHandler);
            var ghost=FindRect("Drag origin");
            Need(ghost!=null&&ghost.gameObject.activeInHierarchy,"drag retains an original-position ghost: "+source);
            Need(Vector3.Distance(ghost.position,originalCenter)<.05f,"original ghost stays at the old node center: "+source);
            Need(JsonUtility.ToJson(Draft()?.working)==before,"drag preview does not mutate the working catalog: "+source);
            Need(!p.eligibleForClick&&app.Page==GamePage.Campaign,"drag never opens a board: "+source);
            if(!outside)
            {
                var expected=finish-screenGrab;
                var actual=RectTransformUtility.WorldToScreenPoint(app.viewCamera,sourceRect.position);
                Need(Vector2.Distance(actual,expected)<2,"dragged tile respects an off-center grab: "+source);
            }
            if(shot!=null){yield return new WaitForSecondsRealtime(.2f);Capture(shot);}
            if(cancel)yield return Press(KeyCode.Escape);
            ExecuteEvents.Execute(from,p,ExecuteEvents.endDragHandler);
            yield return new WaitForSecondsRealtime(.18f);yield return Ready();
            if(cancel||outside)
            {
                Need(JsonUtility.ToJson(Draft()?.working)==before,"cancelled or outside drop leaves content untouched: "+source);
                Need(!FindRect("Drag origin").gameObject.activeSelf&&!FindRect("Drop destination").gameObject.activeSelf,"cancel hides both drag indicators");
            }
        }
        private static void CaptureSize(string name,int width,int height)
        {
            var camera=app.viewCamera;var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.aspect=(float)width/height;app.Board.FitForCapture();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
                texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Evidence,name+".png"),texture.EncodeToPNG());
            }
            finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;app.Board.FitForCapture();rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);}
        }
        private static float Contrast(Color a,Color b)
        {
            float L(Color c){float V(float v)=>v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);return .2126f*V(c.r)+.7152f*V(c.g)+.0722f*V(c.b);}
            float x=L(a),y=L(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);
        }
        private static IEnumerator Usability()
        {
            string profile=Directory.GetParent(app.Library.Folder).FullName;
            Need(profile.StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase),"usability checks are isolated from player storage");
            CreateFixture();Bind(fixture);
            yield return Click("campaign");yield return Chapter("chapter_3");
            Need(app.UI.FindButton("campaign-actions")==null,"chapter map has no redundant level-actions page entry");
            app.UI.FocusButton("select-level-c3_04");yield return Click("level-menu-c3_04");
            Need(app.UI.Root.Find("Level tools")!=null&&app.UI.FindButton("campaign-edit")==null&&app.UI.FindButton("campaign-copy")!=null,"node ellipsis opens compact level tools on the map without duplicate editing actions");
            Capture("node-menu");yield return Click("level-tools-close");
            yield return Options("c3_04");Need(app.UI.Root.Find("Level tools")!=null,"right-click opens the same compact node menu");yield return Click("level-tools-close");

            var layout=new ChapterMapLayout(UsabilityWorking().Chapter("chapter_3"));var nodeCenter=layout.positions["c3_03"];
            var left=layout.DropTarget(nodeCenter+new Vector2(-2,0),"c3_b01");
            var nearRight=layout.DropTarget(nodeCenter+new Vector2(2,0),"c3_b01",left);
            var farRight=layout.DropTarget(nodeCenter+new Vector2(20,0),"c3_b01",left);
            Need(left.HasValue&&nearRight.HasValue&&left.Value.Equals(nearRight.Value)&&farRight.HasValue&&!left.Value.Equals(farRight.Value),"returning a branch to the main row keeps its slot through small pointer jitter");
            var edge=layout.DropTarget(nodeCenter+new Vector2(53.9f,0),"c3_04");
            var beyond=layout.DropTarget(nodeCenter+new Vector2(54.1f,0),"c3_04",edge);
            var released=layout.DropTarget(nodeCenter+new Vector2(74,0),"c3_04",beyond);
            var returning=layout.DropTarget(nodeCenter+new Vector2(60,0),"c3_04",released);
            Need(edge.HasValue&&beyond.HasValue&&edge.Value.Equals(beyond.Value)&&released.HasValue&&!edge.Value.Equals(released.Value)&&returning.HasValue&&released.Value.Equals(returning.Value),"node capture uses different enter and release distances at its edge");

            string originalOrder=JsonUtility.ToJson(Draft()?.working??new CampaignProject(fixture).Source);
            yield return UsabilityDrag("c3_04","c3_03",Vector2.zero,new Vector2(28,20),"drag-origin");
            var main=CampaignDocument.Main(UsabilityWorking().Chapter("chapter_3"));
            Need(main.FindIndex(n=>n.id=="c3_04")+1==main.FindIndex(n=>n.id=="c3_03"),"dropping over a neighboring node reorders at the intended place");
            yield return Click("campaign-undo");Need(JsonUtility.ToJson(Draft()?.working??new CampaignProject(fixture).Source)==originalOrder,"one undo restores the complete reorder");
            yield return UsabilityDrag("c3_04","c3_02",new Vector2(0,64),new Vector2(-24,-18));
            main=CampaignDocument.Main(UsabilityWorking().Chapter("chapter_3"));Need(main.FindIndex(n=>n.id=="c3_04")+1==main.FindIndex(n=>n.id=="c3_02"),"near-main-row release still snaps to the intended node");
            yield return Click("campaign-undo");
            yield return UsabilityDrag("c3_04","c3_02",new Vector2(0,116),new Vector2(22,-16),"drag-branch");
            var branch=UsabilityWorking().Chapter("chapter_3").levels.First(n=>n.id=="c3_04");
            Need(!branch.main&&branch.branchAbove&&branch.requires.SequenceEqual(new[]{"c3_02"}),"release near a branch lane selects its visible parent and side");
            yield return Click("campaign-undo");
            yield return UsabilityDrag("c3_b01","c3_03",new Vector2(-18,0),new Vector2(10,-12));
            main=CampaignDocument.Main(UsabilityWorking().Chapter("chapter_3"));Need(main.FindIndex(n=>n.id=="c3_b01")+1==main.FindIndex(n=>n.id=="c3_03"),"a branch can return to the intended main-path slot");
            yield return Click("campaign-undo");
            yield return UsabilityDrag("c3_04","c3_02",new Vector2(0,-132),new Vector2(16,24),cancel:true);
            yield return UsabilityDrag("c3_04","c3_02",Vector2.zero,new Vector2(16,24),outside:true);
            string before=JsonUtility.ToJson(UsabilityWorking());
            yield return UsabilityDrag("c3_06","c3_03",new Vector2(0,132),Vector2.zero);
            Need(JsonUtility.ToJson(UsabilityWorking())==before,"a main with attached branches cannot be accidentally demoted");

            yield return Click("select-level-c3_04");Need(app.Page==GamePage.Editing,"single-click still opens the board directly");
            var editor=app.Creator;
            Need(app.UI.FindButton("shape-rectangle").transform.Find("Shortcut").GetComponent<TMP_Text>().text=="R","rectangle displays its actual R shortcut");
            Need(app.UI.FindButton("brush-3").transform.Find("Label").GetComponent<TMP_Text>().text=="玩家"&&app.UI.FindButton("brush-6").transform.Find("Label").GetComponent<TMP_Text>().text=="缺口","palette uses player and gap terminology");
            editor.Select(Brush.Box);Need(editor.HandleShortcut(KeyCode.R)&&editor.Shape==PaintShape.Rectangle,"R dispatches rectangle mode");
            Need(!editor.HandleShortcut(KeyCode.E)&&editor.Operation==EditOperation.Paint,"erasing uses the right button without changing the material");
            Need(editor.HandleShortcut(KeyCode.Q)&&editor.Operation==EditOperation.Paint&&editor.Selected==Brush.Box,"Q restores painting with the chosen material");
            Need(editor.HandleShortcut(KeyCode.P)&&editor.Shape==PaintShape.Brush,"P returns to point painting");
            Need(editor.HandleShortcut(KeyCode.LeftBracket)&&editor.Selected==Brush.MirrorSlash&&editor.HandleShortcut(KeyCode.RightBracket)&&editor.Selected==Brush.MirrorBackslash,"brackets select the mirror brushes without colliding with editing modes");
            var title=app.UI.Root.Find("Level title").GetComponent<TMP_InputField>();title.ActivateInputField();EventSystem.current.SetSelectedGameObject(title.gameObject);yield return null;yield return null;
            var shape=editor.Shape;Need(app.UI.HasTextFocus&&!editor.HandleShortcut(KeyCode.R)&&editor.Shape==shape,"typing does not trigger tool shortcuts");app.UI.CommitText();
            yield return Click("editor-files");Need(app.UI.FindButton("editor-help")==null&&app.UI.FindButton("export-editor")!=null,"files popup retains export and removes shortcut help");yield return Click("editor-files");
            yield return Click("leave-editor");

            var pit=LevelData.Blank(11,7);pit.title="缺口预览";pit.player=new Cell(2,3);pit.coffin=new Cell(8,3);
            var document=new EditDocument(pit,false);document.Begin();foreach(var cell in new[]{new Cell(4,1),new Cell(5,1),new Cell(6,1),new Cell(4,2),new Cell(5,2),new Cell(6,2),new Cell(6,3)})document.Paint(cell,Brush.Void);document.End();pit=document.Data.Copy();
            foreach(var theme in GameThemes.All)
            {
                GameThemes.Apply(theme.Id);app.ShowTitle();yield return Ready();
                var entry=app.UI.FindButton("campaign");var rect=(RectTransform)entry.transform;
                Need(rect.anchorMin==Vector2.right&&rect.anchoredPosition==new Vector2(-28,22)&&rect.rect.width>=232,"editor entry keeps its bottom-right position with a larger target: "+theme.Id);
                var face=entry.transform.Find("Surface").GetComponent<UnityEngine.UI.Image>().color;var ink=entry.transform.Find("Label").GetComponent<TMP_Text>().color;
                Need(Contrast(face,ink)>=4.5f,"editor entry has legible themed contrast: "+theme.Id);Capture("title-"+theme.Id);
                if(theme.Id=="wine")CaptureSize("title-wine-4x3",1024,768);
                app.Edit(pit.Copy());yield return Ready();
                var pieces=app.Board.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.gameObject.name.StartsWith("Pit ",StringComparison.Ordinal)).ToArray();
                Need(pieces.Length==7&&pieces.All(r=>Mathf.Abs(r.transform.localScale.x-1)<.001f),"connected pit cells cover their full grid area: "+theme.Id);
                Need(pieces.Select(r=>r.sprite.name).Distinct().Count()>1,"pit perimeter responds to neighboring gaps: "+theme.Id);
                Capture("editor-"+theme.Id);
                if(theme.Id=="wine"||theme.Id=="porcelain")CaptureSize("editor-"+theme.Id+"-4x3",1024,768);
            }
            GameThemes.Apply(GameThemes.DefaultThemeId);app.ShowTitle();yield return Ready();
        }
    }
}
