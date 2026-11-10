using System.Text.Json;
using DraculasBox.Core;

internal static class MirrorDrafts
{
    public static void Publish()
    {
        var opts=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};var root="Assets/DraculasBox/Content/";
        var selected=new[]{("intro","c4_01","转光"),("two/two_06","c4_02","借墙"),("two/two_08","c4_03","背面"),("braid/braid_02","c4_04","两束光"),("gate/gate_06","c4_05","换位"),("gate/gate_05","c4_06","回照"),("braid/braid_09","c4_b01","对照"),("braid/braid_04","c4_b02","往返")};
        var receipts=new List<object>();var replay=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(root+"Replays.json"))!;
        foreach(var (source,id,title) in selected)
        {
            LevelData data;if(source=="intro")data=new LevelData{rows=new[]{"#######","###.###","##..###","#.....#","#.....#","#.....#","#######"},player=new Cell(4,4),coffin=new Cell(3,1),pieces=new[]{new EntityData{id="L",kind=PieceKind.Lamp,cell=new Cell(1,3)},new EntityData{id="M",kind=PieceKind.MirrorSlash,cell=new Cell(3,3)}}};
            else data=JsonSerializer.Deserialize<LevelData>(File.ReadAllText("DraculasBox/Evidence/Chapter4/Drafts/"+source+".json"),opts)!;
            if(id=="c4_02")data.pieces.Single(p=>p.id=="M").kind=PieceKind.Box;
            data.id=id;data.title=title;data.hint="";data.sunEnabled=false;var solve=LevelSearch.Solve(data);if(!solve.Solved)throw new Exception("No solution: "+id);
            var essential=new List<string>();foreach(var piece in data.pieces){var freeze=LevelSearch.Solve(data,new HashSet<string>{piece.id});if(!freeze.Solved&&freeze.Exhausted)essential.Add(piece.id);}
            var opaque=data.Copy();foreach(var p in opaque.pieces)if((int)p.kind>=2)p.kind=PieceKind.Box;var simple=LevelSearch.Solve(opaque);string wrong=id=="c4_01"?"LU":"";
            if(id!="c4_01"&&simple.Solved){var probe=new Session(new Definition(data));foreach(char key in simple.Route){var action=probe.Apply((Command)"URDL".IndexOf(key));wrong+=key;if(action.After.Dead)break;}if(!probe.State.Dead)wrong="";}
            var fixture=new{id,route=solve.Route,shortest=solve.Route,pushes=solve.PushActions,essential,wrong};
            var old=replay["levels"]!.AsArray().FirstOrDefault(n=>n!["id"]!.GetValue<string>()==id);if(old!=null)replay["levels"]!.AsArray().Remove(old);
            replay["levels"]!.AsArray().Add(JsonSerializer.SerializeToNode(fixture,opts));
            File.WriteAllText(root+"Levels/"+id+".json",JsonSerializer.Serialize(data,opts));receipts.Add(new{id,solve,essential,opaqueMoves=simple.Moves,wrong});
        }
        File.WriteAllText(root+"Replays.json",replay.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));
        var catalog=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(root+"Chapters.json"))!;var chapters=catalog["chapters"]!.AsArray();var prior=chapters.FirstOrDefault(n=>n!["id"]!.GetValue<string>()=="chapter_4");if(prior!=null)chapters.Remove(prior);
        var links=selected.Take(6).Select((n,i)=>new{id=n.Item2,main=true,requires=i==0?Array.Empty<string>():new[]{selected[i-1].Item2}}).Concat(new[]{new{id="c4_b01",main=false,requires=new[]{"c4_03"}},new{id="c4_b02",main=false,requires=new[]{"c4_06"}}}).ToArray();
        chapters.Add(JsonSerializer.SerializeToNode(new{id="chapter_4",title="第四章 · 镜面之间",description="",requiresChapter="chapter_3",levels=links}));File.WriteAllText(root+"Chapters.json",catalog.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));
        File.WriteAllText("DraculasBox/Evidence/Chapter4/selected.json",JsonSerializer.Serialize(receipts,opts));Console.WriteLine(JsonSerializer.Serialize(receipts,opts));
    }
    public static void Assess()
    {
        var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};var result=new List<object>();
        foreach(var file in Directory.GetFiles("DraculasBox/Evidence/Chapter4/Drafts","*.json",SearchOption.AllDirectories).Where(f=>!f.EndsWith("results.json")))
        {
            var d=JsonSerializer.Deserialize<LevelData>(File.ReadAllText(file),options)!;var full=LevelSearch.Solve(d);var boxes=d.Copy();foreach(var p in boxes.pieces)if((int)p.kind>=2)p.kind=PieceKind.Box;
            var simple=LevelSearch.Solve(boxes);var noDoor=d.Copy();noDoor.plates=Array.Empty<DeviceData>();noDoor.doors=Array.Empty<DeviceData>();var doorless=LevelSearch.Solve(noDoor);
            var s=new Session(new Definition(d));var pushes=new List<string>();foreach(char c in full.Route){var a=s.Apply((Command)"URDL".IndexOf(c));if(a.PushedCount>0)pushes.Add(c+":"+string.Join(",",Enumerable.Range(0,d.pieces.Length).Where(i=>a.Before.Position(i)!=a.After.Position(i)).Select(i=>d.pieces[i].id+"="+a.After.Position(i))));}
            result.Add(new{id=d.id,moves=full.Moves,boxMoves=simple.Moves,boxRoute=simple.Route,doorless=doorless.Solved?doorless.Moves:-1,route=full.Route,pushes});
        }
        Console.WriteLine(JsonSerializer.Serialize(result,options));File.WriteAllText("DraculasBox/Evidence/Chapter4/assessment.json",JsonSerializer.Serialize(result,options));
    }
    public static void Run(string[] args)
    {
        string mode=args.Length>1?args[1]:"two";var random=new Random(mode=="two"?48317:mode=="dual"?72831:91653);
        string folder="DraculasBox/Evidence/Chapter4/Drafts/"+mode;Directory.CreateDirectory(folder);
        var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};var kept=new List<object>();
        var layouts=new[]{
            new[]{"#########","###...###","##.....##","#.......#","##.....##","##.....##","#########"},
            new[]{"#########","####.####","#.......#","#..#.#..#","#.......#","#.......#","#########"},
            new[]{"#########","#.......#","#..#....#","#..#....#","#....#..#","###.#####","#########"},
            new[]{"#########","#.......#","#...#...#","#.......#","#...#...#","#.......#","#########"},
            new[]{"#########","#.......#","#..###..#","#.......#","#..#....#","#.......#","#########"}
        };
        int attempts=0,target=mode=="two"?12:10;
        for(int trial=0;trial<250000&&kept.Count<target;trial++)
        {
            attempts++;var rows=(string[])layouts[random.Next(layouts.Length)].Clone();
            var free=new List<Cell>();for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[0].Length;x++)if(rows[y][x]=='.')free.Add(new Cell(x,y));
            Cell Take(){int at=random.Next(free.Count);var p=free[at];free.RemoveAt(at);return p;}
            var ends=free.Where(p=>LightField.Directions.Count(d=>free.Contains(p+d))==1).ToArray();var goal=ends.Length>0?ends[random.Next(ends.Length)]:Take();free.Remove(goal);var player=Take();var entities=new List<EntityData>();
            entities.Add(new EntityData{id="L",kind=PieceKind.Lamp,cell=Take()});
            if(mode=="dual"||mode=="cross"||mode=="braid")entities.Add(new EntityData{id="K",kind=PieceKind.Lamp,cell=Take()});
            entities.Add(new EntityData{id="M",kind=PieceKind.MirrorSlash,cell=Take()});
            if(mode!="dual"&&mode!="cross")entities.Add(new EntityData{id="N",kind=PieceKind.MirrorBackslash,cell=Take()});
            if(mode=="dual"||mode=="gate")entities.Add(new EntityData{id="A",kind=PieceKind.Box,cell=Take()});
            var data=new LevelData{id="draft",title="draft",rows=rows,player=player,coffin=goal,pieces=entities.ToArray()};
            if(mode=="gate")
            {
                data.plates=new[]{new DeviceData{cell=Take(),channel=0}};data.doors=new[]{new DeviceData{cell=Take(),channel=0}};
            }
            Definition definition;try{definition=new Definition(data,false);var light=LightField.Compute(definition.Initial());var safe=free.Where(p=>light.Safe(p)).ToArray();if(!light.Safe(data.player)){if(safe.Length==0)continue;data.player=safe[random.Next(safe.Length)];}definition=new Definition(data);}catch(ArgumentException){continue;}
            var start=LightField.Compute(definition.Initial());if(!start.ReflectionEdges.Any(b=>b>0))continue;
            var result=LevelSearch.Solve(data,limit:18000);
            if(!result.Solved||result.Moves<((mode=="two"||mode=="cross")?9:17)||result.Moves>52||result.PushActions<((mode=="two"||mode=="cross")?2:5))continue;
            bool necessary=true;var essential=new List<string>();
            foreach(var p in data.pieces.Where(p=>p.kind==PieceKind.MirrorSlash||p.kind==PieceKind.MirrorBackslash||p.kind==PieceKind.Box))
            {
                var test=LevelSearch.Solve(data,new HashSet<string>{p.id},28000);if(test.Solved||!test.Exhausted){necessary=false;break;}essential.Add(p.id);
            }
            if(!necessary)continue;var boxes=data.Copy();foreach(var p in boxes.pieces)if((int)p.kind>=2)p.kind=PieceKind.Box;var boxResult=LevelSearch.Solve(boxes,limit:28000);if(boxResult.Solved&&boxResult.Moves>=result.Moves-1)continue;
            var session=new Session(definition);int simultaneous=0,fullBends=0;var mirrorMoves=new Dictionary<string,int>();int plateMirror=0,doorChanges=0;
            foreach(char key in result.Route)
            {
                var move=session.Apply((Command)"URDL".IndexOf(key));var field=LightField.Compute(move.After);
                simultaneous=Math.Max(simultaneous,field.ReflectionEdges.Count(b=>b!=0));fullBends=Math.Max(fullBends,field.ReflectionEdges.Count(b=>b==15));
                for(int i=0;i<data.pieces.Length;i++)if((int)data.pieces[i].kind>=2&&move.Before.Position(i)!=move.After.Position(i))mirrorMoves[data.pieces[i].id]=mirrorMoves.GetValueOrDefault(data.pieces[i].id)+1;
                if(mode=="gate")
                {
                    if(move.After.DoorOpen(0)!=move.Before.DoorOpen(0))doorChanges++;
                    if(data.pieces.Where(p=>(int)p.kind>=2).Any(p=>move.After.Position(Array.FindIndex(data.pieces,e=>e.id==p.id))==data.plates[0].cell))plateMirror++;
                }
            }
            if(mode=="two"&&simultaneous<2||(mode=="dual"||mode=="cross")&&fullBends==0||mode=="gate"&&(plateMirror==0||doorChanges<2))continue;
            data.id=mode+"_"+(kept.Count+1).ToString("00");data.title=data.id;
            File.WriteAllText(Path.Combine(folder,data.id+".json"),JsonSerializer.Serialize(data,options));
            kept.Add(new{id=data.id,result,essential,simultaneous,fullBends,mirrorMoves,plateMirror,doorChanges});
            Console.WriteLine(data.id+" "+result.Route+" pushes="+result.PushActions+" visited="+result.Visited);
        }
        File.WriteAllText(Path.Combine(folder,"results.json"),JsonSerializer.Serialize(new{attempts,kept},options));Console.WriteLine("attempts="+attempts+" kept="+kept.Count);
    }
}
