using System.Text.Json;
using DraculasBox.Core;

internal static class PresetWorkshop
{
    public static LevelData Parse(string id,string name,string[] map,bool sun)
    {
        var b=new LevelData{id=id,title=name,rows=map.Select(r=>new string(r.Select(c=>c=='#'?'#':'.').ToArray())).ToArray(),sunEnabled=sun};
        var pieces=new List<EntityData>();var plates=new List<DeviceData>();var doors=new List<DeviceData>();
        for(int y=0;y<map.Length;y++)for(int x=0;x<map[y].Length;x++)
        {
            var p=new Cell(x,y);char c=map[y][x];
            if(c=='@')b.player=p;else if(c=='C')b.coffin=p;
            else if("BL/\\".Contains(c))pieces.Add(new EntityData{id="piece-"+pieces.Count,cell=p,kind=(PieceKind)"BL/\\".IndexOf(c)});
            else if("pqDE".Contains(c)) (c=='p'||c=='q'?plates:doors).Add(new DeviceData{cell=p,channel=c=='q'||c=='E'?1:0});
        }
        b.pieces=pieces.ToArray();b.plates=plates.ToArray();b.doors=doors.ToArray();return b;
    }
    public static void Assess(string path)
    {
        foreach(var c in JsonDocument.Parse(File.ReadAllText(path)).RootElement.EnumerateArray())
        {
            if(!c.GetProperty("result").GetProperty("Solved").GetBoolean()||c.GetProperty("frozen").GetProperty("Solved").GetBoolean())continue;
            var b=JsonSerializer.Deserialize<LevelData>(c.GetProperty("board").GetRawText(),new JsonSerializerOptions{IncludeFields=true})!;
            Console.WriteLine("\n"+b.title+" "+b.solution);
            var s=new Definition(b).Initial();
            foreach(char move in b.solution)
            {
                var n=Rules.Move(s,(Command)"URDL".IndexOf(move)).After;
                for(int i=0;i<b.pieces.Length;i++)if(s.Position(i)!=n.Position(i))
                    Console.WriteLine($" {move}: {b.pieces[i].kind} {i} {s.Position(i).x},{s.Position(i).y}->{n.Position(i).x},{n.Position(i).y} reflect={LightField.Compute(n).ReflectionEdges[n.Position(i).y*s.Level.Width+n.Position(i).x]}");
                s=n;
            }
            for(int i=0;i<b.pieces.Length;i++)
            {
                var r=LevelSearch.Solve(b,new HashSet<string>{b.pieces[i].id},90000);
                Console.WriteLine($" freeze {i} {b.pieces[i].kind}: {(r.Solved?"bypass "+r.Moves:r.Exhausted?"essential":"unknown")}");
            }
        }
    }
    public static void Verify()
    {
        int checks=0;void Need(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        var evidence=new List<object>();var watch=System.Diagnostics.Stopwatch.StartNew();
        Need(DraftPresets.All.Select(p=>p.Id).Distinct().Count()==DraftPresets.All.Count,"distinct preset ids");
        Need(DraftPresets.All.Select(p=>p.Title).Distinct().Count()==DraftPresets.All.Count,"distinct titles");
        foreach(var preset in DraftPresets.All)
        {
            var b=preset.Create();var result=LevelSearch.Solve(b,limit:90000);
            Need(result.Solved&&result.Moves==b.solution.Length,"shortest witness "+preset.Title);
            var frozen=LevelSearch.Solve(b,b.pieces.Select(p=>p.id).ToHashSet(),20000);
            Need(!frozen.Solved&&frozen.Exhausted,"mandatory pushing "+preset.Title);
            var roles=new List<object>();
            for(int i=0;i<b.pieces.Length;i++)
            {
                var test=LevelSearch.Solve(b,new HashSet<string>{b.pieces[i].id},90000);
                string removedLampRoute=null;
                if(b.pieces[i].kind==PieceKind.Lamp)
                {
                    var noLamp=b.Copy();noLamp.pieces=noLamp.pieces.Where(p=>p.id!=b.pieces[i].id).ToArray();
                    var shortcut=LevelSearch.Solve(noLamp,noLamp.pieces.Where(p=>p.kind==PieceKind.Lamp).Select(p=>p.id).ToHashSet(),90000);
                    removedLampRoute=shortcut.Solved?shortcut.Route:null;
                    Console.WriteLine(preset.Title+" lamp "+i+" absent, others fixed: "+(shortcut.Solved?shortcut.Route:"exhausted="+shortcut.Exhausted));
                }
                roles.Add(new{index=i,kind=b.pieces[i].kind.ToString(),mustMove=!test.Solved&&test.Exhausted,alternate=test.Solved?test.Route:null,removedLampRoute});
            }
            var actual=(b.sunEnabled?DraftMechanic.Sun:0)|(b.doors.Length>0?DraftMechanic.Circuit:0)
                |(b.pieces.Any(p=>p.kind==PieceKind.Lamp)?DraftMechanic.Lamp:0)|(b.pieces.Any(p=>(int)p.kind>=2)?DraftMechanic.Mirror:0);
            Need(actual==preset.Mechanics,"mechanic metadata "+preset.Title);
            int reflected=0,doorCrossings=0,boxShade=0,sunThreat=0;
            var session=new Session(new Definition(b));
            foreach(char key in b.solution)
            {
                var a=session.Apply((Command)"URDL".IndexOf(key));
                if(a.After.Level.DoorAt(a.After.Player)>=0)doorCrossings++;
                for(int i=0;i<b.pieces.Length;i++)if((int)b.pieces[i].kind>=2&&a.Before.Position(i)!=a.After.Position(i)
                    &&LightField.Compute(a.After).ReflectionEdges[a.After.Position(i).y*a.After.Level.Width+a.After.Position(i).x]!=0)reflected++;
                if(b.sunEnabled)
                {
                    var dir=LightField.SunTravel[(int)b.sunFrom];var back=new Cell(-dir.x,-dir.y);var one=a.After.Player+back;var two=one+back;
                    foreach(var cmd in new[]{Command.Up,Command.Right,Command.Down,Command.Left}){var move=Rules.Move(a.Before,cmd);if(move.Kind==ActionKind.Died){var f=LightField.Compute(move.After);int at=move.After.Player.y*move.After.Level.Width+move.After.Player.x;if(f.Sun[at]&&!f.Lamp[at])sunThreat++;}}
                    if(a.After.Level.Terrain(one)!='#'&&a.After.Level.Terrain(two)!='#'&&(a.After.At(one)>=0||a.After.At(two)>=0))boxShade++;
                }
            }
            Need(!preset.Mechanics.HasFlag(DraftMechanic.Circuit)||doorCrossings>0,"route uses door "+preset.Title);
            Need(!preset.Mechanics.HasFlag(DraftMechanic.Mirror)||reflected>0,"moved mirror redirects light "+preset.Title);
            Need(!b.sunEnabled||boxShade>0||sunThreat>0,"sunlight affects route choices "+preset.Title);
            string opaqueRoute=null;
            if(preset.Mechanics.HasFlag(DraftMechanic.Mirror))
            {
                var opaque=b.Copy();foreach(var p in opaque.pieces)if((int)p.kind>=2)p.kind=PieceKind.Box;
                var ordinary=LevelSearch.Solve(opaque,limit:90000);opaqueRoute=ordinary.Route;
                var mirrors=b.pieces.Where(p=>(int)p.kind>=2).Select(p=>p.id).ToHashSet();
                var opaqueFrozen=LevelSearch.Solve(opaque,mirrors,90000);
                bool changesRoute=ordinary.Solved&&!LevelSolver.Proves(b,ordinary.Route,out _);
                bool changesApproach=opaqueFrozen.Solved&&!LevelSolver.Proves(b,opaqueFrozen.Route,out _);
                Need(changesRoute||changesApproach,"reflection adds a real constraint "+preset.Title);
                if(changesApproach)opaqueRoute=opaqueFrozen.Route;
            }
            for(int pose=0;pose<8;pose++)
            {
                var variant=preset.Create(pose);int w=variant.rows[0].Length,h=variant.rows.Length;
                Need(Math.Max(w,h)<=10&&Math.Max(w,h)/(float)Math.Min(w,h)<=1.6f,"compact board "+preset.Title);
                Need(LevelSolver.Proves(variant,variant.solution,out int pushes)&&pushes==result.PushActions,"pose proof "+preset.Title+"/"+pose);
                var proof=LevelSearch.Solve(variant,limit:90000);
                Need(proof.Solved&&proof.Moves==result.Moves,"pose shortest "+preset.Title+"/"+pose);
                var play=new Session(new Definition(variant));string initial=play.State.Key();
                foreach(char key in variant.solution)Need(play.Apply((Command)"URDL".IndexOf(key)).Changed&&!play.State.Dead,"live rules move "+preset.Title);
                Need(play.State.Won,"coffin win "+preset.Title);
                for(int i=0;i<variant.solution.Length;i++)play.Apply(Command.Undo);
                Need(play.State.Key()==initial,"undo complete solution "+preset.Title);
            }
            evidence.Add(new{preset.Id,preset.Title,difficulty=preset.Difficulty,mechanics=preset.Mechanics.ToString(),width=b.rows[0].Length,height=b.rows.Length,
                shortest=result.Moves,pushes=result.PushActions,route=result.Route,roles,boxShade,doorCrossings,reflected,opaqueRoute});
        }
        int draws=0;
        for(int mask=0;mask<16;mask++)for(int grade=0;grade<3;grade++)for(int seed=0;seed<12;seed++)
        {
            var allowed=(DraftMechanic)mask;
            var choices=DraftGenerator.Choices(allowed,(DraftComplexity)grade);
            Need(choices.Length==DraftPresets.All.Count(p=>p.Mechanics==allowed&&(int)p.Complexity==grade),"exact filter count");
            if((mask&8)==0||(mask&2)!=0)Need(choices.Length>0,"UI-selectable combination covered "+mask+"/"+grade);
            if(choices.Length==0)
            {
                try{DraftGenerator.Generate(allowed,(DraftComplexity)grade,seed);throw new Exception("unexpected fallback");}
                catch(InvalidOperationException e){Need(e.Message=="暂无匹配草稿","explicit empty result");}
                continue;
            }
            var b=DraftGenerator.Generate(allowed,(DraftComplexity)grade,seed);var repeat=DraftGenerator.Generate(allowed,(DraftComplexity)grade,seed);
            Need(b.LayoutEquals(repeat)&&b.solution==repeat.solution&&b.id!=repeat.id,"deterministic layout, fresh identity");
            var preset=DraftPresets.All.Single(p=>p.Title==b.title);
            Need(preset.Mechanics==allowed&&(int)preset.Complexity==grade,"no disallowed mechanics");
            Need(LevelSolver.Proves(b,b.solution,out _),"generated witness");
            var another=DraftGenerator.Generate(allowed,(DraftComplexity)grade,seed,previousTitle:b.title);
            Need(DraftGenerator.Choices(allowed,(DraftComplexity)grade).Length==1||another.title!=b.title,"reroll distinct authored room");
            var doc=new EditDocument(LevelData.Blank());var original=doc.Data.Copy();doc.ApplyGenerated(b);
            Need(doc.UndoCount==1&&doc.Data.id==original.id&&doc.Data.title==original.title,"adopt identity and single undo");
            Need(LevelSolver.Proves(doc.Data,doc.Data.solution,out _),"adopt proof");doc.Undo();Need(doc.Data.ContentEquals(original),"adopt undo");
            draws++;
        }
        try{DraftGenerator.Generate(0,0,0,new CancellationToken(true));throw new Exception("cancellation ignored");}catch(OperationCanceledException){checks++;}
        Directory.CreateDirectory("DraculasBox/Evidence/DraftPresets");
        File.WriteAllText("DraculasBox/Evidence/DraftPresets/core-checks.json",JsonSerializer.Serialize(new{passed=true,checks,presets=evidence.Count,orientations=evidence.Count*8,draws,seconds=watch.Elapsed.TotalSeconds,levels=evidence},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Preset checks passed: {checks}; {evidence.Count} authored rooms, {evidence.Count*8} orientations, {draws} filter/seed combinations in {watch.Elapsed.TotalSeconds:F2}s.");
    }
    public static void Run(string path)
    {
        var output=new List<object>();int index=0;
        foreach(var c in JsonDocument.Parse(File.ReadAllText(path)).RootElement.EnumerateArray())
        {
            var b=Parse("preset_"+(++index),c.GetProperty("name").GetString()!,c.GetProperty("map").EnumerateArray().Select(x=>x.GetString()!).ToArray(),c.TryGetProperty("sun",out var sun)&&sun.GetBoolean());
            try
            {
                var result=LevelSearch.Solve(b,limit:90000);b.solution=result.Route;
                var frozen=result.Solved?LevelSearch.Solve(b,b.pieces.Select(p=>p.id).ToHashSet(),20000):null;
                Console.WriteLine($"{b.title}: {result.Solved} {result.Moves} steps {result.PushActions} pushes {result.Visited} states frozen={frozen?.Solved}");
                output.Add(new{ name=b.title,map=c.GetProperty("map").EnumerateArray().Select(x=>x.GetString()).ToArray(),sun=b.sunEnabled,result,frozen,board=b});
            }catch(Exception e){Console.WriteLine(b.title+": "+e.Message);}
        }
        File.WriteAllText(path+".results.json",JsonSerializer.Serialize(output,new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));
    }
}

