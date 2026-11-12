using System.Collections;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEngine;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunCanvas()=>Begin(CanvasEditing(),"editor-canvas",true);
        private static IEnumerator CanvasEditing()
        {
            var data=LevelData.Blank(11,7);data.title="画布编辑";
            data.rows=Enumerable.Repeat("...........",7).ToArray();data.player=new Cell(7,5);data.coffin=new Cell(9,5);
            data.pieces=new[]{new EntityData{id="a",cell=new Cell(2,2)},new EntityData{id="b",cell=new Cell(2,4)},new EntityData{id="gap",cell=new Cell(4,3)}};
            app.Edit(data);yield return Ready();var editor=app.Creator;var doc=editor.Document;
            Need(app.UI.FindButton("brush-7")==null,"erase no longer occupies a mode button");
            editor.SelectShape(PaintShape.Rectangle);editor.SetOperation(EditOperation.Select);
            Need(app.UI.FindButton("shape-brush")==null&&app.UI.FindButton("shape-rectangle")==null&&!editor.HandleShortcut(KeyCode.P)&&!editor.HandleShortcut(KeyCode.R),"selection hides shape controls and ignores shape shortcuts");
            var a=app.Board.Screen(new Cell(2,2));var b=app.Board.Screen(new Cell(3,2));
            editor.PointerSample(a,true,true,false);editor.PointerSample(b,false,true,false);editor.PointerSample(b,false,false,true);
            Need(doc.Data.pieces[0].cell==new Cell(3,2),"direct object dragging ignores previously selected rectangle paint shape");editor.Undo();
            a=app.Board.Screen(new Cell(1,1));b=app.Board.Screen(new Cell(2,4));
            editor.PointerSample(a,true,true,false);editor.PointerSample(b,false,true,false);editor.PointerSample(b,false,false,true);
            a=app.Board.Screen(new Cell(2,2));b=app.Board.Screen(new Cell(4,2));
            editor.PointerSample(a,true,true,false);editor.PointerSample(b,false,true,false);editor.PointerSample(b,false,false,true);yield return Ready();
            Need(doc.Data.pieces[0].cell==new Cell(4,2)&&doc.Data.pieces[1].cell==new Cell(4,4)&&doc.Data.pieces[2].cell==new Cell(4,3),"sparse region moves around the stationary box in its hole");
            Capture("canvas-selection-gaps");editor.Undo();editor.SetOperation(EditOperation.Paint);editor.SelectShape(PaintShape.Brush);
            a=app.Board.Screen(new Cell(6,2));b=app.Board.Screen(new Cell(6,3));
            editor.MouseSample(a,true,true,false,false,false,false);
            editor.MouseSample(b,false,true,false,true,true,false);
            editor.MouseSample(b,false,true,false,false,false,true);
            Need(doc.UndoCount==0&&doc.Data.pieces.Length==5,"secondary mouse release neither erases nor ends left paint stroke");
            editor.MouseSample(b,false,false,true,false,false,false);Need(doc.UndoCount==1,"left stroke commits on its own release");editor.Undo();
            a=app.Board.Screen(new Cell(2,2));editor.MouseSample(a,false,false,false,true,true,false);editor.MouseSample(a,false,false,false,false,false,true);
            Need(doc.Data.pieces.Length==2&&editor.Selected==Brush.Box&&editor.Operation==EditOperation.Paint,"right click erases while keeping paint mode and material");editor.Undo();
            editor.SelectShape(PaintShape.Rectangle);a=app.Board.Screen(new Cell(1,1));b=app.Board.Screen(new Cell(4,4));
            editor.MouseSample(a,false,false,false,true,true,false);editor.MouseSample(b,false,false,false,false,true,false);
            Need(doc.Data.ContentEquals(data)&&editor.RegionActive,"right rectangle is a non-destructive preview");
            editor.MouseSample(b,false,false,false,false,false,true);Need(doc.Data.pieces.Length==0&&doc.UndoCount==1,"right rectangle erases as one undo");editor.Undo();editor.SelectShape(PaintShape.Brush);
            // Left expansion must not move the old board beneath the pointer.
            var anchor=app.Board.Screen(new Cell(2,2));var outside=app.Board.Screen(new Cell(-1,3));
            Need(app.Board.InViewport(outside),"fitted canvas leaves room outside the board");
            editor.PointerSample(outside,true,true,false);editor.PointerSample(outside,false,false,true);editor.FlushPreview();yield return Ready();
            Need(doc.Data.rows[0].Length==12&&doc.Data.pieces.Last().cell==new Cell(0,3)&&Vector2.Distance(anchor,app.Board.Screen(new Cell(3,2)))<.2f,"left expansion preserves screen positions and places into the intended new cell");
            Need(doc.UndoCount==1,"expansion and object placement form one undo");Capture("canvas-expanded");editor.Undo();
            Need(doc.Data.ContentEquals(data),"expansion undo restores original draft");
            app.Board.ResetEditorView();a=app.Board.Screen(new Cell(0,0));
            editor.Select(Brush.Wall);outside=app.Board.Screen(new Cell(-1,1));app.Board.BrushPreview(new Cell(-1,1),Brush.Wall,true);app.Board.ExpansionPreview(new Cell(-1,1),true);yield return Ready();Capture("canvas-expansion-preview");
            app.Board.BrushPreview(null,Brush.Wall,true);app.Board.ExpansionPreview(null,false);
            var hoverMethod=typeof(LevelEditor).GetMethod("UpdateLineControls",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var top=app.Board.Screen(new Cell(3,0));var below=app.Board.Screen(new Cell(3,1));
            hoverMethod.Invoke(editor,new object[]{top+Vector2.up*(top.y-below.y)*.82f});
            Need(app.UI.FindButton("line-insert")!=null,"hovering the top gutter reveals column controls");
            var plusRect=(RectTransform)app.UI.FindButton("line-insert").transform;
            hoverMethod.Invoke(editor,new object[]{RectTransformUtility.WorldToScreenPoint(app.viewCamera,plusRect.position)});
            Need(app.UI.FindButton("line-insert")!=null,"moving onto the floating plus keeps it available");
            hoverMethod.Invoke(editor,new object[]{app.Board.Screen(new Cell(5,4))});
            Need(app.UI.FindButton("line-insert")==null,"edge controls disappear away from the gutter");
            editor.ShowLineControls(true,3);app.Board.LinePreview(true,4,true);yield return Ready();
            Need(app.UI.FindButton("line-insert")!=null&&app.UI.FindButton("line-delete")!=null,"column controls appear on the canvas edge");Capture("canvas-insert-column");
            var screen=app.Board.Screen(new Cell(2,2));yield return Click("line-insert");
            Need(doc.Data.rows[0].Length==12&&doc.Data.pieces[2].cell==new Cell(5,3)&&Vector2.Distance(screen,app.Board.Screen(new Cell(2,2)))<.2f,"column insertion shifts only later cells and keeps view stable");editor.Undo();app.Board.ResetEditorView();
            editor.ShowLineControls(false,2);app.Board.LinePreview(false,2,false);yield return Ready();Capture("canvas-delete-row");yield return Click("line-delete");
            Need(doc.Data.rows.Length==6&&doc.Data.pieces.Length==2&&doc.Data.pieces[0].id=="b"&&doc.Data.pieces[0].cell==new Cell(2,3),"row deletion removes intersected objects and shifts later rows");editor.Undo();Need(doc.Data.ContentEquals(data),"row deletion undo restores complete draft");
            app.Board.ResetEditorView();var baseline=doc.Data.Copy();editor.Select(Brush.Box);editor.SelectShape(PaintShape.Rectangle);
            a=app.Board.Screen(new Cell(9,2));b=app.Board.Screen(new Cell(11,3));editor.PointerSample(a,true,true,false);editor.PointerSample(b,false,true,false);editor.CancelRectangle();
            Need(doc.Data.ContentEquals(baseline)&&doc.UndoCount==0,"cancelled outside rectangle cannot resize the document");
            editor.PointerSample(a,true,true,false);editor.PointerSample(b,false,true,false);editor.PointerSample(b,false,false,true);editor.FlushPreview();
            Need(doc.Data.rows[0].Length==12&&doc.UndoCount==1&&doc.Data.pieces.Length==9,"rectangle spans existing and newly expanded cells atomically");editor.Undo();
            Need(doc.Data.ContentEquals(data),"outside rectangle undo restores original content");
            app.ShowTitle();yield return Page(GamePage.Title);
        }
    }
}
