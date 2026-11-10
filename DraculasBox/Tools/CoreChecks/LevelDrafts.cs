using System.Text.Json;
using DraculasBox.Core;

internal static class LevelDrafts
{
    private static readonly JsonSerializerOptions Options=new(){IncludeFields=true,WriteIndented=true};
    public static void Run(string[] args)
    {
        var template=JsonSerializer.Deserialize<LevelData>(File.ReadAllText(args[1]),Options)!;
        string folder=args[2];Directory.CreateDirectory(folder);var random=new Random(int.Parse(args[3]));int wanted=int.Parse(args[4]),saved=0;
        var cells=Enumerable.Range(0,template.rows.Length).SelectMany(y=>Enumerable.Range(0,template.rows[0].Length).Select(x=>new Cell(x,y))).Where(p=>template.rows[p.y][p.x]=='.'&&p!=template.coffin).ToArray();
        for(int attempt=0;attempt<6000&&saved<wanted;attempt++)
        {
            var data=template.Copy();var selected=cells.OrderBy(_=>random.Next()).Take(data.pieces.Length+1).ToArray();data.player=selected[0];for(int i=0;i<data.pieces.Length;i++)data.pieces[i].cell=selected[i+1];
            try{new Definition(data);}catch(ArgumentException){continue;}
            var solution=LevelSearch.Solve(data,limit:18000);if(!solution.Solved||solution.Moves<(data.sunEnabled?12:16)||solution.Moves>34||solution.PushActions<5||solution.LampMoves<(data.sunEnabled?1:2))continue;
            var session=new Session(new Definition(data));var lampDirections=new HashSet<Command>();int lamp=Array.FindIndex(data.pieces,p=>p.kind==PieceKind.Lamp);
            foreach(char c in solution.Route){var move=session.Apply((Command)"URDL".IndexOf(c));if(move.Before.Position(lamp)!=move.After.Position(lamp))lampDirections.Add(move.Command);}
            if(lampDirections.Count<(data.sunEnabled?1:2))continue;
            bool necessary=true;foreach(var piece in data.pieces){var locked=LevelSearch.Solve(data,new HashSet<string>{piece.id},limit:18000);if(locked.Solved||!locked.Exhausted){necessary=false;break;}}
            if(!necessary)continue;
            string path=Path.Combine(folder,$"candidate_{++saved:00}.json");File.WriteAllText(path,JsonSerializer.Serialize(data,Options));
            Console.WriteLine(JsonSerializer.Serialize(new{path,attempt,solution}));
        }
        Console.WriteLine(JsonSerializer.Serialize(new{saved}));
    }
    public static void Trace(string[] args)
    {
        var data=JsonSerializer.Deserialize<LevelData>(File.ReadAllText(args[1]),Options)!;var session=new Session(new Definition(data));
        Console.WriteLine("Start "+session.State.Key());
        string route=args.Length>2?args[2]:LevelSearch.Solve(data).Route;int step=0;
        foreach(char c in route)
        {
            var move=session.Apply((Command)"URDL".IndexOf(c));step++;
            Console.WriteLine($"{step:00} {c} {move.Kind} {move.Reason} P:{move.After.Player} "+string.Join(" ",data.pieces.Select((p,i)=>p.id+":"+move.After.Position(i))));
        }
    }
}
