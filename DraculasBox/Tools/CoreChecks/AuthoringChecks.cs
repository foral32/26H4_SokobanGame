using System.Diagnostics;
using System.Text.Json;
using DraculasBox.Core;

internal static class AuthoringChecks
{
    public static void Run()
    {
        void Check(bool ok,string name){if(!ok)throw new Exception(name);}
        var json=new JsonSerializerOptions{IncludeFields=true};
        var replays=JsonDocument.Parse(File.ReadAllText("Assets/DraculasBox/Content/Replays.json")).RootElement.GetProperty("levels");
        int solved=0,limited=0;
        foreach(var plan in replays.EnumerateArray())
        {
            var id=plan.GetProperty("id").GetString();
            var board=JsonSerializer.Deserialize<LevelData>(File.ReadAllText($"Assets/DraculasBox/Content/Levels/{id}.json"),json);
            string recorded=plan.GetProperty("route").GetString();
            Check(LevelSolver.Proves(board,recorded,out _),"legacy proof "+id);
            var result=LevelSolver.Solve(board);
            if(result.Outcome==SearchOutcome.Limit){limited++;continue;}
            Check(result.Outcome==SearchOutcome.Solved&&result.Route.Length<=recorded.Length&&LevelSolver.Proves(board,result.Route,out int pushes)&&pushes==result.Pushes,"solve "+id);
            if(plan.TryGetProperty("shortest",out var shortest)&&shortest.GetString()?.Length>0)
                Check(result.Route.Length==shortest.GetString().Length,"minimum length "+id);
            solved++;
        }
        Console.WriteLine($"Campaign: {solved} shortest, {limited} bounded searches");
        var small=LevelData.Blank(7,5);small.pieces=Array.Empty<EntityData>();
        var resultSmall=LevelSolver.Solve(small);var path=resultSmall.Route;
        Check(path=="RR"&&!LevelSolver.Proves(small,path+"L",out _)&&!LevelSolver.Proves(small,"R?",out _),"exact witness");
        var editSmall=new EditDocument(small);
        editSmall.Rename("另一个版本");Check(!editSmall.ApplySearchResult(resultSmall),"ignore stale job");
        editSmall.Undo();Check(editSmall.ApplySearchResult(resultSmall)&&editSmall.Data.solution==path,"accept matching job");
        var stale=small.Copy();stale.solution="U";
        var importedDoc=new EditDocument(stale);
        Check(importedDoc.RecordSolution(path)&&importedDoc.Data.solution==path,"invalid short import cannot displace a valid route");
        var trial=new Session(new Definition(small));trial.Apply(Command.Left);trial.Apply(Command.Reset);trial.Apply(Command.Right);
        trial.Apply(Command.Down);trial.Apply(Command.Undo);trial.Apply(Command.Right);
        Check(trial.WinningRoute()=="RR","trial trace excludes undone moves and pre-reset history");
        Check(LevelSolver.Solve(small,default,1).Outcome==SearchOutcome.Limit,"budget is unknown");
        small.rows[2]="#..#..#";small.rows[1]=small.rows[3]="#######";
        Check(LevelSolver.Solve(small).Outcome==SearchOutcome.Unsolvable,"disconnected");
        small.rows[2]="#.....#";small.pieces=new[]{new EntityData{id="box",cell=new Cell(3,2)}};
        Check(LevelSolver.Solve(small).Outcome==SearchOutcome.Unsolvable,"reachable terrain but exhausted pushes");
        small.player=new Cell(-1,-1);Check(LevelSolver.Solve(small).Outcome==SearchOutcome.Invalid,"incomplete board");
        var cancelled=new CancellationToken(true);
        try{LevelSolver.Solve(small,cancelled);throw new Exception("solver ignored cancellation");}catch(OperationCanceledException){}
        try{DraftGenerator.Generate(0,0,1,cancelled);throw new Exception("generator ignored cancellation");}catch(OperationCanceledException){}
        var watch=Stopwatch.StartNew();int drafts=0;var ranges=new[]{"","",""};
        foreach(DraftComplexity grade in Enum.GetValues<DraftComplexity>())
        {
            var lengths=new List<int>();var pushesList=new List<int>();
            for(int mask=0;mask<16;mask++)
            for(int seed=0;seed<12;seed++)
            {
                var options=(DraftMechanic)mask;
                var level=DraftGenerator.Generate(options,grade,seed+mask*71);
                Check(LevelSolver.Proves(level,level.solution,out int pushes)&&pushes>=(int)grade+1,"generated proof "+mask+"/"+grade+"/"+seed);
                Check(level.sunEnabled==options.HasFlag(DraftMechanic.Sun),"sun selection");
                Check(level.pieces.Any(p=>p.kind==PieceKind.Lamp)==((mask&10)!=0),"lamp selection");
                Check(level.pieces.Any(p=>p.kind==PieceKind.MirrorBackslash||p.kind==PieceKind.MirrorSlash)==((mask&8)!=0),"mirror selection");
                Check((level.doors.Length>0)==((mask&4)!=0)&&level.doors.Length==level.plates.Length,"circuit selection");
                lengths.Add(level.solution.Length);pushesList.Add(pushes);drafts++;
            }
            ranges[(int)grade]=$"{grade} {lengths.Min()}–{lengths.Max()} steps, {pushesList.Min()}–{pushesList.Max()} pushes";
        }
        var sample=DraftGenerator.Generate(DraftMechanic.Mirror,DraftComplexity.Standard,42);
        var repeat=DraftGenerator.Generate(DraftMechanic.Mirror,DraftComplexity.Standard,42);
        Check(sample.LayoutEquals(repeat),"seed");
        var doc=new EditDocument(LevelData.Blank());
        var before=doc.Data.Copy();doc.ApplyGenerated(sample);
        Check(doc.Dirty&&doc.UndoCount==1&&doc.Data.id==before.id&&doc.Data.title==before.title,"atomic adopt");
        doc.Rename("改名");Check(doc.Data.solution==sample.solution,"rename keeps proof");
        doc.ConfigureSun(!sample.sunEnabled,SunSide.Bottom);Check(string.IsNullOrEmpty(doc.Data.solution),"invalidate");
        doc.Undo();Check(doc.Data.solution==sample.solution,"undo restores proof");doc.Undo();doc.Undo();
        Check(doc.Data.ContentEquals(before)&&!doc.Dirty,"revert adopt");doc.Redo();
        Check(JsonSerializer.Deserialize<LevelData>(JsonSerializer.Serialize(doc.Data,json),json).ContentEquals(doc.Data),"roundtrip");
        Console.WriteLine($"{drafts} generated and replayed in {watch.Elapsed.TotalSeconds:F1}s. "+string.Join(" / ",ranges));
    }
}
