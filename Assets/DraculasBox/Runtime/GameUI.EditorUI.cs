using System;
using System.Linq;
using DraculasBox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private TMP_Text importStatus;private TMP_InputField importPath;private RectTransform importPreview,importDialog;
        public bool HasTextFocus=>EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>()?.isFocused==true;
        public void CommitText(){if(!HasTextFocus)return;EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>().DeactivateInputField();EventSystem.current.SetSelectedGameObject(null);}
        public void SetLabel(string id,string value){var b=FindButton(id);if(b!=null){var t=b.transform.Find("Label")?.GetComponent<TMP_Text>();if(t!=null)t.text=value;}}
        public void SetEnabled(string id,bool value)
        {
            var b=FindButton(id);if(b==null)return;b.interactable=value;var primary=b.transform.Find("Primary face");if(primary!=null)primary.GetComponent<Image>().color=value?WorkshopPrimary:(light?(GameThemes.IsClassic?new Color32(221,217,224,255):Color.Lerp(WorkshopSurface,WorkshopLine,.5f)):GameThemes.Current.Border);var t=b.GetComponentInChildren<TMP_Text>();if(t!=null)t.color=value?(primary!=null?WorkshopPrimaryInk:light?WorkshopInk:White):light?(GameThemes.IsClassic?new Color32(123,118,130,255):Color.Lerp(WorkshopInk,WorkshopSurface,.5f)):new Color(Quiet.r,Quiet.g,Quiet.b,.38f);var icon=b.transform.Find("Icon")?.GetComponent<Graphic>();if(icon!=null){var c=icon.color;c.a=value?1:.32f;icon.color=c;}var key=b.transform.Find("Shortcut")?.GetComponent<TMP_Text>();if(key!=null){var c=light?Muted:Quiet;c.a=value?1:.38f;key.color=c;}
        }
        public void Panel(string id,Vector2 center,Vector2 size){if(light){WorkshopPanel(id,center,size);return;}var p=Image(id,null,center,size,GameThemes.Current.Panel);p.raycastTarget=true;}
        public Button BrushButton(int index,string label,string sprite,Vector2 position,Action clicked,Transform parent=null)
        {
            var r=Rect("brush-"+index,position,new Vector2(138,46),parent);var button=ClickTarget(r);Rounded(button.GetComponent<Image>());button.GetComponent<Image>().color=Color.clear;
            bool device=index==8||index==9;var icon=Image("Icon",device?null:sprite,new Vector2(-48,0),new Vector2(28,28),sprite=="solid"?SafeColor(index):Color.white,r);if(index==1){icon.sprite=SurfaceSprites.StoneWall(0);icon.color=Color.white;}if(index==6){icon.sprite=SurfaceSprites.Pit(0);icon.color=Color.white;}if(device){icon.sprite=SurfaceSprites.DeviceIcon(index==9);icon.color=BoardView.CircuitColors[0];}
            Label(Rect("Label",new Vector2(5,0),new Vector2(68,40),r),label,19,WorkshopInk,TextAlignmentOptions.MidlineLeft);
            string key=index<7?(index+1).ToString():index==8?"9":index==9?"0":index==10?"Q":"E";
            ShortcutCap(r,key,new Vector2(55,0),new Vector2(19,24));
            var frame=Image("Focus",null,Vector2.zero,new Vector2(138,46),Color.clear,r);Rounded(frame,true);frame.raycastTarget=false;Register("brush-"+index,button,clicked,null,null,false,true,false);return button;
        }
        private void ShortcutCap(Transform parent,string key,Vector2 position,Vector2 size)
        {
            var edge=Image("Shortcut frame",null,position,size,new Color(WorkshopLine.r,WorkshopLine.g,WorkshopLine.b,.55f),parent);Rounded(edge,true);edge.raycastTarget=false;
            var label=Label(Rect("Shortcut",position,size,parent),key,13,light?Muted:Quiet);label.raycastTarget=false;
        }
        public void EditorShortcut(string id,string key)
        {
            var button=FindButton(id);if(button==null)return;var r=(RectTransform)button.transform;
            float width=key.Length>2?50:22;float right=r.rect.width*.5f-11-width*.5f;
            var title=r.Find("Label")?.GetComponent<TMP_Text>();
            if(title!=null){title.rectTransform.anchoredPosition=new Vector2(-(width+8)*.5f,0);title.rectTransform.sizeDelta=new Vector2(r.rect.width-width-22,r.rect.height);title.fontSize=Mathf.Min(title.fontSize,20);}
            var icon=r.Find("Icon") as RectTransform;if(icon!=null)icon.anchoredPosition=new Vector2(-12,0);
            ShortcutCap(r,key,new Vector2(right,0),new Vector2(width,24));
        }
        private Color SafeColor(int tool)=>tool==1?GameThemes.Current.WallTop:tool==6?Night:BoardView.SafeColor;
        public void SelectBrush(Button button,bool selected,bool dim=false)
        {var frame=button.transform.Find("Focus").GetComponent<Graphic>();frame.color=new Color(WorkshopAccent.r,WorkshopAccent.g,WorkshopAccent.b,selected?(dim?.35f:1):0);button.GetComponent<Image>().color=selected?GameThemes.Current.WorkshopSelected:Color.clear;var icon=button.transform.Find("Icon").GetComponent<Graphic>();var color=icon.color;color.a=dim?.42f:1;icon.color=color;button.GetComponentInChildren<TMP_Text>().color=dim?Muted:WorkshopInk;var key=button.transform.Find("Shortcut")?.GetComponent<TMP_Text>();if(key!=null)key.color=dim?Color.Lerp(Muted,WorkshopSurface,.45f):Muted;}
        public Button EditorButton(string id,string label,Vector2 position,Vector2 size,Action action,bool primary=false,Transform parent=null,bool navigable=false)
        {
            var button=Button(id,label,position,size,action,true,navigable);if(parent!=null)button.transform.SetParent(parent,false);var outer=button.GetComponent<Image>();var face=button.transform.Find("Face");if(light){Rounded(outer);outer.color=WorkshopLine;Rounded(face.GetComponent<Image>());face.GetComponent<Image>().color=WorkshopSurface;button.transform.Find("Label").GetComponent<TMP_Text>().color=WorkshopInk;}if(primary){outer.color=WorkshopPrimary;face.name="Primary face";face.GetComponent<Image>().color=WorkshopPrimary;button.transform.Find("Label").GetComponent<TMP_Text>().color=WorkshopPrimaryInk;}return button;
        }
        public void Rule(){Image("Rule",null,new Vector2(490,-76),new Vector2(204,1),new Color(Quiet.r,Quiet.g,Quiet.b,.18f));}
        public void EditorHelp(Action back)
        {
            Dim("编辑操作");Text("Circuit help","9 / 0 压力板、自动门 · Q / E 斜镜",new Vector2(0,175),new Vector2(1100,38),21,Quiet);Text("Painting help","左键绘制 · 右键擦除 · Alt + 左键取色\n矩形：拖出范围后松开 · Shift 临时框选 · Esc 取消",new Vector2(0,104),new Vector2(1100,85),24,White);
            Text("Viewport help","滚轮缩放 · 中键拖动画布",new Vector2(0,13),new Vector2(900,48),24,White);
            Text("History help","Z / Y 撤销、重做 · Ctrl + S 保存\nF5 试玩 / 返回原稿",new Vector2(0,-79),new Vector2(1000,85),24,White);
            MenuAction("close-help","返回编辑",new Vector2(0,-204),240,back,GameIcon.Back);Focus(0);
        }
        public void OperationError(string heading,string message,Action back)
        {Dim(heading);Text("Operation error",message,new Vector2(0,34),new Vector2(920,180),23,Lilac);MenuAction("error-back","返回",new Vector2(0,-164),210,back,GameIcon.Back);Focus(0);}
        public LevelThumbnail Thumbnail(string name,LevelData data,Vector2 center,Vector2 size,Transform parent=null)
        {var thumbnail=Rect(name,center,size,parent).gameObject.AddComponent<LevelThumbnail>();thumbnail.color=Color.white;thumbnail.raycastTarget=false;thumbnail.Configure(assets);thumbnail.Set(data);return thumbnail;}
        private void DialogClose(string id,Vector2 position,Transform parent,Action close)
        {
            var button=EditorButton(id,"",position,new Vector2(32,32),close,false,parent,true);
            foreach(float angle in new[]{-45f,45f})
            {var stroke=Image("Close stroke",null,Vector2.zero,new Vector2(14,1.6f),WorkshopInk,button.transform);stroke.rectTransform.localRotation=Quaternion.Euler(0,0,angle);}
        }
        public void ImportPage(string path)
        {
            CloseWorkshopPopup();
            importDialog=WorkshopOverlay("Import dialog",Vector2.zero,new Vector2(780,250),app.CloseImport);
            var header=Rect("Import header",new Vector2(0,-72),new Vector2(780,144),importDialog);header.anchorMin=header.anchorMax=new Vector2(.5f,1);
            Label(Rect("Import heading",new Vector2(-40,38),new Vector2(636,40),header),"导入关卡",27,WorkshopInk,TextAlignmentOptions.MidlineLeft);
            DialogClose("cancel-import",new Vector2(347,38),header,app.CloseImport);
            importPath=Input("Import path",path??"",new Vector2(-66,-20),new Vector2(582,44),app.SetImportPath,2048);importPath.transform.SetParent(header,false);
            importPath.onValueChanged.AddListener(app.SetImportPath);
            importPath.onSubmit.AddListener(_=>app.PreviewImport());
            var placeholder=Label(Rect("Placeholder",Vector2.zero,new Vector2(562,38),importPath.textViewport),"选择或粘贴 JSON 文件路径",19,Muted,TextAlignmentOptions.MidlineLeft);importPath.placeholder=placeholder;
            EditorButton("browse-import","选择文件",new Vector2(297,-20),new Vector2(120,44),app.BrowseImport,false,header,true);
            importPreview=Rect("Import preview",new Vector2(0,-2),new Vector2(710,218),importDialog);importPreview.gameObject.SetActive(false);
            var footer=Rect("Import actions",new Vector2(0,34),new Vector2(720,60),importDialog);footer.anchorMin=footer.anchorMax=new Vector2(.5f,0);
            importStatus=Label(Rect("Import status",new Vector2(0,46),new Vector2(712,48),footer),"",18,Muted,TextAlignmentOptions.MidlineLeft);
            EditorButton("preview-import","预览",new Vector2(-299,0),new Vector2(116,42),app.PreviewImport,false,footer,true);
            EditorButton("confirm-import","导入并编辑",new Vector2(276,0),new Vector2(162,42),app.ConfirmImport,true,footer,true);
            SetEnabled("confirm-import",false);FocusButton("browse-import");
        }
        public void SetImportPathText(string value){if(importPath!=null)importPath.SetTextWithoutNotify(value);}
        public string ImportPathText=>importPath==null?"":importPath.text;
        public void ImportResult(LevelData data,string message)
        {
            if(importPreview==null)return;
            for(int i=importPreview.childCount-1;i>=0;i--){var child=importPreview.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            var size=new Vector2(780,data==null?250:474);importDialog.sizeDelta=size;importPreview.gameObject.SetActive(data!=null);
            foreach(string surface in new[]{"Overlay surface","Overlay border","Overlay shadow"})
                ((RectTransform)importDialog.Find(surface)).sizeDelta=size+(surface=="Overlay shadow"?Vector2.one*4:Vector2.zero);
            if(data!=null){WorkshopPanel("Preview surface",Vector2.zero,new Vector2(710,218),importPreview).color=BoardView.SafeColor;Thumbnail("Incoming level",data,Vector2.zero,new Vector2(688,206),importPreview);}
            importStatus.text=message;importStatus.color=data==null?WorkshopAccent:Muted;SetEnabled("confirm-import",data!=null);
        }
        public void ConfirmRemove(LevelData data,Action remove,Action cancel)
        {
            Dim("移除关卡？");Thumbnail("Remove preview",data,new Vector2(0,65),new Vector2(270,160));Text("Remove title",data.title,new Vector2(0,-47),new Vector2(700,42),26,White);Text("Remove note","移入本地回收区，可恢复。",new Vector2(0,-95),new Vector2(800,35),18,Quiet);
            MenuAction("confirm-remove","移除",new Vector2(-135,-178),190,remove,GameIcon.Exit);MenuAction("cancel-remove","取消",new Vector2(135,-178),190,cancel,GameIcon.Back);Focus(1);
        }
        public void ExportResult(string path,string title,Action back)
        {
            CloseWorkshopPopup();
            var root=WorkshopOverlay("Export dialog",Vector2.zero,new Vector2(740,248),back);
            Icon("Export check",GameIcon.Check,new Vector2(-321,83),24,WorkshopAccent,root);
            Label(Rect("Export heading",new Vector2(-27,83),new Vector2(544,40),root),"已导出",27,WorkshopInk,TextAlignmentOptions.MidlineLeft);
            DialogClose("export-close",new Vector2(337,83),root,back);
            Label(Rect("Export file",new Vector2(0,30),new Vector2(674,32),root),title,21,WorkshopInk,TextAlignmentOptions.MidlineLeft);
            var field=Input("Export path",path,new Vector2(0,-7),new Vector2(674,38),_=>{},2048);field.transform.SetParent(root,false);field.readOnly=true;field.textComponent.fontSize=16;
            EditorButton("export-folder","打开文件夹",new Vector2(-262,-81),new Vector2(150,42),()=>Application.OpenURL(new Uri(System.IO.Path.GetDirectoryName(path)).AbsoluteUri),false,root,true);
            EditorButton("export-copy","复制路径",new Vector2(-108,-81),new Vector2(130,42),()=>{GUIUtility.systemCopyBuffer=path;SetLabel("export-copy","已复制");},false,root,true);
            EditorButton("export-back","完成",new Vector2(278,-81),new Vector2(118,42),back,true,root,true);FocusButton("export-back");
        }
    }
}
