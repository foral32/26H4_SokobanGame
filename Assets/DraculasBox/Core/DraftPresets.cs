using System;
using System.Collections.Generic;
using System.Linq;

namespace DraculasBox.Core
{
    // Authored rooms. Their routes are independently BFS-checked by PresetWorkshop;
    // rotation/reflection changes presentation, never the number of distinct designs.
    public sealed class DraftPreset
    {
        public string Id { get; }
        public string Title { get; }
        public DraftMechanic Mechanics { get; }
        public DraftComplexity Complexity { get; }
        public string Difficulty => new[]{"Ⅰ","Ⅱ","Ⅲ"}[(int)Complexity];
        private readonly string[] map;
        private readonly string route;
        internal DraftPreset(string id,string title,DraftMechanic mechanics,DraftComplexity complexity,string route,params string[] map)
        {Id=id;Title=title;Mechanics=mechanics;Complexity=complexity;this.route=route;this.map=map;}

        public LevelData Create(int orientation=0)
        {
            if(orientation<0||orientation>7)throw new ArgumentOutOfRangeException(nameof(orientation));
            int turn=orientation%4;bool flip=orientation>=4;
            Cell Vector(Cell p)
            {
                if(flip)p.y=-p.y;
                for(int i=0;i<turn;i++)p=new Cell(-p.y,p.x);
                return p;
            }
            var corners=new[]{Vector(new Cell(0,0)),Vector(new Cell(map[0].Length-1,0)),Vector(new Cell(0,map.Length-1)),Vector(new Cell(map[0].Length-1,map.Length-1))};
            int minX=corners.Min(c=>c.x),minY=corners.Min(c=>c.y);
            int width=corners.Max(c=>c.x)-minX+1,height=corners.Max(c=>c.y)-minY+1;
            Cell Place(Cell p)=>Vector(p)+new Cell(-minX,-minY);
            var rows=Enumerable.Range(0,height).Select(_=>new char[width]).ToArray();
            var pieces=new List<EntityData>();var plates=new List<DeviceData>();var doors=new List<DeviceData>();
            var level=new LevelData{id=Guid.NewGuid().ToString("N"),title=Title,hint="",sunEnabled=Mechanics.HasFlag(DraftMechanic.Sun),
                sunFrom=(SunSide)Array.IndexOf(LightField.SunTravel,Vector(new Cell(1,0)))};
            for(int y=0;y<map.Length;y++)for(int x=0;x<map[y].Length;x++)
            {
                char symbol=map[y][x];var p=Place(new Cell(x,y));rows[p.y][p.x]=symbol=='#'?'#':'.';
                if(symbol=='@')level.player=p;else if(symbol=='C')level.coffin=p;
                else if(symbol=='p'||symbol=='D')(symbol=='p'?plates:doors).Add(new DeviceData{cell=p,channel=0});
                else if("BL/\\".IndexOf(symbol)>=0)
                {
                    if((symbol=='/'||symbol=='\\')&&(flip!=(turn%2==1)))symbol=symbol=='/'?'\\':'/';
                    pieces.Add(new EntityData{id="piece-"+pieces.Count,cell=p,kind=(PieceKind)"BL/\\".IndexOf(symbol)});
                }
            }
            level.rows=rows.Select(row=>new string(row)).ToArray();level.pieces=pieces.ToArray();level.plates=plates.ToArray();level.doors=doors.ToArray();
            level.solution=new string(route.Select(key=>LevelSolver.Directions[Array.IndexOf(LightField.Directions,Vector(LightField.Directions[LevelSolver.Directions.IndexOf(key)]))]).ToArray());
            return level;
        }
    }

