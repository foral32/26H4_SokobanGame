using System;
using System.Collections;
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
        public static void RunChapters()=>Begin(ChapterTools(),"chapter-tools",true);
        private static IEnumerator ChapterPopup(string id)
        {yield return Click("chapter-menu-"+id);yield return new WaitForSecondsRealtime(.18f);}
        private static IEnumerator OutsidePopup()
        {
            yield return Ready();
            var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,app.UI.Root.TransformPoint(new Vector2(-600,220)))};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
            var target=hits.Count==0?null:ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Need(target!=null&&target==app.UI.FindButton("campaign-overlay-outside").gameObject,"outside clicks target only the modal backdrop");
            ExecuteEvents.Execute(target,p,ExecuteEvents.pointerClickHandler);yield return Ready();
        }
        private static IEnumerator ChapterTools()
        {
            CreateFixture();Bind(fixture);yield return Click("campaign");
            var source=new CampaignProject(fixture).Source;string baseline=JsonUtility.ToJson(source);
            var staleRoute=JsonUtility.FromJson<CampaignContent>(baseline);
            var sunnyBoard=staleRoute.Board("c1_02");var sunnyPlan=staleRoute.Plan("c1_02");
            Need(CampaignDocument.Proves(sunnyBoard,sunnyPlan.route,out int verifiedPushes)&&verifiedPushes==4,
                "the unchanged sunny route remains a valid four-push proof");
            sunnyPlan.pushes=3;
            Need(!CampaignProject.PublicationIssues(staleRoute).Any(issue=>issue.id=="c1_02"),
                "a valid sunny route is not rejected when only its stored push count is stale");
            var readyToPublish=CampaignProject.PrepareForPublication(staleRoute);
            Need(readyToPublish.Plan("c1_02").pushes==4&&sunnyPlan.pushes==3,
                "publication recalculates stale push metadata on a copy and leaves the recoverable draft untouched");
            var chapters=source.catalog.chapters;string last=chapters.Last().id;
            Need(chapters.All(c=>app.UI.FindButton("chapter-menu-"+c.id)!=null),"each chapter card exposes its own visible more menu");
            yield return ChapterPopup(last);
            Need(app.UI.Root.Find("Heading").GetComponent<TMP_Text>().text=="编辑正式关卡"&&
                !app.UI.FindButton("campaign-new-chapter").interactable,"chapter tools stay anchored over the gallery and block background actions");
            Need(!app.UI.FindButton("chapter-later").interactable,"the last chapter cannot move beyond the end");
            var field=app.UI.Root.GetComponentInChildren<TMP_InputField>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(app.viewCamera,field.transform.position)};
            ExecuteEvents.Execute(field.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
            Need(field.isFocused&&field.textViewport.Find("Caret")!=null,"the compact chapter name field activates a real desktop caret");
            field.text="第四章 · 月庭";yield return Click("chapter-earlier");
            var changed=Draft().working.catalog.chapters;
            Need(changed[2].id==last&&changed[2].title=="第四章 · 月庭"&&changed[3].requiresChapter==last,"renaming and moving together preserve IDs and rewire chapter unlocks");
            yield return Click("chapter-apply");yield return Press(KeyCode.LeftControl,KeyCode.Z);
            Need(JsonUtility.ToJson(Draft()?.working??source)==baseline,"one undo restores both chapter order and the pending rename");
            yield return Press(KeyCode.LeftControl,KeyCode.Y);
            Need(Draft().working.catalog.chapters[2].id==last,"gallery keyboard redo restores the chapter move");
            yield return ChapterPopup(last);yield return Click("chapter-later");
            field=app.UI.Root.GetComponentInChildren<TMP_InputField>();field.text="第四章 · 月庭";
            yield return new WaitForSecondsRealtime(.18f);Capture("chapter-tools");yield return OutsidePopup();
            Need(Draft().working.Chapter(last).title=="第四章 · 月庭","closing the chapter popover keeps the edited name");

            yield return Click("campaign-new-chapter");string added=Draft().chapter,seed=Draft().selection;
            Need(app.UI.Root.Find("Chapter tools")!=null&&app.UI.FindButton("select-chapter-"+added)!=null,"creating a chapter opens its compact tools on the correct gallery page");
            field=app.UI.Root.GetComponentInChildren<TMP_InputField>();field.text="  ";yield return Click("chapter-apply");
            Need(app.UI.Root.Find("Chapter tools")!=null&&Draft().working.Chapter(added).title=="新章节","an empty name leaves the popover editable without corrupting the draft");
            field=app.UI.Root.GetComponentInChildren<TMP_InputField>();field.text="庭院";yield return Click("chapter-apply");
            Need(app.UI.Root.Find("Chapter page").GetComponent<TMP_Text>().text=="2 / 2","adding a fifth chapter visibly selects the second gallery page");
            yield return Click("campaign-new-chapter");string second=Draft().chapter,secondSeed=Draft().selection;
            field=app.UI.Root.GetComponentInChildren<TMP_InputField>();field.text="误建章节";yield return Click("chapter-apply");
            yield return Click("campaign-review");
            Need(app.UI.FindButton("campaign-fix-"+seed)!=null&&app.UI.FindButton("campaign-fix-"+secondSeed)!=null&&
                app.UI.FindButton("campaign-publish").interactable&&
                app.UI.Root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("尚未记录关卡解")),
                "publication explains missing route proofs and offers direct editing");
            var review=(RectTransform)app.UI.Root.Find("Publication review");
            Need(review.rect.width<=900&&review.rect.height<=560&&app.UI.Root.Find("Heading")!=null,"publication is a compact modal that keeps the chapter context visible");
            yield return new WaitForSecondsRealtime(.18f);Capture("chapter-pending");
            yield return Click("campaign-fix-"+seed);yield return Page(GamePage.Editing);
            Need(app.Creator.Document.Data.id==seed,"the pending level action opens the precise board to repair or solve");
            yield return Click("brush-4");var cell=app.Board.Screen(new Cell(3,3));
            app.Creator.PointerSample(cell,true,true,false);app.Creator.PointerSample(cell,false,false,true);yield return null;
            yield return Click("test");yield return Page(GamePage.Playing);app.Act(Command.Right);yield return Page(GamePage.Editing);
            yield return Click("save");yield return Click("leave-editor");
            Need(app.UI.Root.Find("Publication review")!=null&&app.UI.FindButton("campaign-fix-"+seed)==null&&
                app.UI.FindButton("campaign-fix-"+secondSeed)!=null,"returning from a verified trial reopens the updated publication review");
            yield return Click("campaign-popup-close");yield return Click("campaign-back");
            yield return ChapterPopup(second);yield return Click("chapter-delete");
            Need(app.UI.Root.Find("Delete chapter")!=null&&app.UI.SelectedId=="chapter-delete-cancel","deletion names the chapter in a small confirmation with Cancel selected");
            yield return new WaitForSecondsRealtime(.18f);Capture("chapter-delete");string before=JsonUtility.ToJson(Draft().working);
            yield return Click("chapter-delete-cancel");
            Need(JsonUtility.ToJson(Draft().working)==before,"cancelling chapter deletion changes nothing");
            yield return Click("chapter-delete");yield return Click("chapter-delete-confirm");
            Need(!Draft().working.catalog.chapters.Any(c=>c.id==second)&&!Draft().working.boards.Any(b=>b.id==secondSeed),"deleting an accidental chapter removes its seed level from the working catalog");
            Need(CampaignProject.PublicationIssues(Draft().working).Count==0,"removing an unfinished accidental chapter clears its publication blocker");
            yield return Click("campaign-undo");
            Need(JsonUtility.ToJson(Draft().working)==before&&app.UI.FindButton("select-chapter-"+second)!=null,"undo restores the entire deleted chapter and follows it to the right gallery page");
            yield return Click("campaign-redo");
            Need(!Draft().working.catalog.chapters.Any(c=>c.id==second),"redo deletes the same chapter without duplicating content");
            yield return ChapterPopup(added);yield return Click("chapter-delete");yield return Click("chapter-delete-confirm");
            Need(Draft().working.catalog.chapters.Length==4&&app.UI.FindButton("select-chapter-chapter_1")!=null,"deleting the last card on page two returns to the remaining chapters");

            string middle=chapters[1].id,next=chapters[2].id;var removed=source.Chapter(middle).levels.Select(l=>l.id).ToArray();
            yield return ChapterPopup(middle);yield return Click("chapter-delete");
            string beforeFailure=JsonUtility.ToJson(Draft().working);
            string cache=System.IO.Path.Combine("Library","DraculasBoxCampaign",UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(fixture.catalog)),"draft.json");
            using(var locked=new System.IO.FileStream(cache,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.None))
                yield return Click("chapter-delete-confirm");
            Need(app.Page==GamePage.Campaign&&app.UI.Root.Find("Chapter tools")!=null&&JsonUtility.ToJson(Draft().working)==beforeFailure,
                "a locked draft refuses deletion and preserves the chapter, selection and usable popup");
            yield return Click("chapter-delete");yield return Click("chapter-delete-confirm");
            var data=Draft().working;
            Need(data.Chapter(next).requiresChapter==chapters[0].id&&data.boards.All(b=>!removed.Contains(b.id))&&
                data.replays.levels.All(p=>!removed.Contains(p.id)),"deleting a chapter with branches reconnects later chapters and removes all its boards and routes");
            CampaignDocument.ValidateGraph(data);
            yield return Click("campaign-undo");
            Need(removed.All(id=>Draft().working.boards.Any(b=>b.id==id)&&Draft().working.Plan(id)!=null),"undo restores every main, branch and verified route of a published chapter");
            yield return Click("campaign-redo");yield return Click("campaign-review");
            Need(app.UI.Root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.StartsWith("删除章节"))&&
                app.UI.FindButton("campaign-publish").interactable,"review summarizes chapter removal and allows valid metadata-only publication");
            yield return Click("campaign-reload");yield return Click("campaign-cancel");
            Need(Draft().working.catalog.chapters.Length==3&&app.UI.Root.Find("Publication review")!=null,"cancelling source reload preserves the working chapter changes");
            Need(new CampaignProject(fixture).Revision==Get("start"),"all modal interactions and deletes leave source assets untouched before confirmation");
            yield return new WaitForSecondsRealtime(.18f);Capture("chapter-publication");
            yield return Click("campaign-publish");
            var published=new CampaignProject(fixture);
            Need(!published.HasDraft&&published.Source.catalog.chapters.Length==3&&published.Source.Chapter(next).requiresChapter==chapters[0].id,
                "confirmed publication persists chapter deletion and the corrected unlock chain");
            Need(removed.All(id=>System.IO.File.Exists(Folder+"/Levels/"+id+".json"))&&fixture.levels.All(a=>!removed.Contains(a.name)),
                "published deletion keeps recoverable source boards and removes their player build references");
            var model=new CampaignDocument(JsonUtility.FromJson<CampaignContent>(baseline));
            while(model.Data.catalog.chapters.Length>1)model.Change(()=>model.RemoveChapter(model.Data.catalog.chapters.Last().id));
            string single=JsonUtility.ToJson(model.Data);bool refused=false;
            try{model.Change(()=>model.RemoveChapter(model.Data.catalog.chapters[0].id));}catch(InvalidOperationException){refused=true;}
            Need(refused&&JsonUtility.ToJson(model.Data)==single,"attempting to delete the final chapter is refused atomically");
            CampaignDocument.ValidateGraph(model.Data);
        }
    }
}
