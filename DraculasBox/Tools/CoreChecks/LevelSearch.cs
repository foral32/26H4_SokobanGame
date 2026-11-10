using System.Diagnostics;
using System.Text.Json;
using DraculasBox.Core;

internal static class LevelSearch
{
    private sealed record Node(State State,int Parent,char Move);
    public sealed record Result(bool Solved,bool Exhausted,string Route,int Moves,int PushActions,int EntityShifts,int LampMoves,int Visited,double Seconds);
    public static Result Solve(LevelData data,ISet<string> frozen=null,int limit=350000)
    {
        var clock=Stopwatch.StartNew();var definition=new Definition(data);var initial=definition.Initial();
        var fixedIndices=Enumerable.Range(0,data.pieces.Length).Where(i=>frozen?.Contains(data.pieces[i].id)==true).ToArray();
        var nodes=new List<Node>{new(initial,-1,' ')};var seen=new HashSet<string>{initial.Key()};
        int at=0,goal=-1;
        while(at<nodes.Count&&nodes.Count<limit)
        {
            var current=nodes[at].State;
            for(int d=0;d<4;d++)
            {
                var action=Rules.Move(current,(Command)d);if(!action.Changed||action.After.Dead||fixedIndices.Any(i=>action.After.Position(i)!=initial.Position(i)))continue;
                if(!seen.Add(action.After.Key()))continue;
                nodes.Add(new Node(action.After,at,"URDL"[d]));
                if(action.After.Won){goal=nodes.Count-1;break;}
            }
            if(goal>=0)break;at++;
        }
        if(goal<0)return new(false,at>=nodes.Count,"",0,0,0,0,nodes.Count,clock.Elapsed.TotalSeconds);
        var route=new List<char>();for(int i=goal;nodes[i].Parent>=0;i=nodes[i].Parent)route.Add(nodes[i].Move);route.Reverse();
        var session=new Session(definition);int pushes=0,shifts=0,lamps=0;
        foreach(char key in route)
        {
            var result=session.Apply((Command)"URDL".IndexOf(key));
            if(result.Kind==ActionKind.Pushed)pushes++;shifts+=result.PushedCount;
            for(int i=0;i<data.pieces.Length;i++)if(data.pieces[i].kind==PieceKind.Lamp&&result.Before.Position(i)!=result.After.Position(i))lamps++;
        }
        return new(true,false,new string(route.ToArray()),route.Count,pushes,shifts,lamps,nodes.Count,clock.Elapsed.TotalSeconds);
    }
    public static void Run(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
        var data=JsonSerializer.Deserialize<LevelData>(File.ReadAllText(args[1]),options)!;
        var frozen=args.Skip(2).ToHashSet();Console.WriteLine(JsonSerializer.Serialize(Solve(data,frozen),options));
    }
}
