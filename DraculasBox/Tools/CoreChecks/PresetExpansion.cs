using System.Text.Json;
using DraculasBox.Core;

// Offline authoring aid only: shortlist variants, then review rooms before inclusion.
internal static class PresetExpansion
{
    static readonly JsonSerializerOptions Json=new(){IncludeFields=true,WriteIndented=true};
    public static int Mask(LevelData b)=>(b.sunEnabled?1:0)|(b.pieces.Any(p=>p.kind==PieceKind.Lamp)?2:0)|(b.doors.Length>0?4:0)|(b.pieces.Any(p=>(int)p.kind>=2)?8:0);
    static bool Affects(LevelData b,Action<LevelData> remove)
    {
        var v=b.Copy();remove(v);if(new Definition(v,false).Initial().Dead)return true;var r=LevelSearch.Solve(v,limit:12000);
        return r.Solved&&!LevelSolver.Proves(b,r.Route,out _);
    }
    public static object Inspect(LevelData b)
    {
        var r=LevelSearch.Solve(b,limit:40000);
        if(!r.Solved||r.PushActions==0||r.Moves<4||r.Moves>65)return null;
        if(LevelSearch.Solve(b,b.pieces.Select(p=>p.id).ToHashSet(),5000).Solved)return null;
        int mask=Mask(b);
        bool sunChanges=(mask&1)!=0&&Affects(b,v=>v.sunEnabled=false);


        if((mask&8)!=0&&!Affects(b,v=>{foreach(var p in v.pieces)if((int)p.kind>=2)p.kind=PieceKind.Box;}))return null;
        var s=new Session(new Definition(b));int mirrors=0,doors=0,shade=0,sunThreat=0,lampThreat=0;
        foreach(char key in r.Route)
        {
            var a=s.Apply((Command)"URDL".IndexOf(key));
            foreach(var cmd in new[]{Command.Up,Command.Right,Command.Down,Command.Left}){var next=Rules.Move(a.Before,cmd);if(next.Kind==ActionKind.Died){var light=LightField.Compute(next.After);int at=next.After.Player.y*next.After.Level.Width+next.After.Player.x;if(light.Sun[at]&&!light.Lamp[at])sunThreat++;if(light.Lamp[at])lampThreat++;}}
            if(a.After.Level.DoorAt(a.After.Player)>=0)doors++;
            for(int i=0;i<b.pieces.Length;i++)if((int)b.pieces[i].kind>=2&&a.Before.Position(i)!=a.After.Position(i)&&LightField.Compute(a.After).ReflectionEdges[a.After.Position(i).y*a.After.Level.Width+a.After.Position(i).x]!=0)mirrors++;
            if(b.sunEnabled){var d=LightField.SunTravel[(int)b.sunFrom];var back=new Cell(-d.x,-d.y);var one=a.After.Player+back;var two=one+back;if(a.After.Level.Terrain(one)!='#'&&a.After.Level.Terrain(two)!='#'&&(a.After.At(one)>=0||a.After.At(two)>=0))shade++;}
        }
        if(((mask&4)!=0&&doors==0)||((mask&8)!=0&&mirrors==0))return null;
        if((mask&2)!=0&&lampThreat==0)return null;
        if((mask&1)!=0&&!sunChanges&&sunThreat==0)return null;
        b.solution=r.Route;
        return new{mask,steps=r.Moves,pushes=r.PushActions,mirrors,doors,shade,sunThreat,lampThreat,sunChanges,board=b};
    }
    public static void Run(string[] args)
    {
        if(args.Length>0&&args[0]=="finalize"){FinalizeSelection();return;}
        var bases=DraftPresets.All.Select(p=>p.Create()).ToList();
        foreach(var file in Directory.GetFiles("Assets/DraculasBox/Content/Levels","*.json"))
        {try{var b=JsonSerializer.Deserialize<LevelData>(File.ReadAllText(file),Json);if(b.rows.Length<=10&&b.rows[0].Length<=10)bases.Add(b);}catch{}}
        if(args.Length>0&&args[0]=="sun") { SunRooms(bases);return; }
        if(args.Length>0&&args[0]=="partition") { Partitions(bases);return; }
        if(args.Length>0&&args[0]=="gates") { Gates(bases);return; }
        if(args.Length>0&&args[0]=="boxes") {
            var outputBoxes=new List<object>();foreach(var source in bases){var b=source.Copy();b.sunEnabled=false;foreach(var p in b.pieces)p.kind=PieceKind.Box;
                b.pieces=b.pieces.Concat(b.doors.Select((d,i)=>new EntityData{id="gatebox"+i,cell=d.cell})).ToArray();b.doors=Array.Empty<DeviceData>();b.plates=Array.Empty<DeviceData>();
                try{var r=Inspect(b);if(r!=null)outputBoxes.Add(r);}catch{}
            }File.WriteAllText("DraculasBox/Evidence/DraftPresets/box-candidates.json",JsonSerializer.Serialize(outputBoxes,Json));return;
        }
        var output=new List<object>();int index=0;
        foreach(var source in bases)
        for(int sun=-1;sun<4;sun++)
        {
            var b=source.Copy();b.sunEnabled=sun>=0;b.sunFrom=(SunSide)Math.Max(0,sun);b.id="candidate_"+(index++);
            try{var result=Inspect(b);if(result!=null){output.Add(result);Console.WriteLine(b.id+" "+b.title+" mask="+Mask(b)+" route="+b.solution);}}catch{}
        }
        File.WriteAllText("DraculasBox/Evidence/DraftPresets/expansion-candidates.json",JsonSerializer.Serialize(output,Json));
        Console.WriteLine("Candidates "+output.Count);
    }

