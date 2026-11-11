using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunEditing()=>Begin(EditingChanges(),"editing-changes",true);
        public static void RunPopupPolish()=>Begin(PopupPolish(),"popup-polish",true);
        private static IEnumerator PopupPolish()
        {
            CreateFixture();Bind(fixture);var project=new CampaignProject(fixture);
            var working=CampaignPublication.Copy(project.Source);
            working.catalog.chapters[0].title="第一章 · 弹窗检查";working.boards[0].title="勾选检查";
            project.SaveDraft(new CampaignDraftFile{revision=project.Revision,baseline=project.Source,working=working,chapter=working.catalog.chapters[0].id,selection=working.boards[0].id});
            yield return Click("campaign");yield return Chapter(working.catalog.chapters[0].id);yield return Click("campaign-review");
            yield return new WaitForSecondsRealtime(.25f);
            var root=app.UI.Root.Find("Publication review");var heading=app.UI.Root.Find("Heading");
            var sheet=(RectTransform)root.Find("Review viewport/Review items");var offset=sheet.anchoredPosition;
            var review=new CampaignPublication(project.Source,working);var first=review.Items[0];
            var control=app.UI.FindButton("publish-toggle-"+first.key);
            yield return Click("publish-toggle-"+first.key);
            Need(root==app.UI.Root.Find("Publication review")&&heading==app.UI.Root.Find("Heading")&&control==app.UI.FindButton("publish-toggle-"+first.key),"individual toggle preserves popup, map and button instances");
            Need(sheet.anchoredPosition==offset&&root.GetComponent<CanvasGroup>().alpha>.99f,"toggle preserves scroll and does not restart popup fade");
            Need(control.transform.Find("Label").GetComponent<TMP_Text>().text==""&&TextContains("写回所选（"+(review.Items.Count-1)+"）"),"unchecked mark and selected count update in place");
            yield return Click("publish-select-all");
            Need(root==app.UI.Root.Find("Publication review")&&TextContains("写回所选（"+review.Items.Count+"）"),"select all updates without rebuilding");
            yield return Click("publish-select-all");Need(root==app.UI.Root.Find("Publication review")&&TextContains("写回所选（0）"),"clear all updates without rebuilding");
            yield return Click("campaign-popup-close");yield return Click("campaign-chapter-settings");yield return new WaitForSecondsRealtime(.25f);
            var chapter=(RectTransform)app.UI.Root.Find("Chapter tools");var input=chapter.GetComponentInChildren<TMP_InputField>();var close=(RectTransform)app.UI.FindButton("campaign-popup-close").transform;
            Need(chapter.rect.height==202&&Mathf.Abs(((RectTransform)input.transform).anchoredPosition.y-close.anchoredPosition.y)<.1f&&chapter.Find("Chapter tools heading")==null,"chapter name and close button share one compact header row");
            Capture("chapter-compact-header");yield return Click("campaign-popup-close");app.ShowTitle();yield return Page(GamePage.Title);
        }
        private static bool TextContains(string text)=>app.UI.Root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains(text));
        private static IEnumerator ToggleReview(string key)
        {
            var control=app.UI.FindButton("publish-toggle-"+key);
            Need(control!=null,"review exposes individual change: "+key);
            var rect=(RectTransform)control.transform;var scroll=control.GetComponentInParent<ScrollRect>();
            scroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(-rect.anchoredPosition.y-scroll.viewport.rect.height*.5f,0,Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height)));
            yield return Ready();yield return Click("publish-toggle-"+key);
        }
        private static LevelData SelectionFixture()
        {
            var b=LevelData.Blank(11,7);b.title="选区移动";b.player=new Cell(2,4);b.coffin=new Cell(8,4);
            b.pieces=new[]{new EntityData{id="box-a",cell=new Cell(4,3)},new EntityData{id="lamp-a",kind=PieceKind.Lamp,cell=new Cell(5,3)}};
            b.plates=new[]{new DeviceData{cell=new Cell(4,3),channel=2}};
            b.doors=new[]{new DeviceData{cell=new Cell(7,2),channel=2}};return b;
        }
        private static void SelectionModelChecks()
        {
            var b=SelectionFixture();var doc=new EditDocument(b);var pick=doc.PickSelection(new Cell(4,3));
            Need(pick.Layer==SelectionLayer.Piece,"single click prioritizes the piece above its pressure plate");
            Need(doc.PreviewMove(pick,new Cell(0,-1),out var preview)==null&&doc.Data.ContentEquals(b)&&doc.UndoCount==0,"selection preview never edits the document");
            Need(preview.pieces[0].cell==new Cell(4,2)&&preview.plates[0].cell==new Cell(4,3),"object movement leaves underlying terrain and device in place");
            Need(doc.MoveSelection(pick,new Cell(0,-1),out _)&&doc.UndoCount==1,"one object move is one undo transaction");
            doc.Undo();Need(doc.Data.ContentEquals(b),"undo restores the complete object move");
            var region=new EditSelection(new Cell(3,2),new Cell(5,3));
            Need(doc.MoveSelection(region,new Cell(1,0),out _)&&doc.Data.pieces[0].cell==new Cell(5,3)&&doc.Data.plates[0].cell==new Cell(5,3)&&doc.Data.plates[0].channel==2,"overlapping region translation moves objects and circuits with identities intact");
            doc.Undo();string snapshot=JsonUtility.ToJson(doc.Data);int history=doc.UndoCount;
            Need(!doc.MoveSelection(pick,new Cell(1,0),out var blocked)&&blocked!=null&&JsonUtility.ToJson(doc.Data)==snapshot&&doc.UndoCount==history,"collision refuses movement without destructive overwrite or undo pollution");
            Need(!doc.MoveSelection(region,new Cell(-10,0),out _)&&doc.PreviewMove(new EditSelection(new Cell(-1,0),new Cell(1,1)),new Cell(2,1),out _)!=null,"out-of-bounds source and destination fail safely");
            doc.DeleteSelection(region);Need(doc.Data.pieces.Length==0&&doc.Data.plates.Length==0&&doc.Data.doors.Length==1,"region deletion affects all selected layers only");
            doc.Undo();Need(doc.Data.ContentEquals(b),"one undo restores deleted objects and circuit channels");
        }
        private static IEnumerator EditingChanges()
        {
            CreateFixture();Bind(fixture);SelectionModelChecks();
            var project=new CampaignProject(fixture);var baseline=CampaignPublication.Copy(project.Source);
            var stale=CampaignPublication.Copy(baseline);var proof=stale.Plan("c1_02");proof.pushes=-1;
            var normalized=CampaignProject.PrepareForPublication(stale);
            Need(LevelSolver.Proves(normalized.Board("c1_02"),normalized.Plan("c1_02").route,out int correctPushes)&&normalized.Plan("c1_02").pushes==correctPushes&&proof.pushes==-1,"valid routes repair derived push counts on a copy");
            var change=new CampaignDocument(CampaignPublication.Copy(baseline));
            change.Data.Board("c1_01").title="箱子检查";
            change.Data.Board("c1_02").title="阳光检查";change.Data.Board("c1_02").coffin=new Cell(-1,-1);
            var chapter=change.Data.Chapter("chapter_1");var good=change.AddLevel(chapter,chapter.levels.First(l=>l.id=="c1_01"),true);
            var bad=change.AddLevel(chapter,chapter.levels.First(l=>l.id==good),false);
            var review=new CampaignPublication(baseline,change.Data);
            Need(review.Items.All(i=>i.selected)&&review.Check(out _,out var issues)!=null&&issues.Any(i=>i.kind==CampaignProject.IssueKind.Board)&&issues.Any(i=>i.kind==CampaignProject.IssueKind.MissingRoute),"all changes start selected with distinct board and missing-route reasons");
            foreach(var item in review.Items)if(item.key=="Board:c1_02"||item.key=="LevelAdd:"+bad)item.selected=false;
            Need(review.Check(out var partial,out _)==null&&partial.Board("c1_02").LayoutEquals(baseline.Board("c1_02"))&&partial.Board("c1_02").title=="阳光检查"&&partial.boards.Any(b=>b.id==good)&&!partial.boards.Any(b=>b.id==bad),"independent names and a valid addition publish despite an unfinished board and another new level in the same chapter");
            var invalid=CampaignPublication.Copy(baseline);invalid.Plan("c1_01").route="U";
            Need(CampaignProject.PublicationIssues(invalid).Any(i=>i.id=="c1_01"&&i.kind==CampaignProject.IssueKind.InvalidRoute),"stale route has its own concrete validation reason");
            var renameOnly=new CampaignPublication(baseline,change.Data);foreach(var item in renameOnly.Items)item.selected=item.key=="Title:c1_01";
            Need(renameOnly.Check(out var justName,out _)==null&&JsonUtility.ToJson(justName.replays)==JsonUtility.ToJson(baseline.replays),"rename-only publication preserves replay contents and ordering");
            var moveDoc=new CampaignDocument(CampaignPublication.Copy(baseline));var from=moveDoc.Data.Chapter("chapter_1");var to=moveDoc.Data.Chapter("chapter_2");
            moveDoc.Transfer(from,from.levels.First(l=>l.id=="c1_01"),to);
            var movedReview=new CampaignPublication(baseline,moveDoc.Data);
            Need(movedReview.Check(out var moved,out _)==null&&moved.catalog.chapters.SelectMany(c=>c.levels).Count(l=>l.id=="c1_01")==1&&moved.Chapter("chapter_2").levels.Any(l=>l.id=="c1_01"),"cross-chapter placement is atomic and keeps one stable identity");
            var newChapterDoc=new CampaignDocument(CampaignPublication.Copy(baseline));string newChapter=newChapterDoc.AddChapter();var addedChapter=newChapterDoc.Data.Chapter(newChapter);string unfinished=addedChapter.levels[0].id;
            newChapterDoc.Transfer(newChapterDoc.Data.Chapter("chapter_1"),newChapterDoc.Data.Chapter("chapter_1").levels[0],addedChapter);
            var newChapterReview=new CampaignPublication(baseline,newChapterDoc.Data);newChapterReview.Items.First(i=>i.key=="LevelAdd:"+unfinished).selected=false;
            Need(newChapterReview.Check(out var addedCandidate,out _)==null&&addedCandidate.Chapter(newChapter).levels.Length==1,"new chapters can publish a valid main while leaving an unfinished new level unchecked");
            newChapterReview.Items.First(i=>i.key=="ChapterAdd:"+newChapter).selected=false;
            Need(newChapterReview.Check(out _,out _)?.Contains("请同时勾选新增章节")==true,"unselected chapter dependency explains exactly what is missing");
            moveDoc.RemoveChapter("chapter_1");var removeChapterReview=new CampaignPublication(baseline,moveDoc.Data);
            removeChapterReview.Items.First(i=>i.key=="Placement:c1_01").selected=false;
            Need(removeChapterReview.Check(out _,out _)?.Contains("请同时勾选它的编排修改")==true,"chapter deletion cannot silently remove a level whose transfer is unchecked");
            removeChapterReview.Items.First(i=>i.key=="Placement:c1_01").selected=true;
            Need(removeChapterReview.Check(out _,out _)==null,"chapter deletion and the surviving level transfer can publish together");
            var c2=baseline.Chapter("chapter_2");var branch=c2.levels.First(l=>!l.main);
            Need(CampaignLabels.Prefix(baseline.catalog,branch.id).StartsWith("2-")&&CampaignLabels.Name(baseline.catalog,"c1_01","箱子")=="1-1 箱子","main and branch display prefixes are derived from the catalog");
            var reordered=CampaignPublication.Copy(baseline);Array.Reverse(reordered.boards);Array.Reverse(reordered.replays.levels);
            Need(CampaignPublication.SameContent(baseline,reordered),"asset reload ordering cannot create a phantom pending edit");
            var draft=new CampaignDraftFile{revision=project.Revision,baseline=baseline,working=change.Data,chapter="chapter_1",selection="c1_01"};project.SaveDraft(draft);
            string oldRevision=project.Revision,oldDraft=JsonUtility.ToJson(project.ReadDraft());
            bool rolledBack=false;try{project.Publish(partial,oldRevision,()=>throw new IOException("Injected draft-save failure"),true);}catch(IOException){rolledBack=true;}
            Need(rolledBack&&project.Revision==oldRevision&&JsonUtility.ToJson(project.ReadDraft())==oldDraft,"failure after asset imports rolls back project files and retains the full draft");
            bool conflict=false;try{project.Publish(partial,"stale-revision",null,true);}catch(IOException){conflict=true;}
            Need(conflict&&project.Revision==oldRevision,"external revision conflict refuses writeback");
            yield return Click("campaign");yield return Chapter("chapter_1");yield return Click("campaign-review");
            Need(app.UI.FindButton("campaign-publish").interactable,"invalid writeback remains clickable to explain the reason");
            yield return Click("campaign-publish");Need(TextContains("棺材")&&TextContains("尚未记录关卡解"),"writeback UI names the actual invalid elements rather than claiming not completed");
            Capture("editing-review-issues");
            yield return ToggleReview("Board:c1_02");yield return ToggleReview("LevelAdd:"+bad);
            string cache=Path.Combine("Library","DraculasBoxCampaign",AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(fixture.catalog)),"draft.json");
            using(var locked=new FileStream(cache,FileMode.Open,FileAccess.Read,FileShare.None))yield return Click("campaign-publish");
            Need(new CampaignProject(fixture).Revision==oldRevision&&app.UI.Root.Find("Publication review")!=null,"a real locked draft file keeps the review open and rolls back selected publication");
            yield return Click("campaign-publish");
            var published=new CampaignProject(fixture);var retained=published.ReadDraft();
            Need(published.Source.Board("c1_01").title=="箱子检查"&&published.Source.Board("c1_02").title=="阳光检查"&&published.Source.Board("c1_02").LayoutEquals(baseline.Board("c1_02"))&&published.Source.boards.Any(b=>b.id==good)&&!published.Source.boards.Any(b=>b.id==bad),"real selected writeback imports exactly the chosen names and new level");
            Need(retained.revision==published.Revision&&retained.working.Board("c1_02").coffin==new Cell(-1,-1)&&retained.working.boards.Any(b=>b.id==bad),"remaining draft reloads against the new project revision without losing unchecked work");
            var pending=new CampaignPublication(retained.baseline,retained.working);
            Need(pending.Items.Count==2&&pending.Items.Any(i=>i.key=="Board:c1_02")&&pending.Items.Any(i=>i.key=="LevelAdd:"+bad),"successful rows disappear and only unchecked changes remain pending");
            yield return Options("c1_01");
            Need(app.UI.FindButton("campaign-edit")==null&&app.UI.FindButton("campaign-test")==null&&app.UI.FindButton("campaign-copy")!=null&&app.UI.FindButton("campaign-transfer")!=null&&app.UI.FindButton("campaign-remove")!=null,"node menu contains only copy, move chapter and delete");
            Need(TextContains("1-1 箱子检查"),"node map and popup show chapter-level prefix");
            yield return Click("campaign-remove");Need(((RectTransform)app.UI.Root.Find("Remove level")).rect.height<320&&app.UI.Root.Find("Chapter map")!=null,"level deletion is a compact modal over the map");yield return new WaitForSecondsRealtime(.22f);Capture("editing-delete-dialog");
            yield return Click("campaign-cancel");yield return Click("campaign-transfer");Need(((RectTransform)app.UI.Root.Find("Move level")).rect.height<480,"chapter transfer uses a compact target chooser");
            yield return Click("campaign-popup-close");yield return Click("level-tools-close");yield return Click("select-level-c1_01");yield return Page(GamePage.Editing);
            var title=app.UI.Root.Find("Level title").GetComponent<TMP_InputField>();
            Need(title.text=="箱子检查"&&TextContains("1-1"),"editor prefix is separate from the editable stored title");
            title.ActivateInputField();EventSystem.current.SetSelectedGameObject(title.gameObject);yield return null;yield return null;
            Need(app.UI.HasTextFocus&&!app.Creator.HandleShortcut(KeyCode.A)&&!app.Creator.HandleShortcut(KeyCode.Q)&&!app.Creator.HandleShortcut(KeyCode.E),"A Q E remain normal text input while the title is focused");app.UI.CommitText();
            yield return Click("leave-editor");
            // Discard only the two fixture edits deliberately excluded earlier;
            // then publish a real reorder and verify no phantom draft is left.
            yield return Options(bad);yield return Click("campaign-remove");yield return Click("campaign-confirm");
            yield return Click("select-level-c1_02");yield return Page(GamePage.Editing);
            app.Creator.Document.Begin();app.Creator.Document.Paint(baseline.Board("c1_02").coffin,Brush.Coffin);app.Creator.Document.End();
            yield return Click("save");yield return Click("leave-editor");
            yield return Drag(good,"c1_04",new Vector2(-70,0));
            yield return Click("campaign-review");yield return Click("campaign-publish");
            Need(!new CampaignProject(fixture).HasDraft&&!app.UI.FindButton("campaign-review").interactable,"full writeback clears the draft and disables review after a real order change");
            // A separate unsaved personal fixture exercises pointer interaction without
            // putting test objects into the official working campaign.
            app.Edit(SelectionFixture());yield return Page(GamePage.Editing);var editor=app.Creator;var data=editor.Document.Data.Copy();
            Need(editor.HandleShortcut(KeyCode.A)&&editor.Operation==EditOperation.Select,"A enters selection");
            var start=app.Board.Screen(new Cell(4,3));var end=app.Board.Screen(new Cell(4,2));
            editor.PointerSample(start,true,true,false);editor.PointerSample(end,false,true,false);yield return null;
            Need(editor.Document.Data.ContentEquals(data)&&editor.SelectionGesture,"dragging renders a live move without editing the document");Capture("editing-selection-preview");
            editor.PointerSample(end,false,false,true);Need(editor.Document.Data.pieces[0].cell==new Cell(4,2)&&editor.Document.UndoCount==1,"release commits the object drag once");
            yield return Click("edit-undo");Need(editor.Document.Data.ContentEquals(data),"UI undo restores the object and circuit underneath");
            editor.HandleShortcut(KeyCode.R);start=app.Board.Screen(new Cell(3,2));end=app.Board.Screen(new Cell(5,3));
            editor.PointerSample(start,true,true,false);editor.PointerSample(end,false,true,false);editor.PointerSample(end,false,false,true);
            Need(editor.Selection.HasValue&&editor.Selection.Value.Layer==SelectionLayer.Region,"rectangle selection includes every layer in the region");
            start=app.Board.Screen(new Cell(4,3));end=app.Board.Screen(new Cell(5,3));editor.PointerSample(start,true,true,false);editor.PointerSample(end,false,true,false);editor.PointerSample(end,false,false,true);
            Need(editor.Document.Data.pieces[0].cell==new Cell(5,3)&&editor.Document.Data.plates[0].cell==new Cell(5,3)&&editor.Document.UndoCount==1,"pointer region drag is one reversible move including devices");
            Capture("editing-selection-region");yield return Click("edit-undo");editor.HandleShortcut(KeyCode.P);
            start=app.Board.Screen(new Cell(4,3));end=app.Board.Screen(new Cell(5,3));editor.PointerSample(start,true,true,false);editor.PointerSample(end,false,true,false);editor.PointerSample(end,false,false,true);
            Need(editor.Document.Data.ContentEquals(data)&&editor.Document.UndoCount==0,"pointer collision leaves the source untouched");
            start=app.Board.Screen(new Cell(4,3));end=app.Board.Screen(new Cell(4,2));editor.PointerSample(start,true,true,false);editor.PointerSample(end,false,true,false);editor.ClearSelection();
            Need(!editor.SelectionGesture&&editor.Document.Data.ContentEquals(data),"cancel discards an uncommitted selection move");
            Need(editor.HandleShortcut(KeyCode.Q)&&editor.Operation==EditOperation.Paint&&editor.HandleShortcut(KeyCode.E)&&editor.Operation==EditOperation.Erase,"Q and E switch paint and erase directly");
            Need(editor.HandleShortcut(KeyCode.LeftBracket)&&editor.Selected==Brush.MirrorSlash&&editor.HandleShortcut(KeyCode.RightBracket)&&editor.Selected==Brush.MirrorBackslash,"mirror brushes retain separate unambiguous bracket shortcuts");
            editor.HandleShortcut(KeyCode.A);start=app.Board.Screen(new Cell(4,3));editor.PointerSample(start,true,true,false);editor.PointerSample(start,false,false,true);editor.HandleShortcut(KeyCode.Delete);
            Need(editor.Document.Data.pieces.Length==1&&editor.Document.Data.plates.Length==1,"Delete removes the selected piece and preserves the pressure plate");
            yield return Click("leave-editor");Need(app.UI.Root.Find("Leave dialog")!=null&&((RectTransform)app.UI.Root.Find("Leave dialog")).rect.height<250,"unsaved-change prompt is also a compact modal");
            yield return new WaitForSecondsRealtime(.22f);Capture("editing-leave-dialog");yield return Click("cancel-leave");yield return Page(GamePage.Editing);Need(editor.Document.Data.pieces.Length==1,"cancelling the small leave dialog retains changes");
            yield return Click("leave-editor");yield return Click("discard-leave");yield return Ready();app.ShowTitle();yield return Page(GamePage.Title);
        }
    }
}
