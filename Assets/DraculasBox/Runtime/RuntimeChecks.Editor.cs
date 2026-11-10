using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using TMPro;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private void Rejects(Action operation,string name)
        {bool rejected=false;try{operation();}catch(Exception e)when(e is IOException||e is InvalidDataException||e is ArgumentException||e is UnauthorizedAccessException){rejected=true;}Require(rejected,name);}
        private IEnumerator EditorRegression()
        {
            string root=Directory.GetParent(app.Library.Folder).FullName;
            Require(root.StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase),"destructive fixture checks are confined to isolated storage");
            var all=app.Library.Read(out _);var first=all[0];string selected=app.LibrarySelection;
            var other=all.First(d=>d.id!=selected);app.UI.HoverForCheck("custom-"+other.id);Require(app.LibrarySelection==selected,"hover never changes the target of copy or removal");
            yield return Click("custom-"+first.id);Require(app.Page==GamePage.Library&&app.LibrarySelection==first.id,"single click selects deliberately");
            app.UI.ClickForCheck("custom-"+first.id,2);yield return Ready();Require(app.Page==GamePage.Editing&&app.Creator.Document.Data.id==first.id,"double click opens selected level");
            var doc=app.Creator.Document;string originalName=doc.Data.title;byte[] bytes=File.ReadAllBytes(Path.Combine(app.Library.Folder,first.id+".json"));
            doc.Rename(originalName+" lock");
            using(var held=new FileStream(Path.Combine(app.Library.Folder,first.id+".json"),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {Require(!app.Creator.Save()&&doc.Dirty,"a locked destination reports save failure and keeps dirty draft");Require(app.UI.Root.Find("Editor status").GetComponent<TMP_Text>().text.Contains("保存失败"),"save failure is visible in the workbench");app.Library.Recover(doc.Data);Require(File.Exists(app.Library.RecoveryPath),"recovery still writes while saved destination is locked");}
            Require(bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(app.Library.Folder,first.id+".json")))&&!Directory.GetFiles(app.Library.Folder,"*.tmp").Any(),"failed save preserves old bytes and cleans its temporary file");
            Require(app.Creator.Save()&&!doc.Dirty&&app.Library.Recovery()==null,"retry saves successfully and suppresses stale recovery");
            yield return Click("scene");var saved=doc.Data.Copy();var input=app.UI.Root.Find("Scene properties/Board width").GetComponent<TMP_InputField>();int history=doc.UndoCount;
            input.text="99";input.onEndEdit.Invoke(input.text);Require(input.text==saved.rows[0].Length.ToString()&&doc.Data.ContentEquals(saved)&&doc.UndoCount==history,"invalid numeric size resets without changing data or history");
            input.text="3";input.onEndEdit.Invoke(input.text);Require(doc.Data.ContentEquals(saved)&&app.UI.Root.Find("Editor status").GetComponent<TMP_Text>().text.Contains("裁切"),"refused crop identifies a blocking object");
            yield return Click("scene");yield return Click("editor-help");Require(app.Page==GamePage.Confirming,"editor help is available on demand");yield return Tap(KeyCode.Escape);Require(app.Creator.Document==doc&&doc.UndoCount==history,"closing help keeps document and history");
            string exported=app.Library.Export(doc.Data);
            using(var held=new FileStream(exported,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {app.ExportEditor();yield return Ready();Require(app.UI.FindButton("error-back")!=null&&app.Page==GamePage.Confirming,"failed export displays an actionable error page");}
            yield return Click("error-back");Require(app.Creator.Document==doc&&doc.Data.ContentEquals(saved),"export failure does not lose original draft");
            yield return Click("leave-editor");Require(app.UI.FindButton("recover")==null,"saved drafts do not add a redundant recovery action");
            // Real pointer samples exercise stroke continuity rather than editing the document directly.
            yield return Click("new");doc=app.Creator.Document;var pristine=doc.Data.Copy();yield return Click("brush-1");
            var left=app.Board.Screen(new Cell(1,1));var right=app.Board.Screen(new Cell(9,1));
            app.Creator.PointerSample(left,true,true,false);app.Creator.PointerSample(new Vector2(-100,-100),false,true,false);app.Creator.PointerSample(right,false,true,false);app.Creator.PointerSample(right,false,false,true);yield return null;
            Require(doc.Data.rows[1][1]=='#'&&doc.Data.rows[1][9]=='#'&&doc.Data.rows[1][5]=='.'&&doc.UndoCount==1,"leaving and reentering canvas never paints an unintended connecting line");yield return Click("edit-undo");Require(doc.Data.ContentEquals(pristine),"interrupted stroke still undoes as one gesture");
            app.Creator.PointerSample(left,true,true,false);app.Creator.PointerSample(right,false,true,false);app.Creator.PointerSample(new Vector2(-100,-100),false,false,true);yield return null;
            Require(doc.Data.rows[1].Substring(1,9)==new string('#',9)&&doc.UndoCount==1,"fast drag fills skipped cells and releasing outside closes history");yield return Click("edit-undo");
            yield return Click("save");pristine=doc.Data.Copy();app.Creator.PointerSample(left,true,true,false);app.LeaveEditor();yield return Ready();Require(app.Page==GamePage.Confirming&&doc.Dirty&&doc.UndoCount>0,"leaving commits a pending stroke before checking unsaved changes");yield return Click("cancel-leave");yield return Click("edit-undo");Require(doc.Data.ContentEquals(pristine)&&!doc.Dirty,"cancel leave and undo restore saved baseline");
            Require(app.Creator.ResizeTo(32,32),"stress canvas expands safely");yield return null;yield return Click("zoom-in");
            var timer=new System.Diagnostics.Stopwatch();
            for(int x=12;x<=20;x++)
            {
                timer.Restart();var p=app.Board.Screen(new Cell(x,12));app.Creator.PointerSample(p,x==12,true,false);app.Creator.FlushPreview();timer.Stop();report.maxEditorPreviewMs=Mathf.Max(report.maxEditorPreviewMs,(float)timer.Elapsed.TotalMilliseconds);report.editorStressActions++;yield return null;
            }
            app.Creator.PointerSample(Vector2.zero,false,false,true);Require(doc.Data.rows[12].Substring(12,9)==new string('#',9),"maximum canvas accepts a continuous brush stroke");Require(report.maxEditorPreviewMs<500,"maximum-canvas preview avoids a half-second stall");Capture("workbench_maximum");
            yield return Click("edit-undo");yield return Click("edit-undo");yield return Click("zoom-fit");Require(doc.Data.ContentEquals(pristine)&&!doc.Dirty,"large-canvas stroke and resize undo exactly");Capture("workbench_final");yield return Click("leave-editor");
            var fixture=app.Library.Read(out _)[0];
            for(int n=0;n<5;n++){var copy=fixture.Copy();copy.id=Guid.NewGuid().ToString("N");copy.title="分页 "+n;app.Library.Save(copy);}
            app.LibraryPage(0);yield return Ready();yield return Click("next-page");Require(app.Page==GamePage.Library&&app.UI.Root.Find("Library page").GetComponent<TMP_Text>().text.StartsWith("2 /"),"multiple library pages stay reachable");Capture("library_multiple_pages");yield return Click("prev-page");
            var visible=app.Library.Read(out _).Take(GameUI.LibraryPageSize).First();app.UI.HoverForCheck("custom-"+visible.id);yield return Tap(KeyCode.Return);Require(app.Page==GamePage.Editing&&app.Creator.Document.Data.id==visible.id,"keyboard confirm opens the focused card");yield return Click("leave-editor");
            for(int n=0;n<3;n++){app.Edit(visible);yield return Ready();yield return Click("leave-editor");Require(app.Page==GamePage.Library,"repeated editor open and close");}
            var archived=app.Library.Duplicate(fixture);string archive=app.Library.Archive(archived.id);var reopened=new LevelLibrary(root);
            Require(reopened.LatestArchive()!=null&&!reopened.Exists(archived.id),"archive remains available after reopening library");reopened.Restore(archive);Require(reopened.Exists(archived.id)&&!File.Exists(archive),"reopened library restores archived level");
            archive=reopened.Archive(archived.id);var conflict=archived.Copy();conflict.title="保留的新版本";reopened.Save(conflict);bytes=File.ReadAllBytes(Path.Combine(reopened.Folder,conflict.id+".json"));Rejects(()=>reopened.Restore(archive),"restore refuses to overwrite an existing version");Require(File.Exists(archive)&&bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(reopened.Folder,conflict.id+".json"))),"restore conflict preserves archive and current version");
            var draftA=fixture.Copy();draftA.title="恢复版本 A";reopened.Recover(draftA);var draftB=draftA.Copy();draftB.title="恢复版本 B";reopened.Recover(draftB);File.WriteAllText(reopened.RecoveryPath,"{corrupt");Require(new LevelLibrary(root).Recovery()?.title==draftA.title,"corrupt recovery falls back to last complete checkpoint");reopened.DiscardRecovery(draftA.id);Require(reopened.Recovery()==null,"discarded corrupted recovery is not resurrected from backup");
            string fixtures=Path.Combine(root,"MalformedFixtures");Directory.CreateDirectory(fixtures);var malformed=new List<LevelData>();
            var bad=fixture.Copy();bad.rows=new[]{"...","..","..."};malformed.Add(bad);
            bad=fixture.Copy();bad.schemaVersion=999;malformed.Add(bad);bad=fixture.Copy();bad.pieces=Enumerable.Range(0,129).Select(n=>new EntityData{id="excess-"+n,cell=new Cell(1,1)}).ToArray();malformed.Add(bad);
            bad=fixture.Copy();bad.pieces=new EntityData[]{null};malformed.Add(bad);bad=fixture.Copy();bad.player=new Cell(int.MaxValue,-2);malformed.Add(bad);
            bad=fixture.Copy();bad.pieces=new[]{new EntityData{id="a",cell=fixture.pieces[0].cell},new EntityData{id="b",cell=fixture.pieces[0].cell}};malformed.Add(bad);
            bad=fixture.Copy();bad.coffin=bad.player;malformed.Add(bad);bad=fixture.Copy();bad.sunFrom=(SunSide)99;malformed.Add(bad);
            bad=fixture.Copy();bad.pieces=new[]{new EntityData{id="a",kind=(PieceKind)99,cell=fixture.pieces[0].cell}};malformed.Add(bad);
            bad=fixture.Copy();bad.pieces=new[]{new EntityData{id="same",cell=new Cell(1,1)},new EntityData{id="same",cell=new Cell(2,1)}};malformed.Add(bad);
            int fileIndex=0;foreach(var data in malformed){string path=Path.Combine(fixtures,(fileIndex++)+".json");File.WriteAllText(path,JsonUtility.ToJson(data));Rejects(()=>reopened.PreviewImport(path),"malformed import rejected before preview "+fileIndex);Require(File.Exists(path),"malformed source preserved "+fileIndex);}
            var noPieces=fixture.Copy();noPieces.pieces=null;string normalized=Path.Combine(fixtures,"empty-pieces.json");File.WriteAllText(normalized,JsonUtility.ToJson(noPieces));Require(reopened.PreviewImport(normalized).pieces.Length==0,"Unity canonical empty arrays remain safe importable layouts");
            foreach(string text in new[]{"[]","{}","{\"schemaVersion\":1,\"rows\":null}"}){string path=Path.Combine(fixtures,"invalid-root-"+(fileIndex++)+".json");File.WriteAllText(path,text);Rejects(()=>reopened.PreviewImport(path),"invalid JSON root rejected");}
            string oversized=Path.Combine(fixtures,"oversized.json");File.WriteAllText(oversized,new string(' ',LevelLibrary.MaxFileBytes+1));Rejects(()=>reopened.PreviewImport(oversized),"oversized import rejected before parsing");
            var unfinished=fixture.Copy();unfinished.player=unfinished.coffin=new Cell(-1,-1);string incomplete=Path.Combine(fixtures,"unfinished.json");File.WriteAllText(incomplete,JsonUtility.ToJson(unfinished));Require(reopened.PreviewImport(incomplete)!=null,"incomplete but structurally safe draft remains importable for repair");
            app.OpenLibrary();yield return Ready();yield return Click("invalid-files");Require(app.UI.Root.Find("Operation error").GetComponent<TMP_Text>().text.Contains("broken-layout.json"),"library diagnostics name the malformed file");yield return Click("error-back");Capture("library_workbench_final");
        }
    }
}
