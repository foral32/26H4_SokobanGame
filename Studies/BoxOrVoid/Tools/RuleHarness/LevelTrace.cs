using System.Text.Json;
using BoxOrVoid.Core;

internal static class LevelTrace
{
    private static readonly JsonSerializerOptions Json=new(){IncludeFields=true,WriteIndented=true};
    public static int Run(string root,string[] args)
    {
        string content=Path.Combine(root,"Assets/Studies/BoxOrVoid/Content");
        string output=Path.Combine(root,"Studies/BoxOrVoid/Evidence/Traces");Directory.CreateDirectory(output);
        var ids=args.Length>0?new[]{args[0]}:Directory.GetFiles(Path.Combine(content,"Levels"),"*.json").Select(Path.GetFileNameWithoutExtension).ToArray();
        foreach(string id in ids)
        {
            var definition=PuzzleDefinition.FromData(JsonSerializer.Deserialize<LevelData>(File.ReadAllText(Path.Combine(content,"Levels",id+".json")),Json)!);
            string commands=args.Length>1?args[1]:JsonDocument.Parse(File.ReadAllText(Path.Combine(content,"Replays",id+".json"))).RootElement.GetProperty("commands").GetString()!;
            var session=new PuzzleSession(definition);var steps=new List<object>();steps.Add(Snapshot(0,'-',"Initial",session.State));
            int step=0;
            foreach(char c in commands)
            {var result=session.Apply((Command)"URDLSZX".IndexOf(c));steps.Add(Snapshot(++step,c,result.Kind.ToString(),session.State));}
            string suffix=args.Length>1?"_reference":"";
            File.WriteAllText(Path.Combine(output,id+suffix+".json"),JsonSerializer.Serialize(new{id,commands,solved=session.State.IsSolved,steps},Json));
            Console.WriteLine($"{id}: {commands}, solved={session.State.IsSolved}");
        }
        return 0;
    }
    private static object Snapshot(int step,char command,string result,PuzzleState state)=>new
    {
        step,command,result,active=state.ActiveTone.ToString(),
        pieces=Enumerable.Range(0,state.PieceCount).Select(i=>{var p=state.GetPiece(i);return new {i,kind=p.Kind.ToString(),tone=p.Tone.ToString(),x=p.Cell.X,y=p.Cell.Y,depth=p.Depth,top=state.TopAt(p.Cell)==i};}).ToArray(),
        goals=Enumerable.Range(0,state.Definition.Goals.Count).Select(i=>state.GoalFulfilled(i)).ToArray()
    };
}
