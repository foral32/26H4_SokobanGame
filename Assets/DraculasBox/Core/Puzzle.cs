using System;
using System.Collections.Generic;
using System.Linq;

namespace DraculasBox.Core
{
    public enum Command { Up, Right, Down, Left, Switch, Undo, Reset }
    public enum SunSide { Left, Top, Right, Bottom }
    public enum PieceKind { Box, Lamp, MirrorSlash, MirrorBackslash }
    [Serializable] public struct Cell : IEquatable<Cell>
    {
        public int x,y;
        public Cell(int x,int y){this.x=x;this.y=y;}
        public bool Equals(Cell p)=>x==p.x&&y==p.y;
        public override bool Equals(object o)=>o is Cell p&&Equals(p);
        public override int GetHashCode()=>x*397^y;
        public static bool operator ==(Cell a,Cell b)=>a.Equals(b);
        public static bool operator !=(Cell a,Cell b)=>!a.Equals(b);
        public static Cell operator +(Cell a,Cell b)=>new Cell(a.x+b.x,a.y+b.y);
        public override string ToString()=>x+","+y;
    }
    [Serializable] public sealed class EntityData
    {
        public string id; public PieceKind kind; public Cell cell;
        public EntityData Copy()=>new EntityData{id=id,kind=kind,cell=cell};
    }
    [Serializable] public sealed class DeviceData
    {
        public Cell cell;public int channel;
        public DeviceData Copy()=>new DeviceData{cell=cell,channel=channel};
    }
    [Serializable] public sealed class LevelData
    {
        public int schemaVersion=1; public string id,title,hint,solution,note;
        public string[] rows;
        public Cell player,coffin;
        public EntityData[] pieces=Array.Empty<EntityData>();
        public DeviceData[] plates=Array.Empty<DeviceData>(),doors=Array.Empty<DeviceData>();
        public bool sunEnabled; public SunSide sunFrom;
        public LevelData Copy()=>new LevelData{schemaVersion=schemaVersion,id=id,title=title,hint=hint,solution=solution,note=note,rows=(string[])rows.Clone(),player=player,coffin=coffin,pieces=pieces.Select(p=>p.Copy()).ToArray(),plates=(plates??Array.Empty<DeviceData>()).Select(p=>p.Copy()).ToArray(),doors=(doors??Array.Empty<DeviceData>()).Select(p=>p.Copy()).ToArray(),sunEnabled=sunEnabled,sunFrom=sunFrom};
        public bool ContentEquals(LevelData other)=>other!=null&&id==other.id&&title==other.title&&hint==other.hint&&(note??"")== (other.note??"")&&(solution??"")==(other.solution??"")&&LayoutEquals(other);
        public bool LayoutEquals(LevelData other)
        {
            if(other==null||schemaVersion!=other.schemaVersion||player!=other.player||coffin!=other.coffin||sunEnabled!=other.sunEnabled||sunFrom!=other.sunFrom||!rows.SequenceEqual(other.rows)||pieces.Length!=other.pieces.Length)return false;
            for(int i=0;i<pieces.Length;i++)if(pieces[i].id!=other.pieces[i].id||pieces[i].kind!=other.pieces[i].kind||pieces[i].cell!=other.pieces[i].cell)return false;
            if(!SameDevices(plates,other.plates)||!SameDevices(doors,other.doors))return false;
            return true;
        }
        private static bool SameDevices(DeviceData[] a,DeviceData[] b){a=a??Array.Empty<DeviceData>();b=b??Array.Empty<DeviceData>();return a.Length==b.Length&&a.Select((p,i)=>p.cell==b[i].cell&&p.channel==b[i].channel).All(v=>v);}
        public static LevelData Blank(int w=11,int h=7)
        {
            var rows=new string[h];for(int y=0;y<h;y++)rows[y]=y==0||y==h-1?new string('#',w):"#"+new string('.',w-2)+"#";
            return new LevelData{id=Guid.NewGuid().ToString("N"),title="我的关卡",hint="",rows=rows,player=new Cell(2,h/2),coffin=new Cell(w-3,h/2),pieces=new[]{new EntityData{id="box-1",cell=new Cell(w/2,h/2)}}};
        }
    }
    public sealed class Definition
    {
        private readonly LevelData data;
        private readonly int[] doorAt;
        public int Width=>data.rows[0].Length;
        public int Height=>data.rows.Length;
        public string Title=>data.title;
        public Cell Coffin=>data.coffin;
        public int PieceCount=>data.pieces.Length;
        public bool HasLamp {get;}
        public int PlateCount=>data.plates.Length;
        public int DoorCount=>data.doors.Length;
        public DeviceData Plate(int index)=>data.plates[index].Copy();
        public DeviceData Door(int index)=>data.doors[index].Copy();
        public Cell PlateCell(int index)=>data.plates[index].cell;
        public int PlateChannel(int index)=>data.plates[index].channel;
        public Cell DoorCell(int index)=>data.doors[index].cell;
        public int DoorChannel(int index)=>data.doors[index].channel;
        public int DoorAt(Cell p)=>Inside(p)?doorAt[p.y*Width+p.x]:-1;
        public PieceKind Kind(int i)=>data.pieces[i].kind;
        public bool Inside(Cell p)=>p.x>=0&&p.y>=0&&p.x<Width&&p.y<Height;
        public char Terrain(Cell p)=>Inside(p)?data.rows[p.y][p.x]:' ';
        public bool Floor(Cell p)=>Terrain(p)=='.';
        public LevelData Export()=>data.Copy();
        public Definition(LevelData source,bool validate=true):this(source,validate,false){}
        public static Definition Preview(LevelData source)=>new Definition(source,false,true);
        private Definition(LevelData source,bool validate,bool draft)
        {
            string error=draft?DraftError(source):StructuralError(source);
            if(error!=null)throw new ArgumentException(error);
            data=source.Copy();
            HasLamp=Array.Exists(data.pieces,p=>p.kind==PieceKind.Lamp);
            doorAt=new int[Width*Height];for(int i=0;i<doorAt.Length;i++)doorAt[i]=-1;for(int i=0;i<data.doors.Length;i++)doorAt[data.doors[i].cell.y*Width+data.doors[i].cell.x]=i;
            if(validate&&Initial().Dead)throw new ArgumentException("出生点受光，请移动角色或调整遮挡。");
        }
        public State Initial()=>new State(this,data.player,data.pieces.Select(p=>p.cell).ToArray(),data.sunEnabled,data.sunFrom,Command.Right);
        public static string StructuralError(LevelData d)
        {
            string error=DraftError(d);if(error!=null)return error;
            if(d.player==new Cell(-1,-1))return "请放置玩家。";
            if(d.coffin==new Cell(-1,-1))return "请放置棺材。";
            var plates=d.plates??Array.Empty<DeviceData>();var doors=d.doors??Array.Empty<DeviceData>();
            if(plates.Any(p=>!doors.Any(g=>g.channel==p.channel))||doors.Any(g=>!plates.Any(p=>g.channel==p.channel)))return "压力板与自动门需要有对应的连接组。";
            return null;
        }
        // Incomplete roles are valid authoring state; unsafe geometry never reaches a renderer.
        public static string DraftError(LevelData d)
        {
            if(d==null||d.schemaVersion!=1)return "不支持的关卡格式。";
            if(d.rows==null||d.rows.Length<3||d.rows.Length>32||d.rows[0]==null||d.rows[0].Length<3||d.rows[0].Length>32)return "棋盘尺寸应为 3–32 格。";
            int w=d.rows[0].Length;
            if(d.rows.Any(r=>r==null||r.Length!=w||r.Any(c=>c!='.'&&c!='#'&&c!=' ')))return "地形必须是等宽的地板、墙或空白。";
            if(!Enum.IsDefined(typeof(SunSide),d.sunFrom))return "无效的日照方向。";
            Func<Cell,bool> floor=p=>p.x>=0&&p.y>=0&&p.x<w&&p.y<d.rows.Length&&d.rows[p.y][p.x]=='.';
            bool player=d.player!=new Cell(-1,-1),coffin=d.coffin!=new Cell(-1,-1);
            if(player&&!floor(d.player))return "请把玩家放在地板上。";
            if(coffin&&!floor(d.coffin))return "请把棺材放在地板上。";
            if(player&&coffin&&d.player==d.coffin)return "角色出生点不能与棺材重叠。";
            if(d.pieces==null||d.pieces.Length>128)return "物件数据无效或过多。";
            var cells=new HashSet<Cell>();if(player)cells.Add(d.player);if(coffin)cells.Add(d.coffin);var ids=new HashSet<string>();
            foreach(var p in d.pieces)
            {
                if(p==null||string.IsNullOrEmpty(p.id)||!ids.Add(p.id)||!Enum.IsDefined(typeof(PieceKind),p.kind))return "物件类型或标识无效。";
                if(!floor(p.cell)||!cells.Add(p.cell))return "物件必须在空地板上，不能重叠。";
            }
            var devices=new HashSet<Cell>();
            foreach(var group in new[]{d.plates??Array.Empty<DeviceData>(),d.doors??Array.Empty<DeviceData>()})
            {if(group.Length>32)return "每种机关最多放置 32 个。";foreach(var item in group)if(item==null||item.channel<0||item.channel>3||!floor(item.cell)||item.cell==d.coffin||!devices.Add(item.cell))return "机关需要独立地板格与有效连接组，不能覆盖棺材。";}
            return null;
        }
    }
    public sealed class State
    {
        public Definition Level {get;}
        public Cell Player {get;}
        private readonly Cell[] positions;
        private readonly bool[] openDoors;
        public Cell Position(int i)=>positions[i];
        public bool SunEnabled {get;}
        public SunSide SunFrom {get;}
        public Command Facing {get;}
        public bool Won=>!Dead&&Player==Level.Coffin&&Level.Floor(Player);
        public bool Dead {get;}
        public bool PlatePressed(int index){var p=Level.PlateCell(index);return Player==p||At(p)>=0;}
        public bool DoorOpen(int index)=>openDoors[index];
        public bool ClosedDoor(Cell p){int i=Level.DoorAt(p);return i>=0&&!openDoors[i];}
        public bool Passable(Cell p)=>Level.Floor(p)&&!ClosedDoor(p);
        public State(Definition d,Cell p,Cell[] a,bool sun,SunSide side,Command facing)
        {
            Level=d;Player=p;positions=(Cell[])a.Clone();SunEnabled=sun;SunFrom=side;Facing=facing;
            var pressed=new bool[4];for(int i=0;i<d.PlateCount;i++)if(PlatePressed(i))pressed[d.PlateChannel(i)]=true;
            openDoors=new bool[d.DoorCount];for(int i=0;i<openDoors.Length;i++){var cell=d.DoorCell(i);openDoors[i]=pressed[d.DoorChannel(i)]||Player==cell||At(cell)>=0;}
            Dead=(sun||d.HasLamp)&&d.Floor(p)&&!LightField.Compute(this).Safe(p);
        }
        internal Cell[] CopyPositions()=>(Cell[])positions.Clone();
        public int At(Cell p){for(int i=0;i<positions.Length;i++)if(positions[i]==p)return i;return -1;}
        public string Key()=>Player+"|"+string.Join(";",positions.Select(p=>p.ToString()))+"|"+SunEnabled+"|"+SunFrom;
    }
    public sealed class LightField
    {
        public readonly bool[] Sun,Lamp;
        public readonly byte[] LampAxes;
        public readonly byte[] ReflectionEdges;
        private readonly Definition level;
        private LightField(Definition d){level=d;Sun=new bool[d.Width*d.Height];Lamp=new bool[Sun.Length];LampAxes=new byte[Sun.Length];ReflectionEdges=new byte[Sun.Length];}
        public bool Lit(Cell p)=>level.Inside(p)&&(Sun[p.y*level.Width+p.x]||Lamp[p.y*level.Width+p.x]);
        public bool Safe(Cell p)=>!Lit(p);
        public static readonly Cell[] Directions={new Cell(0,-1),new Cell(1,0),new Cell(0,1),new Cell(-1,0)};
        public static readonly Cell[] SunTravel={new Cell(1,0),new Cell(0,1),new Cell(-1,0),new Cell(0,-1)};
        public static LightField Compute(State s)
        {
            var d=s.Level;var f=new LightField(d);
            Func<Cell,bool> opaque=p=>d.Terrain(p)=='#'||s.At(p)>=0||s.ClosedDoor(p);
            if(s.SunEnabled)
            {
                for(int y=0;y<d.Height;y++)for(int x=0;x<d.Width;x++)f.Sun[y*d.Width+x]=d.Floor(new Cell(x,y));
                var travel=SunTravel[(int)s.SunFrom];
                for(int y=0;y<d.Height;y++)for(int x=0;x<d.Width;x++)
                {
                    var p=new Cell(x,y);if(!opaque(p))continue;
                    for(int n=0;n<2;n++){p+=travel;if(d.Inside(p))f.Sun[p.y*d.Width+p.x]=false;}
                }
            }
            // A directed cell can only contribute the same remaining ray once per source.
            // This also bounds reflected loops independently of level layout.
            var visited=new byte[f.Sun.Length];
            for(int i=0;i<d.PieceCount;i++)if(d.Kind(i)==PieceKind.Lamp)
            {
                var start=s.Position(i);f.Lamp[start.y*d.Width+start.x]=true;f.LampAxes[start.y*d.Width+start.x]=3;
                Array.Clear(visited,0,visited.Length);
                for(int seed=0;seed<4;seed++)
                {
                    var p=start;int direction=seed;
                    while(true)
                    {
                        p+=Directions[direction];if(!d.Inside(p))break;int at=p.y*d.Width+p.x;
                        byte flag=(byte)(1<<direction);if((visited[at]&flag)!=0)break;visited[at]|=flag;
                        if(d.Terrain(p)=='#'||s.ClosedDoor(p))break;
                        int piece=s.At(p);
                        if(piece>=0)
                        {
                            var kind=d.Kind(piece);if(kind!=PieceKind.MirrorSlash&&kind!=PieceKind.MirrorBackslash)break;
                            int next=kind==PieceKind.MirrorSlash?direction^1:3-direction;
                            f.ReflectionEdges[at]|=(byte)((1<<((direction+2)%4))|(1<<next));direction=next;continue;
                        }
                        if(d.Floor(p)){f.Lamp[at]=true;f.LampAxes[at]|=(byte)(direction%2==0?2:1);}
                    }
                }
            }
            return f;
        }
    }
    public enum ActionKind { Moved,Pushed,Blocked,Undone,Reset,NoChange,Died }
    public enum BlockReason { None,Terrain,Chain,Coffin,Light,Finished,Door }
    public sealed class ActionResult
    {
        public ActionKind Kind; public BlockReason Reason; public State Before,After;public Command Command;
        public int PushedCount;public Cell RejectedCell;
        public bool Changed=>Kind!=ActionKind.Blocked&&Kind!=ActionKind.NoChange;
    }
    public static class Rules
    {
        public static ActionResult Move(State s,Command cmd)
        {
            var r=new ActionResult{Before=s,After=s,Command=cmd,Kind=ActionKind.Blocked};
            if(s.Won||s.Dead){r.Reason=BlockReason.Finished;return r;}
            if(cmd<Command.Up||cmd>Command.Left){r.Kind=ActionKind.NoChange;return r;}
            var dir=LightField.Directions[(int)cmd];var target=s.Player+dir;r.RejectedCell=target;
            if(!s.Level.Floor(target)){r.Reason=BlockReason.Terrain;return r;}
            if(s.ClosedDoor(target)){r.Reason=BlockReason.Door;return r;}
            var chain=new List<int>();var at=target;int index;
            while((index=s.At(at))>=0){chain.Add(index);at+=dir;}
            if(chain.Count>0&&(!s.Passable(at)||at==s.Level.Coffin))
            {r.Reason=at==s.Level.Coffin?BlockReason.Coffin:s.ClosedDoor(at)?BlockReason.Door:BlockReason.Chain;return r;}
            var positions=s.CopyPositions();foreach(int i in chain)positions[i]+=dir;
            var next=new State(s.Level,target,positions,s.SunEnabled,s.SunFrom,cmd);
            r.After=next;r.Kind=next.Dead?ActionKind.Died:chain.Count==0?ActionKind.Moved:ActionKind.Pushed;r.Reason=next.Dead?BlockReason.Light:BlockReason.None;r.PushedCount=chain.Count;return r;
        }
    }
    public sealed class Session
    {
        private readonly Stack<State> history=new Stack<State>();
        private readonly Stack<int> moveHistory=new Stack<int>();
        public State State {get;private set;}
        public int UndoCount=>history.Count;
        public int Moves {get;private set;}
        public Session(Definition d){State=d.Initial();}
        public string WinningRoute()
        {
            if(!State.Won||Moves==0)return null;
            var route=new char[Moves];int i=Moves-1;route[i]=LevelSolver.Directions[(int)State.Facing];
            // After Undo the abandoned branch is already gone. Moves stops at the last Reset.
            foreach(var previous in history){if(i==0)break;route[--i]=LevelSolver.Directions[(int)previous.Facing];}
            return new string(route);
        }
        public ActionResult Apply(Command cmd)
        {
            var before=State;
            if(cmd==Command.Undo)
            {
                if(history.Count==0)return new ActionResult{Kind=ActionKind.NoChange,Before=before,After=before,Command=cmd};
                State=history.Pop();Moves=moveHistory.Pop();
                return new ActionResult{Kind=ActionKind.Undone,Before=before,After=State,Command=cmd};
            }
            if(cmd==Command.Reset)
            {
                var initial=State.Level.Initial();if(initial.Key()==State.Key())return new ActionResult{Kind=ActionKind.NoChange,Before=before,After=before,Command=cmd};
                history.Push(State);moveHistory.Push(Moves);State=initial;Moves=0;
                return new ActionResult{Kind=ActionKind.Reset,Before=before,After=State,Command=cmd};
            }
            var result=Rules.Move(State,cmd);
            if(result.Changed){history.Push(State);moveHistory.Push(Moves);State=result.After;Moves++;}return result;
        }
    }
}
