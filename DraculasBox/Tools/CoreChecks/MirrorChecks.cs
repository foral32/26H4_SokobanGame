using DraculasBox.Core;

internal static class MirrorChecks
{
    public static int Run()
    {
        int checks=0;void Need(bool ok,string label){checks++;if(!ok)throw new Exception("mirror: "+label);}
        LevelData Board(params EntityData[] pieces)=>new LevelData{id="mirror-check",title="Mirror",rows=new[]{"#######","#.....#","#.....#","#.....#","#.....#","#.....#","#######"},player=new Cell(5,5),coffin=new Cell(5,1),pieces=pieces};
        EntityData Piece(string id,int kind,Cell pos)=>new EntityData{id=id,kind=(PieceKind)kind,cell=pos};
        for(int kind=2;kind<=3;kind++)for(int direction=0;direction<4;direction++)
        {
            var d=LightField.Directions[direction];var center=new Cell(3,3);var source=new Cell(3-d.x*2,3-d.y*2);
            var data=Board(Piece("L",1,source),Piece("M",kind,center));
            Need(Definition.DraftError(data)==null,"new entity kind remains structurally valid");
            var f=LightField.Compute(new Definition(data,false).Initial());
            var turn=kind==2?new Cell(-d.y,-d.x):new Cell(d.y,d.x);
            Need(f.Lit(center+turn),"reflection from every side "+kind+" "+direction);
            Need(!f.Lit(center+d),"no straight-through beam "+kind+" "+direction);
        }
        var twice=Board(Piece("L",1,new Cell(1,3)),Piece("M",2,new Cell(3,3)),Piece("N",3,new Cell(3,1)));
        var doubleField=LightField.Compute(new Definition(twice,false).Initial());
        Need(doubleField.Lit(new Cell(2,1))&&!doubleField.Lit(new Cell(4,1)),"two consecutive turns");
        twice.pieces=twice.pieces.Concat(new[]{Piece("B",0,new Cell(3,2))}).ToArray();
        Need(!LightField.Compute(new Definition(twice,false).Initial()).Lit(new Cell(2,1)),"box stops a reflected ray");
        var intro=Board(Piece("L",1,new Cell(1,3)),Piece("M",2,new Cell(3,3)));intro.rows=new[]{"#######","###.###","##..###","#.....#","#.....#","#.....#","#######"};intro.player=new Cell(4,4);intro.coffin=new Cell(3,1);
        var session=new Session(new Definition(intro));string initial=session.State.Key();
        session.Apply(Command.Left);Need(session.Apply(Command.Up).Kind==ActionKind.Died,"pushing mirror into incoming ray kills player");
        session.Apply(Command.Undo);Need(!session.State.Dead&&session.State.Position(1)==new Cell(3,3),"undo restores mirror and life");session.Apply(Command.Undo);
        foreach(char c in "ULUU")session.Apply((Command)"URDL".IndexOf(c));Need(session.State.Won,"approved mirror puzzle is playable");
        session.Apply(Command.Reset);Need(session.State.Key()==initial,"reset restores reflected layout");
        var doc=new EditDocument(LevelData.Blank());doc.Begin();doc.Paint(new Cell(3,2),(Brush)10);doc.Paint(new Cell(4,2),(Brush)11);doc.End();
        Need((int)doc.Data.pieces.Single(p=>p.cell==new Cell(3,2)).kind==2&&(int)doc.Data.pieces.Single(p=>p.cell==new Cell(4,2)).kind==3,"both editor brushes preserve mirror kind");
        doc.Undo();Need(doc.Data.pieces.Length==1,"mirror stroke is one undo");doc.Redo();Need(doc.Data.pieces.Length==3,"mirror stroke redo");
        // A source on a four-mirror ring must stop when returning to the opaque lamp.
        var ring=Board(Piece("L",1,new Cell(3,2)),Piece("A",2,new Cell(2,2)),Piece("B",3,new Cell(4,2)),Piece("C",2,new Cell(4,4)),Piece("D",3,new Cell(2,4)));
        var ringField=LightField.Compute(new Definition(ring,false).Initial());Need(ringField.Lit(new Cell(3,4)),"closed reflected route terminates without losing lit cells");
        var combined=Board(Piece("L",1,new Cell(1,3)),Piece("K",1,new Cell(3,5)),Piece("M",2,new Cell(3,3)));
        var together=LightField.Compute(new Definition(combined,false).Initial());Need(together.ReflectionEdges[3*7+3]==15&&together.Lit(new Cell(3,2))&&together.Lit(new Cell(4,3)),"both mirror faces combine independent lamp rays");
        combined.pieces=combined.pieces.Concat(new[]{Piece("B",0,new Cell(3,4))}).ToArray();together=LightField.Compute(new Definition(combined,false).Initial());
        Need(together.Lit(new Cell(3,2))&&!together.Lit(new Cell(4,3)),"blocking one source retains the other reflection");
        var circuit=Board(Piece("L",1,new Cell(1,3)),Piece("M",2,new Cell(3,3)),Piece("N",3,new Cell(5,4)));
        circuit.plates=new[]{new DeviceData{cell=new Cell(5,4)}};circuit.doors=new[]{new DeviceData{cell=new Cell(3,2)}};
        var weight=new Session(new Definition(circuit));Need(weight.State.DoorOpen(0)&&LightField.Compute(weight.State).Lit(new Cell(3,1)),"mirror weight opens a door to reflected light");
        weight.Apply(Command.Up);weight.Apply(Command.Left);Need(!weight.State.DoorOpen(0)&&!LightField.Compute(weight.State).Lit(new Cell(3,1)),"leaving the plate closes and stops a reflection");
        weight.Apply(Command.Undo);Need(weight.State.DoorOpen(0)&&LightField.Compute(weight.State).Lit(new Cell(3,1)),"undo restores the light through the door");
        foreach(var kind in new[]{PieceKind.MirrorSlash,PieceKind.MirrorBackslash})foreach(SunSide side in Enum.GetValues<SunSide>())
        {
            var shade=Board(Piece("M",(int)kind,new Cell(3,3)));shade.sunEnabled=true;shade.sunFrom=side;
            var sun=LightField.Compute(new Definition(shade,false).Initial());Need(!sun.Lit(new Cell(3,3)+LightField.SunTravel[(int)side]),"mirror retains same-height solar shadow");
        }
        return checks;
    }
}
