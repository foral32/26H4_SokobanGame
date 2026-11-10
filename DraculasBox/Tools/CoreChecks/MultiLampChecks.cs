using DraculasBox.Core;

internal static class MultiLampChecks
{
    public static int Run()
    {
        int n=0;void Check(bool value,string label){n++;if(!value)throw new Exception(label);}
        var d=new LevelData{id="multi",title="multi",rows=new[]{"#########","#.......#","#.......#","#.......#","#.......#","#.......#","#########"},player=new Cell(1,5),coffin=new Cell(7,5),pieces=new[]{new EntityData{id="L1",kind=PieceKind.Lamp,cell=new Cell(2,3)},new EntityData{id="L2",kind=PieceKind.Lamp,cell=new Cell(5,1)}}};
        var def=new Definition(d);var field=LightField.Compute(def.Initial());Check(field.Lit(new Cell(5,3))&&field.LampAxes[3*9+5]==3,"crossed lamps retain both beam axes");Check(!field.Lit(new Cell(4,2)),"multiple point lights do not spill onto diagonals");
        var blocked=d.Copy();blocked.pieces=blocked.pieces.Concat(new[]{new EntityData{id="A",cell=new Cell(3,3)}}).ToArray();field=LightField.Compute(new Definition(blocked).Initial());Check(field.Lit(new Cell(5,3))&&field.LampAxes[3*9+5]==2,"one shadow does not cancel another lamp");blocked.pieces=blocked.pieces.Concat(new[]{new EntityData{id="B",cell=new Cell(5,2)}}).ToArray();field=LightField.Compute(new Definition(blocked).Initial());Check(!field.Lit(new Cell(5,3)),"all illuminating rays must be occluded");
        var doc=new EditDocument(LevelData.Blank());doc.Begin();foreach(var p in new[]{new Cell(1,1),new Cell(2,1),new Cell(3,1)})Check(doc.Paint(p,Brush.Lamp),"each lamp brush stamp adds a new lamp");doc.End();Check(doc.Data.pieces.Count(p=>p.kind==PieceKind.Lamp)==3&&doc.Data.pieces.Select(p=>p.id).Distinct().Count()==doc.Data.pieces.Length,"independent lamps keep unique identities");doc.Undo();Check(!doc.Data.pieces.Any(p=>p.kind==PieceKind.Lamp),"undo removes grouped lamp stamps");doc.Redo();doc.Begin();doc.Paint(new Cell(2,1),Brush.Erase);doc.End();Check(doc.Data.pieces.Count(p=>p.kind==PieceKind.Lamp)==2&&doc.Data.pieces.Any(p=>p.cell==new Cell(1,1))&&doc.Data.pieces.Any(p=>p.cell==new Cell(3,1)),"erasing one lamp preserves the others");
        var chain=d.Copy();chain.player=new Cell(2,3);chain.pieces=new[]{new EntityData{id="A",cell=new Cell(3,3)},new EntityData{id="L1",kind=PieceKind.Lamp,cell=new Cell(4,3)},new EntityData{id="L2",kind=PieceKind.Lamp,cell=new Cell(5,3)}};var s=new Session(new Definition(chain));string key=s.State.Key();var move=s.Apply(Command.Right);Check(move.Kind==ActionKind.Pushed&&move.PushedCount==3&&s.State.Position(1)==new Cell(5,3)&&s.State.Position(2)==new Cell(6,3),"a shielded chain moves both lamps");s.Apply(Command.Undo);Check(s.State.Key()==key,"undo restores every light source");
        return n;
    }
}