    static void FinalizeSelection()
    {
        var output=new List<object>();
        foreach(var item in JsonDocument.Parse(File.ReadAllText("DraculasBox/Evidence/DraftPresets/selected-expansion.json")).RootElement.EnumerateArray())
        {
            var b=JsonSerializer.Deserialize<LevelData>(item.GetProperty("board").GetRawText(),Json);var original=LevelSearch.Solve(b,limit:90000);
            var session=new Session(new Definition(b));var moved=new HashSet<string>();foreach(char k in original.Route){var a=session.Apply((Command)"URDL".IndexOf(k));for(int i=0;i<b.pieces.Length;i++)if(a.Before.Position(i)!=a.After.Position(i))moved.Add(b.pieces[i].id);}
            foreach(var p in b.pieces.Where(p=>p.kind==PieceKind.Box&&!moved.Contains(p.id)).ToArray())
            {var v=b.Copy();v.pieces=v.pieces.Where(e=>e.id!=p.id).ToArray();try{var r=LevelSearch.Solve(v,limit:90000);if(r.Solved&&r.Moves==original.Moves&&Inspect(v)!=null)b=v;}catch{}}
            var reachable=new HashSet<Cell>{b.player};var queue=new Queue<Cell>();queue.Enqueue(b.player);
            while(queue.Count>0){var p=queue.Dequeue();foreach(var d in LightField.Directions){var n=p+d;if(n.x>=0&&n.y>=0&&n.y<b.rows.Length&&n.x<b.rows[0].Length&&b.rows[n.y][n.x]=='.'&&reachable.Add(n))queue.Enqueue(n);}}
            var trim=b.Copy();for(int y=0;y<b.rows.Length;y++)for(int x=0;x<b.rows[0].Length;x++)if(b.rows[y][x]=='.'&&!reachable.Contains(new Cell(x,y))){var row=trim.rows[y].ToCharArray();row[x]='#';trim.rows[y]=new string(row);}
            try{var r=LevelSearch.Solve(trim,limit:90000);if(r.Solved&&r.Moves==original.Moves&&Inspect(trim)!=null)b=trim;}catch{}
            b.solution=LevelSearch.Solve(b,limit:90000).Route;b.title=item.GetProperty("title").GetString();
            output.Add(new{source=item.GetProperty("source").GetString(),grade=item.GetProperty("grade").GetInt32(),mask=Mask(b),board=b});Console.WriteLine(b.title+" pieces="+b.pieces.Length+" moves="+b.solution.Length);
        }
        File.WriteAllText("DraculasBox/Evidence/DraftPresets/selected-final.json",JsonSerializer.Serialize(output,Json));
    }

    static void Gates(List<LevelData> bases)
    {
        foreach(var v in JsonDocument.Parse(File.ReadAllText("DraculasBox/Evidence/DraftPresets/sun-candidates.json")).RootElement.EnumerateArray())
            if(v.GetProperty("mask").GetInt32()==11)bases.Add(JsonSerializer.Deserialize<LevelData>(v.GetProperty("board").GetRawText(),Json));
        var sources=bases.Where(b=>Mask(b)==10||Mask(b)==11).ToArray();var rng=new Random(9182);var output=new List<object>();
        for(int i=0;i<24000;i++)
        {
            var b=sources[rng.Next(sources.Length)].Copy();b.id="gate_"+i;
            var free=new List<Cell>();for(int y=1;y<b.rows.Length-1;y++)for(int x=1;x<b.rows[0].Length-1;x++){var p=new Cell(x,y);if(b.rows[y][x]=='.'&&p!=b.player&&p!=b.coffin&&!b.pieces.Any(e=>e.cell==p))free.Add(p);}
            if(free.Count<2)continue;var plate=free[rng.Next(free.Count)];free.Remove(plate);var door=free[rng.Next(free.Count)];
            b.plates=new[]{new DeviceData{cell=plate}};b.doors=new[]{new DeviceData{cell=door}};
            free.Remove(door);if(free.Count>0)b.pieces=b.pieces.Append(new EntityData{id="plate-box",cell=free[rng.Next(free.Count)]}).ToArray();
            try{var quick=LevelSearch.Solve(b,limit:5000);if(!quick.Solved||quick.Moves<7)continue;
                var noGate=b.Copy();noGate.plates=Array.Empty<DeviceData>();noGate.doors=Array.Empty<DeviceData>();var open=LevelSearch.Solve(noGate,limit:5000);if(!open.Solved||quick.Moves<=open.Moves)continue;
                var result=Inspect(b);if(result!=null){output.Add(result);Console.WriteLine(b.id+" "+Mask(b)+" "+quick.Moves+" "+quick.PushActions);}
            }catch{}
        }
        File.WriteAllText("DraculasBox/Evidence/DraftPresets/gate-candidates.json",JsonSerializer.Serialize(output,Json));
    }

