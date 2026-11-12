using System.Text.Json;
using DraculasBox.Core;

var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
LevelData Read(string path)=>JsonSerializer.Deserialize<LevelData>(File.ReadAllText(path),options)!;
object Analyze(LevelData data,bool testLocks=true)
{
    var result=LevelSearch.Solve(data,limit:500000);
    if(!result.Solved)return new{data.id,data.title,result};
    var session=new Session(new Definition(data));
    var pushes=new List<object>();var directions=data.pieces.Select(_=>new List<string>()).ToArray();
    int step=0;
    foreach(char key in result.Route)
    {
        step++;var move=session.Apply((Command)"URDL".IndexOf(key));
        var moved=Enumerable.Range(0,data.pieces.Length).Where(i=>move.Before.Position(i)!=move.After.Position(i)).ToArray();
        if(moved.Length>0)pushes.Add(new{step,key,player=move.After.Player.ToString(),moved=moved.Select(i=>data.pieces[i].id+":"+move.Before.Position(i)+">"+move.After.Position(i)).ToArray()});
        foreach(int i in moved)directions[i].Add(key.ToString());
    }
    var locks=new Dictionary<string,object>();
    if(testLocks)foreach(var p in data.pieces)
    {var locked=LevelSearch.Solve(data,new HashSet<string>{p.id},limit:500000);locks[p.id]=new{locked.Solved,locked.Exhausted,locked.Moves,locked.Visited};}
    return new{data.id,data.title,result,pushes,signature=data.pieces.Select((p,i)=>p.id+":"+string.Concat(directions[i])).ToArray(),locks};
}
if(args[0]=="chapter-sample"){ChapterCandidates.Sample(args.Skip(1).ToArray());return;}
if(args[0]=="chapter-assess"){ChapterCandidates.Assess(args.Skip(1).ToArray());return;}
if(args[0]=="chapter-proof"){ChapterCandidates.Proof(args.Skip(1).ToArray());return;}
if(args[0]=="audit")
{
    var catalog=JsonDocument.Parse(File.ReadAllText("Assets/DraculasBox/Content/Chapters.json"));
    var reports=catalog.RootElement.GetProperty("chapters").EnumerateArray().Take(2).SelectMany(c=>c.GetProperty("levels").EnumerateArray()).Select(l=>l.GetProperty("id").GetString()!).Select(id=>Analyze(Read("Assets/DraculasBox/Content/Levels/"+id+".json"))).ToArray();
    File.WriteAllText(args[1],JsonSerializer.Serialize(reports,options));Console.WriteLine("Audited "+reports.Length+" campaign levels");
}
else if(args[0]=="analyze")
{
    var reports=args.Skip(2).Select(path=>Analyze(Read(path))).ToArray();
    File.WriteAllText(args[1],JsonSerializer.Serialize(reports,options));Console.WriteLine(JsonSerializer.Serialize(reports,options));
}
else if(args[0]=="sweep")
{
    // Layouts are authored. Placement sampling supplies material to inspect and revise;
    // solver length is only a filter, never a verdict on puzzle quality.
    var template=Read(args[1]);string folder=args[2],mode=args[3];int attempts=int.Parse(args[4]);
    Directory.CreateDirectory(folder);var random=new Random(int.Parse(args[5]));int saved=0;
    var cells=Enumerable.Range(0,template.rows.Length).SelectMany(y=>Enumerable.Range(0,template.rows[0].Length).Select(x=>new Cell(x,y))).Where(p=>template.rows[p.y][p.x]=='.'&&p!=template.coffin).ToArray();
    for(int attempt=0;attempt<attempts&&saved<12;attempt++)
    {
        var data=template.Copy();var selected=cells.OrderBy(_=>random.Next()).Take(data.pieces.Length+1).ToArray();data.player=selected[0];for(int i=0;i<data.pieces.Length;i++)data.pieces[i].cell=selected[i+1];
        try{new Definition(data);}catch(ArgumentException){continue;}
        var solution=LevelSearch.Solve(data,limit:22000);if(!solution.Solved||solution.Moves<15||solution.Moves>48||solution.PushActions<5)continue;
        var session=new Session(new Definition(data));var shifts=data.pieces.Select(_=>new List<int>()).ToArray();
        foreach(char c in solution.Route){var r=session.Apply((Command)"URDL".IndexOf(c));for(int i=0;i<shifts.Length;i++)if(r.Before.Position(i)!=r.After.Position(i))shifts[i].Add((int)r.Command);}
        var lamps=Enumerable.Range(0,shifts.Length).Where(i=>data.pieces[i].kind==PieceKind.Lamp).ToArray();
        bool reversal=shifts.Where((p,i)=>data.pieces[i].kind==PieceKind.Box).Any(p=>p.Any(d=>p.Contains((d+2)%4)));
        bool elbow=lamps.Any(i=>shifts[i].Select(d=>d%2).Distinct().Count()==2);
        if(mode=="reuse"&&!reversal||mode=="elbow"&&!elbow||mode=="pair"&&lamps.Any(i=>shifts[i].Count==0)||mode=="shutters"&&lamps.Any(i=>shifts[i].Count>0))continue;
        bool necessary=true;foreach(var piece in data.pieces.Where(p=>mode!="shutters"||p.kind==PieceKind.Box))
        {var locked=LevelSearch.Solve(data,new HashSet<string>{piece.id},limit:22000);if(locked.Solved||!locked.Exhausted){necessary=false;break;}}
        if(!necessary)continue;
        string path=Path.Combine(folder,$"pool_{++saved:00}.json");File.WriteAllText(path,JsonSerializer.Serialize(data,options));
        Console.WriteLine(JsonSerializer.Serialize(new{path,attempt,solution,signature=data.pieces.Select((p,i)=>p.id+":"+string.Concat(shifts[i].Select(d=>"URDL"[d]))).ToArray()}));
    }
    Console.WriteLine(JsonSerializer.Serialize(new{saved,mode}));
}
else if(args[0]=="constraints")
{
    var data=Read(args[1]);var definition=new Definition(data);var initial=definition.Initial();
    string condition=args[2];int limit=500000;
    var nodes=new List<(State state,int parent,char move)>{(initial,-1,' ')};var seen=new HashSet<string>{initial.Key()};
    int head=0,goal=-1;
    for(;head<nodes.Count&&nodes.Count<limit&&goal<0;head++)for(int d=0;d<4;d++)
    {
        var move=Rules.Move(nodes[head].state,(Command)d);if(!move.Changed||move.After.Dead)continue;
        var shifted=Enumerable.Range(0,data.pieces.Length).Where(i=>move.Before.Position(i)!=move.After.Position(i)).ToArray();
        if(condition=="single"&&shifted.Length>1)continue;
        if(condition=="no-convoy"&&shifted.Count(i=>data.pieces[i].kind==PieceKind.Lamp)>1)continue;
        var parts=condition.Split(':');
        if(parts.Length==2&&shifted.Any(i=>data.pieces[i].id==parts[0])&&parts[1].Contains("URDL"[d]))continue;
        if(!seen.Add(move.After.Key()))continue;
        nodes.Add((move.After,head,"URDL"[d]));if(move.After.Won){goal=nodes.Count-1;break;}
    }
    var route=new List<char>();if(goal>=0)for(int i=goal;nodes[i].parent>=0;i=nodes[i].parent)route.Add(nodes[i].move);route.Reverse();
    Console.WriteLine(JsonSerializer.Serialize(new{condition,solved=goal>=0,exhausted=head>=nodes.Count,visited=nodes.Count,route=new string(route.ToArray())},options));
}
else if(args[0]=="trace")
{
    var data=Read(args[1]);var s=new Session(new Definition(data));string route=args.Length>2?args[2]:LevelSearch.Solve(data).Route;
    void Print()
    {
        var field=LightField.Compute(s.State);for(int y=0;y<data.rows.Length;y++){for(int x=0;x<data.rows[0].Length;x++){var p=new Cell(x,y);int i=s.State.At(p),gate=s.State.Level.DoorAt(p);char c=s.State.Player==p?'@':i>=0?(data.pieces[i].kind==PieceKind.Lamp?'L':data.pieces[i].kind==PieceKind.MirrorSlash?'/':data.pieces[i].kind==PieceKind.MirrorBackslash?'\\':data.pieces[i].id[0]):p==data.coffin?'X':gate>=0?(s.State.DoorOpen(gate)?'g':'G'):data.plates.Any(t=>t.cell==p)?'p':data.rows[y][x]=='.'?(field.Lit(p)?'!':'.'):data.rows[y][x];Console.Write(c);}Console.WriteLine();}
    }
    Print();int at=0;foreach(char c in route){var r=s.Apply((Command)"URDL".IndexOf(c));at++;Console.WriteLine($"{at} {c} {r.Kind} {r.Reason}");if(r.PushedCount>0||r.After.Won)Print();}
}
