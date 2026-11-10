using DraculasBox.Core;

static class EditorChecks
{
    public static void Run(Action<bool,string> require)
    {
        var draft=LevelData.Blank();draft.player=new Cell(-1,-1);draft.coffin=new Cell(-1,-1);
        require(Definition.DraftError(draft)==null,"unfinished draft with missing roles stays editable");
        require(!Definition.Preview(draft).Initial().Won,"an empty authoring draft is not a completed puzzle");
        var bad=draft.Copy();bad.pieces=bad.pieces.Concat(new[]{bad.pieces[0].Copy()}).ToArray();bad.pieces[1].id="another";
        require(Definition.DraftError(bad)!=null,"overlapping imported pieces rejected before preview");
        bad=draft.Copy();bad.player=new Cell(int.MaxValue,1);require(Definition.DraftError(bad)!=null,"invalid coordinates rejected before rendering");
        bad=draft.Copy();bad.player=bad.pieces[0].cell;require(Definition.DraftError(bad)!=null,"player overlapping object rejected");
        var edit=new EditDocument(LevelData.Blank());var original=edit.Data.Copy();
        edit.Begin();edit.Paint(new Cell(1,1),Brush.Box);edit.End(true);require(edit.Data.ContentEquals(original)&&!edit.Dirty&&edit.UndoCount==0,"cancel stroke restores document and baseline");
        string resize=edit.ResizeError(4,4);require(resize!=null&&!edit.Resize(4,4)&&edit.Data.ContentEquals(original)&&edit.UndoCount==0,"blocked crop explains issue and does not mutate history");
        edit.Rename("A");edit.Rename("B");edit.Undo();edit.Rename("C");require(!edit.Redo()&&edit.Data.title=="C","new edit invalidates abandoned redo branch");
        edit.Saved();edit.Begin();edit.Paint(new Cell(1,1),Brush.Box);edit.End();edit.Undo();require(!edit.Dirty,"grouped edits return to saved baseline");
        var saved=edit.Data.Copy();edit.Begin();edit.Paint(new Cell(1,1),Brush.Box);edit.End();edit.ConfigureSun(false,SunSide.Right);edit.Resize(32,32);
        while(edit.Undo()&&!edit.Data.ContentEquals(saved)){}require(edit.Data.ContentEquals(saved)&&!edit.Dirty,"mixed authoring history restores saved document");
        var rng=new Random(241109);edit=new EditDocument(LevelData.Blank(),false);
        for(int n=0;n<500;n++)
        {
            var before=edit.Data.Copy();edit.Begin();var brush=(Brush)rng.Next(8);
            for(int k=0;k<4;k++)edit.Paint(new Cell(rng.Next(-2,14),rng.Next(-2,10)),brush);
            edit.End();require(Definition.DraftError(edit.Data)==null,"random authoring keeps safe draft structure");
            if(!edit.Data.ContentEquals(before)){var after=edit.Data.Copy();edit.Undo();require(edit.Data.ContentEquals(before),"random grouped undo restores exact content");edit.Redo();require(edit.Data.ContentEquals(after),"random grouped redo restores exact content");}
        }
    }
}
