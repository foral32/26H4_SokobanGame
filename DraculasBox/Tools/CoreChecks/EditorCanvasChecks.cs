using DraculasBox.Core;

internal static class EditorCanvasChecks
{
    public static void Run()
    {
        int checks=0;
        void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
        LevelData Room(int w=12,int h=8)=>new LevelData{id="canvas-spec",title="Canvas",rows=Enumerable.Repeat(new string('.',w),h).ToArray(),player=new Cell(w-2,h-2),coffin=new Cell(w-1,h-1),pieces=Array.Empty<EntityData>(),plates=Array.Empty<DeviceData>(),doors=Array.Empty<DeviceData>()};
        var data=Room();data.pieces=new[]{new EntityData{id="a",cell=new Cell(2,2)},new EntityData{id="b",cell=new Cell(2,4)},new EntityData{id="keep",cell=new Cell(4,3)}};
        data.plates=new[]{new DeviceData{cell=new Cell(2,4),channel=2}};
        var row=data.rows[5].ToCharArray();row[4]='#';data.rows[5]=new string(row);
        data.rows[0]=" "+data.rows[0].Substring(1);
        var doc=new EditDocument(data);var region=new EditSelection(new Cell(2,2),new Cell(2,5));
        Check(doc.PreviewMove(region,new Cell(2,0),out var candidate)==null,"empty holes do not collide with an external box or wall");
        Check(doc.Data.ContentEquals(data)&&doc.UndoCount==0,"preview is read-only");
        Check(candidate.pieces[2].cell==new Cell(4,3)&&candidate.rows[5][4]=='#'&&candidate.plates[0].cell==new Cell(4,4),"stationary hole content and selected device survive");
        Check(doc.MoveSelection(region,new Cell(2,0),out _)&&doc.UndoCount==1,"sparse move commits once");doc.Undo();Check(doc.Data.ContentEquals(data),"sparse move undo is exact");
        Check(!doc.MoveSelection(region,new Cell(2,1),out _)&&doc.Data.ContentEquals(data)&&doc.UndoCount==0,"actual occupied destinations reject atomically");
        Check(doc.MoveSelection(new EditSelection(new Cell(1,1),new Cell(2,4)),new Cell(1,0),out _),"self-overlapping rectangle may move");doc.Undo();
        var wall=doc.PickSelection(new Cell(4,5));Check(!doc.MoveSelection(wall,new Cell(0,-2),out _)&&doc.Data.ContentEquals(data),"wall cannot overwrite stationary box");
        var piece=doc.PickSelection(new Cell(2,2));Check(doc.MoveSelection(piece,new Cell(1,0),out _)&&doc.Data.plates[0].cell==new Cell(2,4),"single object movement is independent");doc.Undo();

        foreach(var cell in new[]{new Cell(-2,1),new Cell(3,-2),new Cell(13,2),new Cell(2,10),new Cell(-2,-2)})
        {
            doc=new EditDocument(data);doc.Begin();Check(doc.PaintExpanded(cell,Brush.Box,0,out var shift,out var error)&&error==null,"outside paint expands");doc.End();
            Check(doc.Data.pieces.Last().cell==cell+shift&&doc.Data.player==data.player+shift&&doc.Data.coffin==data.coffin+shift,"negative origin shifts every entity");
            Check(doc.Data.plates[0].channel==2&&doc.Data.plates[0].cell==data.plates[0].cell+shift&&Definition.DraftError(doc.Data)==null,"expansion preserves device channel and valid data");
            Check(Enumerable.Range(0,doc.Data.rows.Length).All(y=>Enumerable.Range(0,doc.Data.rows[0].Length).All(x=>
                doc.Data.rows[y][x]==(x>=shift.x&&y>=shift.y&&x<shift.x+data.rows[0].Length&&y<shift.y+data.rows.Length?data.rows[y-shift.y][x-shift.x]:'.'))),"new area is floor in every direction while original walls and gaps remain intact");
            Check(doc.UndoCount==1&&doc.Dirty,"expansion and paint are one edit");var grown=doc.Data.Copy();doc.Undo();Check(doc.Data.ContentEquals(data)&&!doc.Dirty,"growth undo restores exact dimensions and saved baseline");doc.Redo();Check(doc.Data.ContentEquals(grown),"growth redo is exact");
        }
        doc=new EditDocument(data);doc.Begin();doc.PaintExpanded(new Cell(-1,0),Brush.Wall,0,out _,out _);doc.PaintExpanded(new Cell(-1,0),Brush.Wall,0,out _,out _);doc.End();
        Check(doc.UndoCount==1&&doc.Data.rows[0].Length==14,"repeated left expansion shares one stroke");doc.Undo();Check(doc.Data.ContentEquals(data),"multi-expansion undo is exact");
        doc=new EditDocument(data);Check(!doc.PaintExpanded(new Cell(-1,0),Brush.Erase,0,out _,out _)&&doc.Data.ContentEquals(data)&&doc.UndoCount==0,"erase outside cannot grow board");
        Check(!doc.PaintExpanded(new Cell(-40,0),Brush.Box,0,out _,out var limit)&&limit!=null&&doc.Data.ContentEquals(data),"growth limit is atomic");
        var result=doc.PaintExpandedRectangle(new Cell(-2,-2),new Cell(-1,-1),Brush.Lamp,0,out var offset,out var rectangleError);
        Check(result.Applied==4&&rectangleError==null&&offset==new Cell(2,2)&&doc.UndoCount==1,"outside rectangle creates four cells in one edit");
        Check(doc.Data.rows[0][3]=='.'&&doc.Data.rows[0][0]=='.'&&doc.Data.rows[2][2]==' '&&doc.Data.pieces.Length==7,"rectangle expansion pads with floor and preserves original gaps");doc.Undo();Check(doc.Data.ContentEquals(data),"rectangle undo restores exact board");
        result=doc.PaintExpandedRectangle(new Cell(-2,-2),new Cell(-1,-1),Brush.Player,0,out _,out _);
        Check(result.Applied==0&&doc.Data.ContentEquals(data)&&doc.UndoCount==0,"invalid unique-role rectangle does not expand");
        foreach(bool column in new[]{true,false})
        {
            int size=column?data.rows[0].Length:data.rows.Length;
            for(int index=0;index<=size;index++)
            {
                doc=new EditDocument(data);Check(doc.ChangeLine(column,index,true,out _)&&Definition.DraftError(doc.Data)==null,"insert line at every boundary");
                Check(doc.ChangeLine(column,index,false,out _)&&doc.Data.ContentEquals(data),"inserting then removing empty line restores spatial content");
            }
            for(int index=0;index<size;index++)
            {
                doc=new EditDocument(data);Check(doc.ChangeLine(column,index,false,out _)&&Definition.DraftError(doc.Data)==null,"delete line removes roles and devices safely");
                Check(doc.UndoCount==1&&doc.Undo()&&doc.Data.ContentEquals(data),"deleted line and contents restore in one undo");
            }
        }
        doc=new EditDocument(Room(3,3));Check(!doc.ChangeLine(true,1,false,out _)&&!doc.ChangeLine(false,1,false,out _)&&doc.UndoCount==0,"minimum bounds protected");
        doc=new EditDocument(Room(32,32));Check(!doc.ChangeLine(true,32,true,out _)&&!doc.ChangeLine(false,32,true,out _)&&doc.UndoCount==0,"maximum bounds protected");
        Check(doc.PaintExpanded(new Cell(-1,0),Brush.Box,0,out _,out _) == false&&doc.UndoCount==0,"outside maximum board cannot mutate");
        Console.WriteLine($"Editor canvas checks passed: {checks}");
    }
}
