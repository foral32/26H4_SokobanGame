using System.Text.Json;
using DraculasBox.Core;

if(args.Length>0&&args[0]=="authoring"){AuthoringChecks.Run();return;}
if(args.Length>0&&args[0]=="solve"){LevelSearch.Run(args);return;}
if(args.Length>0&&args[0]=="coffin"){Console.WriteLine("Coffin checks: "+CoffinChecks.Run());return;}
if(args.Length>0&&args[0]=="audit-exits"){CoffinChecks.Audit();return;}
if(args.Length>0&&args[0]=="exit-candidates"){CoffinChecks.Candidates(args.Skip(1).ToArray());return;}
if(args.Length>0&&args[0]=="mirrors"){Console.WriteLine("Mirror checks: "+MirrorChecks.Run());return;}
if(args.Length>0&&args[0]=="draft-mirrors"){MirrorDrafts.Run(args);return;}
if(args.Length>0&&args[0]=="assess-mirrors"){MirrorDrafts.Assess();return;}
if(args.Length>0&&args[0]=="publish-mirrors"){MirrorDrafts.Publish();return;}
if(args.Length>0&&args[0]=="draft-lights"){CampaignDrafts.Run(args);return;}
if(args.Length>0&&args[0]=="generate"){LevelDrafts.Run(args);return;}
if(args.Length>0&&args[0]=="trace"){LevelDrafts.Trace(args);return;}
if(args.Length>0&&args[0]=="input"){var check=InputChecks.Run();Console.WriteLine(JsonSerializer.Serialize(check.report,new JsonSerializerOptions{WriteIndented=true}));Environment.ExitCode=check.passed?0:1;return;}

