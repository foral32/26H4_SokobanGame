using DraculasBox.Core;

static class AreaChecks
{
    public static void Run(Action<bool,string> require)
    {
        var doc=new EditDocument(LevelData.Blank());var original=doc.Data.Copy();
        var result=doc.PaintRectangle(new Cell(5,2),new Cell(3,1),Brush.Box);
        require(result.Applied==6&&doc.Data.pieces.Length==original.pieces.Length+6&&doc.UndoCount==1,"reverse rectangle places six boxes in one history record");
        doc.Undo();require(doc.Data.ContentEquals(original)&&!doc.Dirty,"rectangle undo restores complete saved content");doc.Redo();require(doc.Data.pieces.Length==7,"rectangle redo restores every box");doc.Undo();
        result=doc.PaintRectangle(new Cell(1,1),new Cell(9,5),Brush.Wall);
        require(doc.Data.player==original.player&&doc.Data.coffin==original.coffin&&doc.Data.pieces.Length==1&&result.Skipped>=3,"terrain rectangles preserve occupied cells");doc.Undo();
        result=doc.PaintRectangle(new Cell(-100,-100),new Cell(1,1),Brush.Floor);require(result.Applied==4,"rectangle bounds clip to board");doc.Undo();
        int history=doc.UndoCount;result=doc.PaintRectangle(new Cell(-9,-9),new Cell(-2,-2),Brush.Box);require(result.Applied==0&&doc.UndoCount==history,"fully outside rectangle is a no-op");
        foreach(var brush in new[]{Brush.Player,Brush.Coffin}){var snapshot=doc.Data.Copy();result=doc.PaintRectangle(new Cell(1,1),new Cell(3,2),brush);require(result.Applied==0&&doc.Data.ContentEquals(snapshot),"unique roles never mass duplicate");}
        result=doc.PaintRectangle(new Cell(1,1),new Cell(2,2),Brush.Lamp);require(result.Applied==4&&doc.Data.pieces.Count(p=>p.kind==PieceKind.Lamp)==4,"rectangle lamp placement follows multi-light rules");doc.Undo();
        doc.PaintRectangle(new Cell(1,1),new Cell(3,2),Brush.Plate,2);require(doc.Data.plates.Length==6&&doc.Data.plates.All(p=>p.channel==2),"rectangle devices use selected connection group");doc.Undo();
        doc.Begin();doc.Paint(new Cell(2,2),Brush.Plate,1);doc.Paint(new Cell(2,2),Brush.Box);doc.End();var layered=doc.Data.Copy();
        doc.PaintRectangle(new Cell(0,0),new Cell(10,6),Brush.Erase);require(doc.Data.player==new Cell(-1,-1)&&doc.Data.coffin==new Cell(-1,-1)&&doc.Data.pieces.Length==0&&doc.Data.plates.Length==0&&doc.Data.rows.All(r=>r.All(c=>c=='.')),"area erase clears every layer and preserves floor");doc.Undo();require(doc.Data.ContentEquals(layered),"area erase undo restores layered objects and roles");
        var big=new EditDocument(LevelData.Blank());big.Resize(32,32);result=big.PaintRectangle(new Cell(0,0),new Cell(31,31),Brush.Box);require(big.Data.pieces.Length==128&&Definition.DraftError(big.Data)==null&&result.Skipped>0,"maximum rectangle respects piece limit and valid structure");
    }
}