    static void Partitions(List<LevelData> bases)
    {
        var output=new List<object>();int index=0;
        foreach(var source in bases)
        {
            var original=source.Copy();original.sunEnabled=false;
            LevelSearch.Result r;try{r=LevelSearch.Solve(original,limit:40000);}catch{continue;}
            if(!r.Solved)continue;
            var used=new HashSet<Cell>();var session=new Session(new Definition(original));
            void Keep(){used.Add(session.State.Player);for(int n=0;n<original.pieces.Length;n++)used.Add(session.State.Position(n));}
            Keep();foreach(char k in r.Route){session.Apply((Command)"URDL".IndexOf(k));Keep();}
            foreach(var d in original.doors.Concat(original.plates))used.Add(d.cell);used.Add(original.coffin);
            for(int side=0;side<4;side++)for(int variation=0;variation<16;variation++)
            {
                var b=original.Copy();b.sunEnabled=true;b.sunFrom=(SunSide)side;b.id="part_"+index++;
                var rng=new Random(variation*187+index);
                for(int y=1;y<b.rows.Length-1;y++)for(int x=1;x<b.rows[0].Length-1;x++)
                    if(b.rows[y][x]=='.'&&!used.Contains(new Cell(x,y))&&(variation==0||rng.Next(3)!=0))
                    {var row=b.rows[y].ToCharArray();row[x]='#';b.rows[y]=new string(row);}
                try{var result=Inspect(b);if(result!=null){output.Add(result);Console.WriteLine(b.id+" "+b.title+" "+Mask(b)+" "+b.solution);}}catch{}
            }
        }
        File.WriteAllText("DraculasBox/Evidence/DraftPresets/partition-candidates.json",JsonSerializer.Serialize(output,Json));
    }

    static void SunRooms(List<LevelData> bases)
    {
        var rng=new Random(91247);var output=new List<object>();int safe=0,solved=0;string error="";
        // Try local room partitions on existing door/mirror puzzles, not serial corridor assembly.
        var sources=bases.Where(b=>(Mask(b)&(4|8))!=0).ToArray();
        for(int i=0;i<40000;i++)
        {
            var source=sources[rng.Next(sources.Length)];var b=source.Copy();
            b.sunEnabled=true;b.sunFrom=(SunSide)rng.Next(4);b.id="sun_"+i;
            var occupied=b.pieces.Select(p=>p.cell).Concat(b.doors.Select(d=>d.cell)).Concat(b.plates.Select(d=>d.cell)).Append(b.player).Append(b.coffin).ToHashSet();
            var floors=new List<Cell>();for(int y=1;y<b.rows.Length-1;y++)for(int x=1;x<b.rows[0].Length-1;x++)if(b.rows[y][x]=='.'&&!occupied.Contains(new Cell(x,y)))floors.Add(new Cell(x,y));
            if(i%2==0&&floors.Count>0)b.player=floors[rng.Next(floors.Count)];
            if(i%3==0&&floors.Count>0&&b.pieces.Length>0)b.pieces[rng.Next(b.pieces.Length)].cell=floors[rng.Next(floors.Count)];
            floors.RemoveAll(p=>p==b.player||b.pieces.Any(e=>e.cell==p));
            int changes=rng.Next(1,6);
            for(int j=0;j<changes&&floors.Count>0;j++){var p=floors[rng.Next(floors.Count)];var row=b.rows[p.y].ToCharArray();row[p.x]='#';b.rows[p.y]=new string(row);}
            try{
                if(LightField.Compute(new Definition(b).Initial()).Lit(b.player))continue;
                safe++;var quick=LevelSearch.Solve(b,limit:3500);if(!quick.Solved||quick.PushActions<1)continue;
                solved++;var result=Inspect(b);if(result!=null){output.Add(result);Console.WriteLine(b.id+" "+b.title+" mask="+Mask(b)+" steps="+quick.Moves+" pushes="+quick.PushActions);}
            }catch(Exception ex){error=ex.Message;}
            if(i%1000==0)File.WriteAllText("DraculasBox/Evidence/DraftPresets/sun-candidates.json",JsonSerializer.Serialize(output,Json));
        }
        File.WriteAllText("DraculasBox/Evidence/DraftPresets/sun-candidates.json",JsonSerializer.Serialize(output,Json));
        Console.WriteLine($"safe={safe} solved={solved} error={error}");
    }
}