    public static class DraftPresets
    {
        public static IReadOnlyList<DraftPreset> All { get; }=Array.AsReadOnly(new[]{
            // 先绕到箱子侧面，再给出口让位。
            new DraftPreset("room_01","让位",(DraftMechanic)0,(DraftComplexity)0,"RUULUU",
                "#######",
                "###C###",
                "###.###",
                "#..B..#",
                "#..#..#",
                "#..@..#",
                "#######"),
            // 两箱连推让出中央入口，保留两侧站位。
            new DraftPreset("room_02","并推",(DraftMechanic)0,(DraftComplexity)0,"RUURUU",
                "#########",
                "####C####",
                "####.####",
                "#...BB..#",
                "#.#..#..#",
                "#.@.....#",
                "#########"),
            // 可先借侧室挪箱，再将两箱推至同一侧；保留另一条等长解。
            new DraftPreset("room_03","借位",(DraftMechanic)0,(DraftComplexity)1,"RUULURRU",
                "#########",
                "####C####",
                "#...B...#",
                "#..B##..#",
                "#.#...#.#",
                "#.@.....#",
                "#########"),
            // 先退回墙影，再把箱影移至相邻一排。
            new DraftPreset("room_04","转荫",(DraftMechanic)1,(DraftComplexity)0,"LLULDRDR",
                "########",
                "#......#",
                "#..B@..#",
                "#..C...#",
                "#......#",
                "########"),
            // 一箱负责向下开路，一箱留下横向换位的安全格。
            new DraftPreset("room_05","接影",(DraftMechanic)1,(DraftComplexity)2,"LLRULLULDRDRDR",
                "#########",
                "#....#..#",
                "#..B....#",
                "#...B@..#",
                "#...C...#",
                "#.......#",
                "#########"),
            // 从下方阴影绕到箱后，将第二箱推向上方出口。
            new DraftPreset("room_06","向阳台",(DraftMechanic)1,(DraftComplexity)2,"LULRDLLDLURURUR",
                "#########",
                "#...C...#",
                "#..B....#",
                "#...B@..#",
                "#.......#",
                "#.......#",
                "#########"),
            // 隔箱将灯推离出口所在的横向光线。
            new DraftPreset("room_07","灯龛",(DraftMechanic)2,(DraftComplexity)0,"RURURU",
                "########",
                "#...#C##",
                "#..L...#",
                "#..B...#",
                "#.@....#",
                "#......#",
                "########"),
            // 沿柱边推灯，再绕至新的安全一侧。
            new DraftPreset("room_08","绕灯",(DraftMechanic)2,(DraftComplexity)1,"UURRUUR",
                "#########",
                "#....C..#",
                "#..#....#",
                "#..BL...#",
                "#..#....#",
                "#.@.....#",
                "#########"),
            // 借左侧空间搬箱，避开第二盏灯的射线，从侧面推开第一盏灯。
            new DraftPreset("room_09","双灯庭",(DraftMechanic)2,(DraftComplexity)2,"RRULDLUULURRRUR",
                "#########",
                "#....C..#",
                "#..L....#",
                "#..#..L.#",
                "#..B....#",
                "#.@.....#",
                "#########"),
            // 箱子留在压力板上，人转身进入侧室。
            new DraftPreset("room_10","留门",(DraftMechanic)4,(DraftComplexity)0,"RUUURRR",
                "#########",
                "#..p#...#",
                "#...D.C.#",
                "#.#.#####",
                "#..B....#",
                "#.@.....#",
                "#########"),
            // 箱子先左移再上推；站位和板的位置分处柱的两边。
            new DraftPreset("room_11","绕柱开门",(DraftMechanic)4,(DraftComplexity)1,"RRULDLUURULRRRR",
                "#########",
                "#...#...#",
                "#p..D.C.#",
                "#...#...#",
                "#..B.####",
                "#.@..####",
                "#########"),
            // 先移动门口的箱子，绕到右室后将另一箱运回左侧压板。
            new DraftPreset("room_12","双室接手",(DraftMechanic)4,(DraftComplexity)2,"UURRDRRULLLLLDLUUURRRRR",
                "#########",
                "#p..#...#",
                "#...D.C.#",
                "#.#.#####",
                "#..B.B..#",
                "#...#...#",
                "#..@#...#",
                "#########"),
            // 从不受光的一侧推镜，将照着终点的反射光移走。
            new DraftPreset("room_13","移光",(DraftMechanic)10,(DraftComplexity)0,"ULUU",
                "########",
                "#..C...#",
                "#......#",
                "#L./...#",
                "#...@..#",
                "#......#",
                "########"),
            // 用下方的镜子截走上游光线，使原来的反射通道熄灭。
            new DraftPreset("room_14","截光",(DraftMechanic)10,(DraftComplexity)1,"URRRUUULL",
                "#######",
                "###C..#",
                "#.....#",
                "#L./#.#",
                "#./...#",
                "#.@...#",
                "#######"),
            // 先移箱借出推镜站位，挪开反射线后把箱子送回压力板。
            new DraftPreset("room_15","镜室开门",(DraftMechanic)14,(DraftComplexity)2,"UUURDRDLLUURRU",
                "#########",
                "####C####",
                "#...D####",
                "#..B/L..#",
                "#p......#",
                "#.@.....#",
                "#########")
        });
    }
}
