using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BoxOrVoid.Core;

internal static class LevelSolver
{
    private sealed record Node(PuzzleState State, int Parent, Command Move, int Steps);
    private static readonly JsonSerializerOptions Json = new() { IncludeFields=true, WriteIndented=true };

    public static int Run(string root, string[] ids)
    {
        string assets=Path.Combine(root,"Assets/Studies/BoxOrVoid/Content");
        string output=Path.Combine(root,"Studies/BoxOrVoid/Evidence/Replays");
        Directory.CreateDirectory(output);
        int failed=0;
        foreach(string id in ids)
        {
            var definition=PuzzleDefinition.FromData(JsonSerializer.Deserialize<LevelData>(File.ReadAllText(Path.Combine(assets,"Levels",id+".json")),Json)!);
            var watch=Stopwatch.StartNew();
            var nodes=new List<Node> { new(definition.CreateInitialState(),-1,Command.Up,0) };
            var costs=new Dictionary<string,int> { [Key(nodes[0].State)]=0 };
            var queue=new PriorityQueue<int,float>();queue.Enqueue(0,0);
            int solved=-1;
            while(queue.TryDequeue(out int index,out _) && nodes.Count<1_500_000)
            {
                var node=nodes[index];
                if(node.State.IsSolved) { solved=index;break; }
                if(costs[Key(node.State)]<node.Steps) continue;
                for(int c=0;c<5;c++)
                {
                    var action=PuzzleRules.Apply(node.State,(Command)c);
                    if(!action.Changed || !Inside(action.After)) continue;
                    string key=Key(action.After);int steps=node.Steps+1;
                    if(costs.TryGetValue(key,out int old) && old<=steps) continue;
                    costs[key]=steps;
                    int next=nodes.Count;nodes.Add(new(action.After,index,(Command)c,steps));
                    queue.Enqueue(next,steps+4*Distance(action.After));
                }
            }
            if(solved<0) { failed++;Console.WriteLine($"UNSOLVED {id}: {nodes.Count} states, {watch.Elapsed.TotalSeconds:F1}s");continue; }
            var commands=new List<Command>();
            for(int i=solved;nodes[i].Parent>=0;i=nodes[i].Parent) commands.Add(nodes[i].Move);
            commands.Reverse();
            Validate(definition,commands);
            string encoded=new(commands.Select(c=>"URDLS"[(int)c]).ToArray());
            File.WriteAllText(Path.Combine(output,id+".json"),JsonSerializer.Serialize(new {id,commands=encoded,steps=commands.Count,visited=nodes.Count,seconds=watch.Elapsed.TotalSeconds,solved=true,undoRoundTrip=true},Json));
            Console.WriteLine($"SOLVED {id}: {commands.Count} actions, {nodes.Count} states, {watch.Elapsed.TotalSeconds:F2}s");
        }
        return failed==0?0:1;
    }

    private static void Validate(PuzzleDefinition definition,List<Command> commands)
    {
        var session=new PuzzleSession(definition);
        string initial=session.State.PositionKey();
        foreach(var command in commands) if(!session.Apply(command).Changed) throw new Exception("Replay contains a rejected command");
        if(!session.State.IsSolved) throw new Exception("Replay does not solve the level");
        string final=session.State.PositionKey();
        session.Apply(Command.Reset);session.Apply(Command.Undo);
        if(session.State.PositionKey()!=final) throw new Exception("Reset/undo lost solved state");
        foreach(var command in commands) session.Apply(Command.Undo);
        if(session.State.PositionKey()!=initial) throw new Exception("Undo did not restore initial board");
    }

    private static bool Inside(PuzzleState state)
    {
        for(int i=0;i<state.PieceCount;i++)
        { var p=state.GetPiece(i).Cell;if(p.X<0 || p.Y<0 || p.X>=state.Definition.Width || p.Y>=state.Definition.Height) return false; }
        return true;
    }

    // Equal-colored boxes are interchangeable. Relative stack order is significant.
    private static string Key(PuzzleState state)
    {
        var tokens=new string[state.PieceCount];
        for(int i=0;i<tokens.Length;i++)
        {
            var p=state.GetPiece(i);int rank=0;
            for(int j=0;j<tokens.Length;j++) if(state.GetPiece(j).Cell==p.Cell && state.GetPiece(j).Depth<p.Depth) rank++;
            tokens[i]=$"{(int)p.Kind},{(int)p.Tone},{p.Cell.X},{p.Cell.Y},{rank}";
        }
        Array.Sort(tokens,StringComparer.Ordinal);
        return (int)state.ActiveTone+"|"+string.Join(';',tokens);
    }

    // Search ordering only, not a claim of shortest solutions. Rules remain in the shared core.
    private static float Distance(PuzzleState state)
    {
        float sum=0;
        for(int g=0;g<state.Definition.Goals.Count;g++)
        {
            if(state.GoalFulfilled(g)) continue;
            var goal=state.Definition.Goals[g];int best=100;
            for(int i=0;i<state.PieceCount;i++)
            {
                var p=state.GetPiece(i);
                if(p.Kind==goal.RequiredKind && p.Tone==goal.Tone)
                    best=Math.Min(best,Math.Abs(p.Cell.X-goal.Cell.X)+Math.Abs(p.Cell.Y-goal.Cell.Y));
            }
            sum+=best==0?2:best;
        }
        return sum;
    }
}
