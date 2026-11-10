using System.Text.Json;
using DraculasBox.Core;

internal static class CampaignDrafts
{
    public static void Run(string[] args)
    {
        var opts=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};var source=JsonSerializer.Deserialize<LevelData>(File.ReadAllText(args[1]),opts)!;string folder=args[2];Directory.CreateDirectory(folder);
        var occupied=source.pieces.Select(p=>p.cell).Append(source.player).Append(source.coffin).ToHashSet();var free=new List<Cell>();for(int y=0;y<source.rows.Length;y++)for(int x=0;x<source.rows[0].Length;x++)if(source.rows[y][x]=='.'&&!occupied.Contains(new Cell(x,y)))free.Add(new Cell(x,y));
        int tries=0,kept=0;var best=new List<object>();
        foreach(var light in free)foreach(var weight in free.Append(new Cell(-1,-1)))
        {
            if(weight==light)continue;tries++;var d=source.Copy();var pieces=d.pieces.ToList();pieces.Add(new EntityData{id="M",kind=PieceKind.Lamp,cell=light});if(weight.x>=0)pieces.Add(new EntityData{id="C",cell=weight});d.pieces=pieces.ToArray();
            LevelSearch.Result result;try{result=LevelSearch.Solve(d,limit:55000);}catch(ArgumentException){continue;}
            if(!result.Solved||result.Moves<28||result.PushActions<8)continue;
            bool necessary=true;foreach(var piece in d.pieces){var frozen=LevelSearch.Solve(d,new HashSet<string>{piece.id},55000);if(frozen.Solved||!frozen.Exhausted){necessary=false;break;}}
            if(!necessary)continue;string id="draft_"+(++kept).ToString("000");d.id=id;d.title=id;File.WriteAllText(Path.Combine(folder,id+".json"),JsonSerializer.Serialize(d,opts));best.Add(new{id,light=light.ToString(),weight=weight.ToString(),result});Console.WriteLine(id+" moves="+result.Moves+" pushes="+result.PushActions+" "+result.Route);
        }
        File.WriteAllText(Path.Combine(folder,"results.json"),JsonSerializer.Serialize(new{tries,kept,results=best},opts));Console.WriteLine("tried "+tries+" selected "+kept);
    }
}