int checks=0;
checks+=CoffinChecks.Run();
checks+=MirrorChecks.Run();
checks+=MultiLampChecks.Run();
checks+=DoorChecks.Run();
void Require(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
var inputCheck=InputChecks.Run();Require(inputCheck.passed,"short taps, repeat timing and cancellation");checks+=inputCheck.checks;
var opts=new JsonSerializerOptions{IncludeFields=true};
var root=Directory.GetCurrentDirectory();
var replays=JsonDocument.Parse(File.ReadAllText("Assets/DraculasBox/Content/Replays.json")).RootElement.GetProperty("levels").EnumerateArray().ToArray();
Require(replays.Length>0&&replays.Select(r=>r.GetProperty("id").GetString()).Distinct().Count()==replays.Length,"main and optional puzzles have distinct replay fixtures");
foreach(var replay in replays)
{
    string id=replay.GetProperty("id").GetString()!,route=replay.GetProperty("route").GetString()!;
    var data=JsonSerializer.Deserialize<LevelData>(File.ReadAllText($"Assets/DraculasBox/Content/Levels/{id}.json"),opts)!;
    var s=new Session(new Definition(data));string initial=s.State.Key();
    int pushes=0;
    foreach(char c in route){var result=s.Apply((Command)"URDL".IndexOf(c));Require(result.Changed,"campaign route "+id+" "+c);if(result.Kind==ActionKind.Pushed)pushes++;}
    Require(s.State.Won&&pushes==replay.GetProperty("pushes").GetInt32(),"campaign win and pushes "+id);int moves=s.Moves;
    s.Apply(Command.Reset);Require(s.State.Key()==initial&&s.Moves==0,"reset initial");
    s.Apply(Command.Undo);Require(s.State.Won&&s.Moves==moves,"reset undo snapshot");
    for(int j=0;j<route.Length;j++)s.Apply(Command.Undo);
    Require(s.State.Key()==initial&&s.Moves==0,"full undo "+id);
    if(id=="c1_01")Require(!data.sunEnabled,"no sunlight in teaching level");
    if(id=="c1_02")Require(data.sunEnabled,"second level introduces sunlight");
    if(string.CompareOrdinal(id,"c1_05")>=0&&replay.TryGetProperty("shortest",out var shortest)&&!string.IsNullOrEmpty(shortest.GetString()))
    {
        var solution=LevelSearch.Solve(data);Require(solution.Solved&&solution.Route.Length==replay.GetProperty("shortest").GetString()!.Length,"shortest route verified "+id);
        string[] essential=replay.GetProperty("essential").EnumerateArray().Select(v=>v.GetString()!).ToArray();
        foreach(string piece in essential){var blocked=LevelSearch.Solve(data,new HashSet<string>{piece});Require(!blocked.Solved&&blocked.Exhausted,"cannot bypass piece "+id+" "+piece);}
        if(id=="c1_05")Require(data.sunFrom==SunSide.Top,"fifth level changes sun direction without a new rule");
        if(id=="c1_06")
        {
            Require(!data.sunEnabled,"first lamp puzzle isolates lamp from sunlight");
            foreach(char c in "LURDRU")Require(s.Apply((Command)"URDL".IndexOf(c)).Changed,"lamp wrong-turn setup");
            var dead=data.Copy();dead.player=s.State.Player;for(int j=0;j<dead.pieces.Length;j++)dead.pieces[j].cell=s.State.Position(j);
            var trapped=LevelSearch.Solve(dead);Require(!trapped.Solved&&trapped.Exhausted,"pushing lamp into the coffin neck is a recoverable-by-undo deadlock");
        }
        if(id=="c2_01")
        {
            Require(LevelSearch.Solve(data,new HashSet<string>{"L"}).Solved,"second chapter opener supports shielding without moving the lamp");
            var alternate=new Session(new Definition(data));foreach(char c in replay.GetProperty("alternate").GetString()!)Require(alternate.Apply((Command)"URDL".IndexOf(c)).Changed,"alternative approach");
            Require(alternate.State.Won,"second chapter opener also supports relocating the lamp");
        }
        if(id=="c2_02"||id=="c2_03"||id=="c2_06"||id=="c2_07")
        {
            var turn=new Session(new Definition(data));int lightIndex=Array.FindIndex(data.pieces,p=>p.kind==PieceKind.Lamp);var directions=new HashSet<Command>();
            foreach(char c in route){var move=turn.Apply((Command)"URDL".IndexOf(c));if(move.Before.Position(lightIndex)!=move.After.Position(lightIndex))directions.Add(move.Command);}
            Require(directions.Count>=2,"lamp changes axis "+id);
        }
    }
}
var catalog=JsonDocument.Parse(File.ReadAllText("Assets/DraculasBox/Content/Chapters.json")).RootElement.GetProperty("chapters").EnumerateArray().ToArray();
Require(catalog.Length>0&&Enumerable.Range(1,catalog.Length-1).All(i=>catalog[i].GetProperty("requiresChapter").GetString()==catalog[i-1].GetProperty("id").GetString()),"chapters unlock in authored sequence");
Require(catalog.SelectMany(c=>c.GetProperty("levels").EnumerateArray()).All(l=>replays.Any(r=>r.GetProperty("id").GetString()==l.GetProperty("id").GetString())),"every campaign node has a playable fixture");
foreach(var chapter in catalog){var links=chapter.GetProperty("levels").EnumerateArray().ToArray();var main=links.Where(l=>l.GetProperty("main").GetBoolean()).Select(l=>l.GetProperty("id").GetString()).ToHashSet();Require(links.Where(l=>!l.GetProperty("main").GetBoolean()).All(l=>l.GetProperty("requires").EnumerateArray().Any()&&l.GetProperty("requires").EnumerateArray().All(r=>main.Contains(r.GetString()))),"optional levels unlock from main nodes");Require(links.Where(l=>l.GetProperty("main").GetBoolean()).All(l=>l.GetProperty("requires").EnumerateArray().All(r=>main.Contains(r.GetString()))),"main line never requires an optional puzzle");}
LevelData Room()=>new LevelData{id="spec",title="spec",rows=Enumerable.Repeat(".........",9).ToArray(),player=new Cell(1,1),coffin=new Cell(8,8),pieces=new[]{new EntityData{id="A",cell=new Cell(4,4)}}};
foreach(SunSide side in Enum.GetValues<SunSide>())
{
    var a=Room();a.sunEnabled=true;a.sunFrom=side;
    // A safe spawn on the finite shadow, keeping test fixture legitimate.
    a.player=a.pieces[0].cell+LightField.SunTravel[(int)side];
    var def=new Definition(a);var f=LightField.Compute(def.Initial());
    var expected=new HashSet<Cell>{a.player,a.player+LightField.SunTravel[(int)side]};
    for(int y=0;y<9;y++)for(int x=0;x<9;x++)Require(f.Sun[y*9+x]==!expected.Contains(new Cell(x,y)),"finite shadow all directions");
}
var lamp=Room();lamp.pieces[0].kind=PieceKind.Lamp;
var lf=LightField.Compute(new Definition(lamp).Initial());
Require(lf.Lamp.Count(x=>x)==17,"empty room cross 17");Require(!lf.Lit(new Cell(3,3)),"diagonal dark");
lamp.pieces=lamp.pieces.Concat(new[]{new EntityData{id="shield",cell=new Cell(4,2)}}).ToArray();
lf=LightField.Compute(new Definition(lamp).Initial());Require(!lf.Lit(new Cell(4,0))&&!lf.Lit(new Cell(4,1)),"ray stops indefinitely");
var litSpawn=lamp.Copy();litSpawn.player=new Cell(4,5);bool rejected=false;try{new Definition(litSpawn);}catch(ArgumentException){rejected=true;}Require(rejected,"unsafe spawn rejected");
var chain=LevelData.Blank();chain.player=new Cell(1,3);chain.coffin=new Cell(8,2);chain.pieces=new[]{new EntityData{id="box",cell=new Cell(2,3)},new EntityData{id="light",kind=PieceKind.Lamp,cell=new Cell(3,3)}};
var session=new Session(new Definition(chain));var act=session.Apply(Command.Right);Require(act.PushedCount==2&&act.Changed,"box transmits force to lamp");
Require(session.State.Position(0)==new Cell(3,3)&&session.State.Position(1)==new Cell(4,3),"chain positions");
session.Apply(Command.Undo);Require(session.State.Player==chain.player,"chain undo");
var exposure=Room();exposure.player=new Cell(3,2);exposure.sunEnabled=true;exposure.pieces=new[]{new EntityData{id="A",cell=new Cell(3,3)},new EntityData{id="B",cell=new Cell(2,2)}};
session=new Session(new Definition(exposure));string before=session.State.Key();act=session.Apply(Command.Down);
Require(act.Changed&&session.State.Player==new Cell(3,3)&&session.State.Position(0)==new Cell(3,4)&&session.UndoCount==1,"unsafe push enters light and commits complete layout");
Require(LightField.Compute(session.State).Lit(session.State.Player),"death occurs on the actual illuminated cell");
Require(!session.Apply(Command.Right).Changed,"cannot keep walking after death");
session.Apply(Command.Reset);Require(session.State.Key()==before,"reset recovers death");
session.Apply(Command.Undo);Require(LightField.Compute(session.State).Lit(session.State.Player),"undo reset restores dead snapshot");
session.Apply(Command.Undo);Require(session.State.Key()==before&&session.UndoCount==0,"undo death restores all pushed objects");
var lampExposure=Room();lampExposure.player=new Cell(3,3);lampExposure.pieces[0].kind=PieceKind.Lamp;
session=new Session(new Definition(lampExposure));act=session.Apply(Command.Right);
Require(act.Changed&&act.After.Player==new Cell(4,3)&&LightField.Compute(act.After).Lit(act.After.Player),"lamp light can be entered");
session.Apply(Command.Undo);Require(session.State.Player==lampExposure.player,"lamp death is undoable");
var coffin=Room();coffin.player=new Cell(4,4);coffin.coffin=new Cell(4,3);coffin.pieces[0].cell=new Cell(3,4);coffin.sunEnabled=true;
session=new Session(new Definition(coffin));Require(session.Apply(Command.Up).After.Dead&&!session.State.Won,"sunlit coffin is fatal");
Require(!session.Apply(Command.Right).Changed,"no movement after fatal coffin entry");
var edit=new EditDocument(LevelData.Blank());string original=JsonSerializer.Serialize(edit.Data,opts);
edit.Begin();Require(edit.Paint(new Cell(2,2),Brush.Wall),"paint");Require(edit.Paint(new Cell(3,2),Brush.Wall),"stroke");edit.End();
Require(edit.UndoCount==1,"one drag one history");Require(edit.Undo()&&JsonSerializer.Serialize(edit.Data,opts)==original,"edit undo restores entire document");Require(edit.Redo(),"edit redo");
edit.ConfigureSun(true,SunSide.Right);Require(edit.Data.sunEnabled&&edit.Undo(),"sun configuration undo");
var copy=edit.Data.Copy();copy.rows[1]=".........";Require(edit.Data.rows[1]!=copy.rows[1],"draft copy isolated");
edit.Begin();edit.Paint(edit.Data.player,Brush.Erase);edit.End();Require(edit.Validation()!=null,"invalid draft cannot play");
edit.Undo();Require(edit.Validation()==null,"recover invalid draft");
edit.Saved();edit.ConfigureSun(true,SunSide.Top);Require(edit.Dirty,"modified document is dirty");edit.Undo();Require(!edit.Dirty,"undo back to saved layout clears dirty state");
Require(edit.Resize(32,32)&&edit.Data.rows.Length==32,"editor supports the full level format size");edit.Undo();Require(!edit.Dirty,"resize undo restores saved state");
var savedLayout=edit.Data.Copy();edit.Rename("recovered draft");var recovered=new EditDocument(edit.Data);recovered.SavedBaseline(savedLayout);Require(recovered.Dirty,"recovery compares against disk baseline, not recovered data");
EditorChecks.Run(Require);
AreaChecks.Run(Require);
Console.WriteLine(JsonSerializer.Serialize(new{passed=true,checks,campaignLevels=replays.Length,solarDirections=4},new JsonSerializerOptions{WriteIndented=true}));
