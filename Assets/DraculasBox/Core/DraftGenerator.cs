using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DraculasBox.Core
{
    [Flags] public enum DraftMechanic { None=0, Sun=1, Lamp=2, Circuit=4, Mirror=8 }
    public enum DraftComplexity { Intro, Standard, Advanced }

    public static class DraftGenerator
    {
        // s/g are room connectors. A room carries a witness that is checked again after
        // assembly: a neighbouring lamp, an open door or a new wall can change exposure.
        private sealed class Room
        {
            public readonly string route;
            public readonly string[] rows;
            public Room(string route, params string[] rows){this.route=route;this.rows=rows;}
        }
        private static readonly Room shortTurn=new Room("RRDRR","s.B.#","##..g");
        private static readonly Room longTurn=new Room("RRRDRR","s.B..#","###..g");
        private static readonly Room sidePocket=new Room("RRURDRRRDR","##..####","s..B...#","###.##.g");
        private static readonly Room shortShadow=new Room("RRDRR","###.#","s.B.#","##..g");
        private static readonly Room longShadow=new Room("RRRDRR","####.#","s.B..#","###..g");
        private static readonly Room lampCorner=new Room("RRDRDRRR","s.BL.##","##..###","###...g");
        private static readonly Room lampPass=new Room("RRRDRDRRR","s.B.L.##","###..###","####...g");
        private static readonly Room plateNook=new Room("RRURDRR","##.Bp#","s...Dg");
        private static readonly Room plateGallery=new Room("RRURRDRR","##.B.p#","s....Dg");
        private static readonly Room mirrorTurn=new Room("RRRRDRR","#####L#","s..\\..#","####..g");
        private static readonly Room mirrorNook=new Room("RRRDRR","####L#","s.\\..#","###..g");

        private readonly struct Pose
        {
            private readonly int turn;
            private readonly bool flip;
            public Pose(int turn,bool flip){this.turn=turn;this.flip=flip;}
            public Cell Apply(Cell p)
            {
                if(flip)p.y=-p.y;
                for(int i=0;i<turn;i++)p=new Cell(-p.y,p.x);
                return p;
            }
            public char Move(char key)
            {
                var direction=Apply(LightField.Directions[LevelSolver.Directions.IndexOf(key)]);
                return LevelSolver.Directions[Array.IndexOf(LightField.Directions,direction)];
            }
            public char Symbol(char c)
            {
                if((c=='/'||c=='\\')&&(flip!=(turn%2==1)))return c=='/'?'\\':'/';
                return c;
            }
        }

        public static LevelData Generate(DraftMechanic allowed, DraftComplexity complexity, int seed, CancellationToken token=default)
        {
            if((allowed&~(DraftMechanic)15)!=0||!Enum.IsDefined(typeof(DraftComplexity),complexity))throw new ArgumentOutOfRangeException();
            if(allowed.HasFlag(DraftMechanic.Mirror))allowed|=DraftMechanic.Lamp;
            var random=new Random(seed);
            var required=new List<DraftMechanic>();
            if(allowed.HasFlag(DraftMechanic.Sun))required.Add(DraftMechanic.Sun);
            if(allowed.HasFlag(DraftMechanic.Circuit))required.Add(DraftMechanic.Circuit);
            if(allowed.HasFlag(DraftMechanic.Mirror))required.Add(DraftMechanic.Mirror);
            else if(allowed.HasFlag(DraftMechanic.Lamp))required.Add(DraftMechanic.Lamp);
            if(required.Count==0)required.Add(DraftMechanic.None);
            var extras=new List<DraftMechanic>(required){DraftMechanic.None};
            if(allowed.HasFlag(DraftMechanic.Lamp))extras.Add(DraftMechanic.Lamp);

            for(int attempt=0;attempt<40;attempt++)
            {
                token.ThrowIfCancellationRequested();
                bool straight=attempt>=28;
                var sequence=new List<DraftMechanic>(required);
                for(int i=0;i<(int)complexity;i++)sequence.Add(straight?DraftMechanic.None:extras[random.Next(extras.Count)]);
                for(int i=sequence.Count-1;i>0;i--){int j=random.Next(i+1);var swap=sequence[i];sequence[i]=sequence[j];sequence[j]=swap;}
                var data=Assemble(sequence,random,straight,allowed.HasFlag(DraftMechanic.Sun));
                if(data==null||!LevelSolver.Proves(data,data.solution,out int pushes))continue;
                var result=LevelSolver.Solve(data,token,6500,350);
                if(result.Outcome==SearchOutcome.Solved){data.solution=result.Route;pushes=result.Pushes;}
                int grade=(int)complexity;
                if(pushes>=grade+1&&data.solution.Length>=4+grade*5&&UsesMechanisms(data,allowed))return data;
            }
            throw new InvalidOperationException("未找到符合选项的草稿，请再生成一次。");
        }

        private static bool UsesMechanisms(LevelData level,DraftMechanic requested)
        {
            var used=DraftMechanic.None;
            var state=new Definition(level).Initial();
            foreach(char key in level.solution)
            {
                var next=Rules.Move(state,(Command)LevelSolver.Directions.IndexOf(key)).After;
                if(next.Level.DoorAt(next.Player)>=0)used|=DraftMechanic.Circuit;
                for(int i=0;i<level.pieces.Length;i++)
                {
                    if(state.Position(i)==next.Position(i))continue;
                    var kind=level.pieces[i].kind;
                    if(kind==PieceKind.Lamp)used|=DraftMechanic.Lamp;
                    if((kind==PieceKind.MirrorSlash||kind==PieceKind.MirrorBackslash)&&!used.HasFlag(DraftMechanic.Mirror))
                    {
                        var p=next.Position(i);
                        if(LightField.Compute(next).ReflectionEdges[p.y*next.Level.Width+p.x]!=0)used|=DraftMechanic.Mirror|DraftMechanic.Lamp;
                    }
                }
                if(level.sunEnabled&&!used.HasFlag(DraftMechanic.Sun))
                {
                    var travel=LightField.SunTravel[(int)level.sunFrom];
                    var a=new Cell(next.Player.x-travel.x,next.Player.y-travel.y);
                    var b=new Cell(a.x-travel.x,a.y-travel.y);
                    if(next.Level.Terrain(a)!='#'&&next.Level.Terrain(b)!='#'&&!next.ClosedDoor(a)&&!next.ClosedDoor(b)
                        &&(next.At(a)>=0||next.At(b)>=0))used|=DraftMechanic.Sun;
                }
                if((used&requested)==requested)return true;
                state=next;
            }
            return false;
        }

        private static Room Choose(DraftMechanic mechanic, Random random, bool simple)
        {
            bool alternate=!simple&&random.Next(2)==0;
            switch(mechanic)
            {
                case DraftMechanic.Sun:return alternate?longShadow:shortShadow;
                case DraftMechanic.Circuit:return alternate?plateGallery:plateNook;
                case DraftMechanic.Lamp:return alternate?lampPass:lampCorner;
                case DraftMechanic.Mirror:return alternate?mirrorNook:mirrorTurn;
                default:return !simple&&random.Next(3)==0?sidePocket:alternate?longTurn:shortTurn;
            }
        }

        private static LevelData Assemble(List<DraftMechanic> sequence, Random random, bool straight, bool sun)
        {
            var tiles=new Dictionary<Cell,(char symbol,int group)>();
            var path=new List<char>();
            var entrance=new Cell(0,0);var exit=entrance;
            int channel=0,heading=0;
            foreach(var mechanic in sequence)
            {
                var room=Choose(mechanic,random,straight);
                if(!straight&&!sun&&path.Count>0&&random.Next(3)==0)heading=(heading+(random.Next(2)==0?1:3))%4;
                var pose=new Pose(heading,!straight&&mechanic!=DraftMechanic.Sun&&random.Next(2)==0);
                var origin=exit;
                int startY=Array.FindIndex(room.rows,row=>row[0]=='s');
                for(int y=0;y<room.rows.Length;y++)
                for(int x=0;x<room.rows[y].Length;x++)
                {
                    char symbol=room.rows[y][x];
                    if(symbol=='#')continue;
                    var p=origin+pose.Apply(new Cell(x,y-startY));
                    if(tiles.ContainsKey(p)&&symbol!='s')return null;
                    if(symbol=='g')exit=p;
                    tiles[p]=(pose.Symbol(symbol),channel);
                }
                foreach(char key in room.route)path.Add(pose.Move(key));
                if(mechanic==DraftMechanic.Circuit)channel++;
            }
            if(!straight)
            {
                // Open alternate approaches between existing corridors. The final solve
                // rejects shortcuts that would bypass the selected puzzle interactions.
                int links=random.Next(sequence.Count+1);
                for(int i=0;i<links;i++)
                {
                    var neighbours=new Dictionary<Cell,int>();
                    foreach(var cell in tiles.Keys)
                    foreach(var d in LightField.Directions)
                    {
                        var p=cell+d;if(tiles.ContainsKey(p))continue;
                        neighbours.TryGetValue(p,out int count);neighbours[p]=count+1;
                    }
                    var linksToOpen=neighbours.Where(pair=>pair.Value==2).Select(pair=>pair.Key).ToArray();
                    if(linksToOpen.Length==0)break;
                    tiles.Add(linksToOpen[random.Next(linksToOpen.Length)],('.',0));
                }
                // Vary the entry and the final approach without opening a second shortcut.
                for(int i=0,count=random.Next(4);i<count;i++)
                {
                    bool start=random.Next(2)==0;
                    var end=start?entrance:exit;
                    var directions=Enumerable.Range(0,4).Where(d=>
                    {
                        var p=end+LightField.Directions[d];
                        return !tiles.ContainsKey(p)&&LightField.Directions.Count(n=>tiles.ContainsKey(p+n))==1;
                    }).ToArray();
                    if(directions.Length==0)continue;
                    int direction=directions[random.Next(directions.Length)];
                    var cell=end+LightField.Directions[direction];
                    tiles.Add(cell,('.',0));
                    if(start){entrance=cell;path.Insert(0,LevelSolver.Directions[(direction+2)%4]);}
                    else{exit=cell;path.Add(LevelSolver.Directions[direction]);}
                }
            }
            var whole=new Pose(random.Next(4),random.Next(2)==0);
            var cells=tiles.Keys.Select(whole.Apply).ToArray();
            int minX=cells.Min(c=>c.x)-1,minY=cells.Min(c=>c.y)-1;
            int width=cells.Max(c=>c.x)-minX+2,height=cells.Max(c=>c.y)-minY+2;
            if(width>32||height>32)return null;
            Cell Place(Cell p)=>whole.Apply(p)+new Cell(-minX,-minY);
            var pieces=new List<EntityData>();var plates=new List<DeviceData>();var doors=new List<DeviceData>();
            var rows=new char[height][];
            for(int i=0;i<height;i++)rows[i]=new string('#',width).ToCharArray();
            foreach(var tile in tiles)
            {
                var p=Place(tile.Key);rows[p.y][p.x]='.';
                char symbol=whole.Symbol(tile.Value.symbol);
                if(symbol=='p'||symbol=='D')
                    (symbol=='p'?plates:doors).Add(new DeviceData{cell=p,channel=tile.Value.group});
                else if(symbol=='B'||symbol=='L'||symbol=='/'||symbol=='\\')
                {
                    var kind=symbol=='B'?PieceKind.Box:symbol=='L'?PieceKind.Lamp:symbol=='/'?PieceKind.MirrorSlash:PieceKind.MirrorBackslash;
                    pieces.Add(new EntityData{id="piece-"+pieces.Count,kind=kind,cell=p});
                }
            }
            return new LevelData{schemaVersion=1,id=Guid.NewGuid().ToString("N"),title="生成草稿",hint="",
                rows=rows.Select(row=>new string(row)).ToArray(),player=Place(entrance),coffin=Place(exit),
                pieces=pieces.ToArray(),plates=plates.ToArray(),doors=doors.ToArray(),sunEnabled=sun,
                sunFrom=(SunSide)Array.IndexOf(LightField.SunTravel,whole.Apply(new Cell(0,1))),
                solution=new string(path.Select(whole.Move).ToArray())};
        }
    }
}
