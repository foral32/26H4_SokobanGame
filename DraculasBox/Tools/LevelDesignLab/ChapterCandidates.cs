using System.Text.Json;
using DraculasBox.Core;

internal static class ChapterCandidates
{
    private static readonly JsonSerializerOptions Json=new(){IncludeFields=true,WriteIndented=true};
    private static LevelData Read(string path)=>JsonSerializer.Deserialize<LevelData>(File.ReadAllText(path),Json)!;
    private static LevelSearch.Result Solve(LevelData d,ISet<string> frozen=null,int limit=100000)
    {try{return LevelSearch.Solve(d,frozen,limit);}catch(ArgumentException){return new(false,true,"",0,0,0,0,0,0);}}
    private sealed record Node(State State,int Parent,char Move);
    private static int BendDepth(State s)
    {
        int maximum=0;var d=s.Level;
        for(int lamp=0;lamp<d.PieceCount;lamp++)if(d.Kind(lamp)==PieceKind.Lamp)for(int seed=0;seed<4;seed++)
        {
            var p=s.Position(lamp);int direction=seed;var seen=new HashSet<string>();var mirrors=new HashSet<int>();
            while(true)
            {
                p+=LightField.Directions[direction];if(!d.Inside(p)||d.Terrain(p)=='#'||s.ClosedDoor(p)||!seen.Add(p+":"+direction))break;
                int at=s.At(p);if(at<0)continue;var kind=d.Kind(at);
                if(kind!=PieceKind.MirrorSlash&&kind!=PieceKind.MirrorBackslash)break;
                mirrors.Add(at);maximum=Math.Max(maximum,mirrors.Count);direction=kind==PieceKind.MirrorSlash?direction^1:3-direction;
            }
        }
        return maximum;
    }
    public sealed record Bounded(bool Solved,bool Exhausted,int Visited,string Route);
    public static Bounded Constrain(LevelData d,string condition,int limit=100000)
    {
        var def=new Definition(d);var initial=def.Initial();var nodes=new List<Node>{new(initial,-1,' ')};var seen=new HashSet<string>{initial.Key()};int head=0,goal=-1;
        while(head<nodes.Count&&nodes.Count<limit&&goal<0)
        {
            var before=nodes[head].State;
            for(int direction=0;direction<4;direction++)
            {
                var a=Rules.Move(before,(Command)direction);if(!a.Changed||a.After.Dead)continue;var after=a.After;
                var moved=Enumerable.Range(0,d.pieces.Length).Where(i=>before.Position(i)!=after.Position(i)).ToArray();
                bool reject=false;
                if(condition=="no-close")reject=Enumerable.Range(0,def.DoorCount).Any(i=>before.DoorOpen(i)&&!after.DoorOpen(i));
                if(condition=="no-open")reject=Enumerable.Range(0,def.DoorCount).Any(i=>!before.DoorOpen(i)&&after.DoorOpen(i));
                if(condition=="no-held")reject=Enumerable.Range(0,def.DoorCount).Any(i=>after.DoorOpen(i)&&!Enumerable.Range(0,def.PlateCount).Any(p=>def.PlateChannel(p)==def.DoorChannel(i)&&after.PlatePressed(p)));
                if(condition=="no-held-release")reject=Enumerable.Range(0,def.DoorCount).Any(i=>after.DoorOpen(i)&&Enumerable.Range(0,def.PlateCount).Any(p=>def.PlateChannel(p)==def.DoorChannel(i)&&before.PlatePressed(p))&&!Enumerable.Range(0,def.PlateCount).Any(p=>def.PlateChannel(p)==def.DoorChannel(i)&&after.PlatePressed(p)));
                if(condition=="no-weight-release")reject=moved.Any(i=>d.plates.Any(p=>p.cell==before.Position(i)));
                if(condition=="no-lamp-plate")reject=Enumerable.Range(0,d.pieces.Length).Any(i=>d.pieces[i].kind==PieceKind.Lamp&&d.plates.Any(p=>p.cell==after.Position(i)));
                if(condition=="single")reject=moved.Length>1;
                var parts=condition.Split(':');if(parts.Length==2&&parts[0]=="freeze")reject=moved.Any(i=>d.pieces[i].id==parts[1]);
                if(parts.Length==2&&parts[0]=="no-close-door"){int i=int.Parse(parts[1]);reject=before.DoorOpen(i)&&!after.DoorOpen(i);}
                if(parts.Length==2&&parts[0]=="no-press")reject=Enumerable.Range(0,def.PlateCount).Any(i=>def.PlateChannel(i)==int.Parse(parts[1])&&after.PlatePressed(i));
                if(parts.Length==2&&parts[0]!="freeze"&&parts[0]!="no-press"&&parts[0]!="no-close-door")reject=moved.Any(i=>d.pieces[i].id==parts[0])&&parts[1].Contains("URDL"[direction]);
                if(reject||!seen.Add(after.Key()))continue;
                nodes.Add(new(after,head,"URDL"[direction]));if(after.Won){goal=nodes.Count-1;break;}
            }
            head++;
        }
        var path=new List<char>();if(goal>=0)for(int i=goal;nodes[i].Parent>=0;i=nodes[i].Parent)path.Add(nodes[i].Move);path.Reverse();
        return new(goal>=0,head>=nodes.Count,nodes.Count,new string(path.ToArray()));
    }
    public static object Describe(LevelData d,LevelSearch.Result result)
    {
        var s=new Session(new Definition(d));var trace=new List<object>();int toggles=0,held=0,releases=0,lampPlate=0,maxMirrors=0,maxBendDepth=BendDepth(s.State);
        var directions=d.pieces.Select(_=>new List<char>()).ToArray();
        for(int step=0;step<result.Route.Length;step++)
        {
            var a=s.Apply((Command)"URDL".IndexOf(result.Route[step]));var moved=Enumerable.Range(0,d.pieces.Length).Where(i=>a.Before.Position(i)!=a.After.Position(i)).ToArray();
            var changed=Enumerable.Range(0,d.doors.Length).Where(i=>a.Before.DoorOpen(i)!=a.After.DoorOpen(i)).Select(i=>i+":"+(a.After.DoorOpen(i)?"open":"closed")).ToArray();toggles+=changed.Length;
            foreach(var i in moved)directions[i].Add(result.Route[step]);
            releases+=moved.Count(i=>d.plates.Any(p=>p.cell==a.Before.Position(i)));
            held+=Enumerable.Range(0,d.doors.Length).Count(i=>a.After.DoorOpen(i)&&!d.plates.Any(p=>p.channel==d.doors[i].channel&&(a.After.Player==p.cell||a.After.At(p.cell)>=0)));
            lampPlate+=Enumerable.Range(0,d.pieces.Length).Count(i=>d.pieces[i].kind==PieceKind.Lamp&&d.plates.Any(p=>p.cell==a.After.Position(i)));
            maxMirrors=Math.Max(maxMirrors,LightField.Compute(a.After).ReflectionEdges.Count(p=>p!=0));
            maxBendDepth=Math.Max(maxBendDepth,BendDepth(a.After));
            if(moved.Length>0||changed.Length>0)trace.Add(new{step=step+1,key=result.Route[step],player=a.After.Player.ToString(),moved=moved.Select(i=>d.pieces[i].id+":"+a.Before.Position(i)+">"+a.After.Position(i)).ToArray(),doors=changed});
        }
        return new{result,toggles,held,releases,lampPlate,maxMirrors,maxBendDepth,signature=d.pieces.Select((p,i)=>p.id+":"+new string(directions[i].ToArray())).ToArray(),trace};
    }
    public static void Assess(string[] args)
    {
        var reports=new List<object>();foreach(string file in args.Skip(1))
        {
            var d=Read(file);var result=Solve(d,limit:500000);var locks=d.pieces.ToDictionary(p=>p.id,p=>Solve(d,new HashSet<string>{p.id},300000));
            var boxes=d.Copy();foreach(var p in boxes.pieces)if((int)p.kind>=2)p.kind=PieceKind.Box;
            var allBox=Solve(boxes,limit:300000);
            var missingDoor=d.Copy();missingDoor.plates=Array.Empty<DeviceData>();missingDoor.doors=Array.Empty<DeviceData>();var doorless=Solve(missingDoor,limit:300000);
            var flatLocks=d.pieces.Where(p=>(int)p.kind>=2).ToDictionary(p=>p.id,p=>Solve(boxes,new HashSet<string>{p.id},300000));
            reports.Add(new{file,d.id,d.title,description=result.Solved?Describe(d,result):null,result,locks,allBox,flatLocks,doorless});
        }
        File.WriteAllText(args[0],JsonSerializer.Serialize(reports,Json));Console.WriteLine("Assessed "+reports.Count+" drafts -> "+args[0]);
    }
    public static void Proof(string[] args)
    {Console.WriteLine(JsonSerializer.Serialize(Constrain(Read(args[0]),args[1],500000),Json));}
    public static void Sample(string[] args)
    {
        var template=Read(args[0]);string folder=args[1],mode=args[2];int attempts=int.Parse(args[3]);var random=new Random(int.Parse(args[4]));Directory.CreateDirectory(folder);int saved=0;
        var devices=template.plates.Select(p=>p.cell).Concat(template.doors.Select(p=>p.cell)).ToHashSet();
        var cells=Enumerable.Range(0,template.rows.Length).SelectMany(y=>Enumerable.Range(0,template.rows[0].Length).Select(x=>new Cell(x,y))).Where(p=>template.rows[p.y][p.x]=='.'&&p!=template.coffin).ToArray();
        for(int trial=0;trial<attempts&&saved<10;trial++)
        {
            var d=template.Copy();var spots=cells.OrderBy(_=>random.Next()).Take(d.pieces.Length+1).ToArray();d.player=spots[0];for(int i=0;i<d.pieces.Length;i++)d.pieces[i].cell=spots[i+1];
            if(mode=="lamp-plate"&&d.pieces.Any(p=>p.kind==PieceKind.Lamp&&d.plates.Any(q=>q.cell==p.cell)))continue;
            Definition def;try{def=new Definition(d);}catch(ArgumentException){continue;}
            var result=Solve(d,limit:20000);if(!result.Solved||result.Moves<12||result.Moves>42||result.PushActions<4)continue;
            var s=new Session(def);int close=0,open=0,release=0,lampPlate=0,lampEntries=0,maxMirrors=0,maxBendDepth=BendDepth(s.State);bool cross=LightField.Compute(s.State).ReflectionEdges.Any(b=>b==15);
            foreach(char key in result.Route)
            {
                var a=s.Apply((Command)"URDL".IndexOf(key));
                close+=Enumerable.Range(0,def.DoorCount).Count(i=>a.Before.DoorOpen(i)&&!a.After.DoorOpen(i));
                open+=Enumerable.Range(0,def.DoorCount).Count(i=>!a.Before.DoorOpen(i)&&a.After.DoorOpen(i));
                release+=Enumerable.Range(0,d.pieces.Length).Count(i=>a.Before.Position(i)!=a.After.Position(i)&&d.plates.Any(p=>p.cell==a.Before.Position(i)));
                lampPlate+=Enumerable.Range(0,d.pieces.Length).Count(i=>d.pieces[i].kind==PieceKind.Lamp&&d.plates.Any(p=>p.cell==a.After.Position(i)));
                lampEntries+=Enumerable.Range(0,d.pieces.Length).Count(i=>d.pieces[i].kind==PieceKind.Lamp&&a.Before.Position(i)!=a.After.Position(i)&&d.plates.Any(p=>p.cell==a.After.Position(i)));
                maxMirrors=Math.Max(maxMirrors,LightField.Compute(a.After).ReflectionEdges.Count(b=>b!=0));
                maxBendDepth=Math.Max(maxBendDepth,BendDepth(a.After));
                cross|=LightField.Compute(a.After).ReflectionEdges.Any(b=>b==15);
            }
            if(mode=="close"&&(close==0||open==0)||mode=="release"&&release==0||mode=="lamp-plate"&&lampEntries==0||mode=="double-bend"&&maxBendDepth<2)continue;
            if(mode=="cross"&&!cross)continue;
            bool essential=true;foreach(var p in d.pieces.Where(p=>p.kind!=PieceKind.Lamp))
            {var frozen=Solve(d,new HashSet<string>{p.id},20000);if(frozen.Solved||!frozen.Exhausted){essential=false;break;}}
            if(!essential)continue;
            if(d.pieces.Any(p=>(int)p.kind>=2))
            {
                var boxes=d.Copy();foreach(var p in boxes.pieces)if((int)p.kind>=2)p.kind=PieceKind.Box;
                var flat=Solve(boxes,limit:22000);if(!flat.Solved||flat.Moves>result.Moves-4)continue;
            }
            if(mode=="channels")foreach(int channel in d.plates.Select(p=>p.channel).Distinct())
            {var proof=Constrain(d,"no-press:"+channel,22000);if(proof.Solved||!proof.Exhausted){essential=false;break;}}
            if(!essential)continue;
            if(mode=="close"||mode=="release"||mode=="lamp-plate"||mode=="held")
            {
                string condition=mode=="close"?"no-close":mode=="release"?"no-weight-release":mode=="held"?"no-held":"no-lamp-plate";
                var proof=Constrain(d,condition,22000);if(proof.Solved||!proof.Exhausted)continue;
            }
            string path=Path.Combine(folder,$"pool_{++saved:00}.json");d.solution=result.Route;File.WriteAllText(path,JsonSerializer.Serialize(d,Json));
            Console.WriteLine(JsonSerializer.Serialize(new{path,trial,moves=result.Moves,pushes=result.PushActions,close,open,release,lampPlate,maxMirrors,maxBendDepth,route=result.Route}));
        }
        Console.WriteLine("Saved "+saved+" "+mode+" samples");
    }
}
