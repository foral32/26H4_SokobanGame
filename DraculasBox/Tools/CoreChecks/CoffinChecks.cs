using System.Text.Json;
using DraculasBox.Core;

internal static class CoffinChecks
{
    public static int Run()
    {
        int checks=0;
        void Require(bool value,string label){checks++;if(!value)throw new Exception(label);}
        LevelData Room()=>new LevelData{id="coffin-spec",title="coffin",rows=Enumerable.Repeat(".......",7).ToArray(),player=new Cell(3,3),coffin=new Cell(4,3)};
        foreach(var side in Enum.GetValues<SunSide>())
        {
            var travel=LightField.SunTravel[(int)side];var d=Room();d.sunEnabled=true;d.sunFrom=side;
            d.pieces=new[]{new EntityData{id="shade",cell=new Cell(3,3)}};
            d.player=d.pieces[0].cell+travel;var turn=new Cell(-travel.y,travel.x);d.coffin=d.player+turn;
            var cmd=(Command)Array.IndexOf(LightField.Directions,turn);var session=new Session(new Definition(d));string initial=session.State.Key();
            var result=session.Apply(cmd);
            Require(result.Changed&&result.Kind==ActionKind.Died&&result.After.Player==d.coffin,"sunlit coffin admits a lethal step "+side);
            Require(result.After.Dead&&!result.After.Won&&!LightField.Compute(result.After).Safe(d.coffin),"coffin never overrides sunlight "+side);
            Require(!session.Apply(Command.Right).Changed,"death locks movement "+side);
            session.Apply(Command.Undo);Require(session.State.Key()==initial&&!session.State.Dead&&!session.State.Won,"undo lit-coffin death "+side);
            d.coffin=d.player+travel;session=new Session(new Definition(d));
            result=session.Apply((Command)Array.IndexOf(LightField.Directions,travel));
            Require(result.After.Won&&!result.After.Dead,"shaded coffin completes "+side);
        }
        var lamp=Room();lamp.player=new Cell(3,2);lamp.coffin=new Cell(4,2);lamp.pieces=new[]{new EntityData{id="lamp",kind=PieceKind.Lamp,cell=new Cell(4,4)}};
        var play=new Session(new Definition(lamp));var entered=play.Apply(Command.Right);
        Require(entered.Kind==ActionKind.Died&&!entered.After.Won,"cardinal lamp kills at coffin");
        play.Apply(Command.Reset);play.Apply(Command.Undo);Require(play.State.Dead&&!play.State.Won,"reset undo preserves fatal coffin snapshot");
        lamp.pieces=lamp.pieces.Concat(new[]{new EntityData{id="box",cell=new Cell(4,3)}}).ToArray();
        entered=new Session(new Definition(lamp)).Apply(Command.Right);Require(entered.After.Won&&!entered.After.Dead,"blocked cardinal lamp permits coffin");
        var reflected=Room();reflected.player=new Cell(3,1);reflected.coffin=new Cell(4,1);reflected.pieces=new[]{new EntityData{id="lamp",kind=PieceKind.Lamp,cell=new Cell(1,3)},new EntityData{id="mirror",kind=PieceKind.MirrorSlash,cell=new Cell(4,3)}};
        entered=new Session(new Definition(reflected)).Apply(Command.Right);Require(entered.Kind==ActionKind.Died&&!entered.After.Won,"reflected light kills at coffin");
        var overlap=Room();overlap.sunEnabled=true;overlap.player=new Cell(3,2);overlap.coffin=new Cell(4,2);overlap.pieces=new[]{new EntityData{id="sunshade",cell=new Cell(2,2)},new EntityData{id="lamp",kind=PieceKind.Lamp,cell=new Cell(4,4)}};
        entered=new Session(new Definition(overlap)).Apply(Command.Right);Require(entered.After.Dead&&!entered.After.Won,"sun shadow does not protect a lamp-lit coffin");
        overlap.pieces=overlap.pieces.Concat(new[]{new EntityData{id="lampshade",cell=new Cell(4,3)}}).ToArray();
        entered=new Session(new Definition(overlap)).Apply(Command.Right);Require(entered.After.Won&&!entered.After.Dead,"coffin completes only after every source is occluded");
        return checks;
    }

    public static void Audit()
    {
        var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
        var fixtures=JsonDocument.Parse(File.ReadAllText("Assets/DraculasBox/Content/Replays.json")).RootElement.GetProperty("levels");
        var records=new List<object>();
        foreach(var fixture in fixtures.EnumerateArray())
        {
            string id=fixture.GetProperty("id").GetString()!,route=fixture.GetProperty("route").GetString()!;
            var data=JsonSerializer.Deserialize<LevelData>(File.ReadAllText($"Assets/DraculasBox/Content/Levels/{id}.json"),options)!;
            var session=new Session(new Definition(data));int reached=0;
            foreach(char c in route){var move=session.Apply((Command)"URDL".IndexOf(c));if(!move.Changed)break;reached++;}
            var light=LightField.Compute(session.State);
            var solution=session.State.Won?null:LevelSearch.Solve(data);
            records.Add(new{id,replayWon=session.State.Won,reached,at=session.State.Player.ToString(),coffin=data.coffin.ToString(),sun=light.Sun[data.coffin.y*data.rows[0].Length+data.coffin.x],lamp=light.Lamp[data.coffin.y*data.rows[0].Length+data.coffin.x],solution});
        }
        Console.WriteLine(JsonSerializer.Serialize(records,options));
    }

    public static void Candidates(string[] ids)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        var fixtures=JsonDocument.Parse(File.ReadAllText("Assets/DraculasBox/Content/Replays.json")).RootElement.GetProperty("levels").EnumerateArray().ToDictionary(r=>r.GetProperty("id").GetString()!);
        foreach(string id in ids)
        {
            var data=JsonSerializer.Deserialize<LevelData>(File.ReadAllText($"Assets/DraculasBox/Content/Levels/{id}.json"),options)!;
            var essential=fixtures[id].GetProperty("essential").EnumerateArray().Select(p=>p.GetString()!).ToArray();
            var results=new List<(Cell Cell,LevelSearch.Result Solution)>();
            for(int y=0;y<data.rows.Length;y++)for(int x=0;x<data.rows[0].Length;x++)
            {
                var cell=new Cell(x,y);if(data.rows[y][x]!='.'||cell==data.player||data.pieces.Any(p=>p.cell==cell))continue;
                var draft=data.Copy();draft.coffin=cell;if(Definition.StructuralError(draft)!=null)continue;var solution=LevelSearch.Solve(draft);if(!solution.Solved)continue;
                if(essential.All(piece=>{var frozen=LevelSearch.Solve(draft,new HashSet<string>{piece});return !frozen.Solved&&frozen.Exhausted;}))results.Add((cell,solution));
            }
            foreach(var r in results.OrderBy(r=>Math.Abs(r.Cell.x-data.coffin.x)+Math.Abs(r.Cell.y-data.coffin.y)).ThenByDescending(r=>r.Solution.Moves).Take(8))
                Console.WriteLine(JsonSerializer.Serialize(new{id,coffin=r.Cell,solution=r.Solution},options));
        }
    }
}
