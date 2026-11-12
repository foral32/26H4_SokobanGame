using System.Collections.Generic;
namespace DraculasBox.Core
{
    public static partial class DraftPresets
    {
        // Reviewed compact rooms. Sources and grading notes: Document/Design/草稿预设.md.
        private static IEnumerable<DraftPreset> Expansion()
        {
            // box:c3_b02
            yield return new DraftPreset("room_16","交错让位",(DraftMechanic)0,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"##########","#...#....#","#........#","#........#","#...#....#","#...#....#","#####....#","#######.##","##########"},
                player=new Cell(2,1),coffin=new Cell(7,7),sunEnabled=false,sunFrom=(SunSide)0,
                solution="RDRRRRDRDDDLD",
                pieces=new EntityData[]{new EntityData{id="gatebox0",kind=(PieceKind)0,cell=new Cell(4,2)},new EntityData{id="gatebox2",kind=(PieceKind)0,cell=new Cell(6,6)},new EntityData{id="gatebox4",kind=(PieceKind)0,cell=new Cell(7,6)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // expansion:candidate_12
            yield return new DraftPreset("room_17","向光让位",(DraftMechanic)1,(DraftComplexity)1,new LevelData
            {
                rows=new[]{"#########","####.####","#.......#","#...##..#","#.#...#.#","#.......#","#########"},
                player=new Cell(2,5),coffin=new Cell(4,1),sunEnabled=true,sunFrom=(SunSide)1,
                solution="RUULURRU",
                pieces=new EntityData[]{new EntityData{id="piece-0",kind=(PieceKind)0,cell=new Cell(4,2)},new EntityData{id="piece-1",kind=(PieceKind)0,cell=new Cell(3,3)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // partition:part_402
            yield return new DraftPreset("room_18","窗下推灯",(DraftMechanic)3,(DraftComplexity)0,new LevelData
            {
                rows=new[]{"########","###.#.##","###....#","##.....#","##..#.##","###.####","########"},
                player=new Cell(2,4),coffin=new Cell(5,1),sunEnabled=true,sunFrom=(SunSide)1,
                solution="RURURU",
                pieces=new EntityData[]{new EntityData{id="piece-0",kind=(PieceKind)1,cell=new Cell(3,2)},new EntityData{id="piece-1",kind=(PieceKind)0,cell=new Cell(3,3)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // expansion:candidate_124
            yield return new DraftPreset("room_19","窗边换手",(DraftMechanic)3,(DraftComplexity)1,new LevelData
            {
                rows=new[]{"##########","#####....#","#####...##","###.....##","###.....##","######..##","##########","##########"},
                player=new Cell(6,2),coffin=new Cell(8,1),sunEnabled=true,sunFrom=(SunSide)3,
                solution="LDLDRURURUR",
                pieces=new EntityData[]{new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(6,3)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(5,3)},new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(6,4)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // expansion:candidate_106
            yield return new DraftPreset("room_20","双光归途",(DraftMechanic)3,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"########","###..###","###...##","###....#","###....#","###...##","####.###","########"},
                player=new Cell(5,3),coffin=new Cell(5,5),sunEnabled=true,sunFrom=(SunSide)0,
                solution="ULULDDURDDRD",
                pieces=new EntityData[]{new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(4,3)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(3,2)},new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(4,4)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // sun:sun_2815
            yield return new DraftPreset("room_21","日光门廊",(DraftMechanic)5,(DraftComplexity)0,new LevelData
            {
                rows=new[]{"#########","##..#...#","#.......#","#.#.#####","#.......#","#.......#","#########"},
                player=new Cell(2,5),coffin=new Cell(6,2),sunEnabled=true,sunFrom=(SunSide)1,
                solution="RUUURRR",
                pieces=new EntityData[]{new EntityData{id="piece-0",kind=(PieceKind)0,cell=new Cell(3,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(3,1)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,2)}},
            });
            // sun:sun_924
            yield return new DraftPreset("room_22","侧室压板",(DraftMechanic)5,(DraftComplexity)1,new LevelData
            {
                rows=new[]{"#########","#...#...#","#.......#","#.#.#####","#.......#","#....#..#","#########"},
                player=new Cell(7,4),coffin=new Cell(6,2),sunEnabled=true,sunFrom=(SunSide)1,
                solution="LLLDLUUURRR",
                pieces=new EntityData[]{new EntityData{id="piece-0",kind=(PieceKind)0,cell=new Cell(5,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(3,1)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,2)}},
            });
            // sun:sun_1964
            yield return new DraftPreset("room_23","隔庭送箱",(DraftMechanic)5,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"#########","#.#.#...#","#.......#","#.#.#####","#.......#","#...#...#","#...#...#","#########"},
                player=new Cell(2,4),coffin=new Cell(6,2),sunEnabled=true,sunFrom=(SunSide)1,
                solution="DRURRDRRULLLLLDLUUURRRRR",
                pieces=new EntityData[]{new EntityData{id="piece-0",kind=(PieceKind)0,cell=new Cell(3,4)},new EntityData{id="piece-1",kind=(PieceKind)0,cell=new Cell(5,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(1,1)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,2)}},
            });
            // expansion:candidate_170
            yield return new DraftPreset("room_24","灯下留门",(DraftMechanic)6,(DraftComplexity)0,new LevelData
            {
                rows=new[]{"#########","#.......#","#....####","#.......#","#....####","#.......#","#########"},
                player=new Cell(2,5),coffin=new Cell(7,3),sunEnabled=false,sunFrom=(SunSide)0,
                solution="URRURRR",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(2,3)},new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(4,3)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(2,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(2,2)},new DeviceData{channel=0,cell=new Cell(4,3)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(6,3)}},
            });
            // expansion:candidate_180
            yield return new DraftPreset("room_25","双灯接手",(DraftMechanic)6,(DraftComplexity)1,new LevelData
            {
                rows=new[]{"##########","####.#.###","####.#.###","#........#","#......###","#....#####","##########"},
                player=new Cell(2,4),coffin=new Cell(8,3),sunEnabled=false,sunFrom=(SunSide)0,
                solution="RDRULURRDRURR",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(4,1)},new EntityData{id="M",kind=(PieceKind)1,cell=new Cell(6,1)},new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(4,3)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(3,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,3)},new DeviceData{channel=0,cell=new Cell(6,3)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,2)},new DeviceData{channel=0,cell=new Cell(6,2)},new DeviceData{channel=0,cell=new Cell(7,3)}},
            });
            // expansion:candidate_185
            yield return new DraftPreset("room_26","分室接力",(DraftMechanic)6,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"##########","#...#....#","#........#","#........#","#...#....#","#...#....#","#####....#","#######.##","##########"},
                player=new Cell(2,1),coffin=new Cell(7,7),sunEnabled=false,sunFrom=(SunSide)0,
                solution="DRRRDULLLLDRRLDDRULURRRRLDRRDDD",
                pieces=new EntityData[]{new EntityData{id="M",kind=(PieceKind)1,cell=new Cell(7,1)},new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(2,2)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(5,3)},new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(5,4)},new EntityData{id="C",kind=(PieceKind)0,cell=new Cell(3,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(2,3)},new DeviceData{channel=1,cell=new Cell(5,5)},new DeviceData{channel=2,cell=new Cell(8,3)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,2)},new DeviceData{channel=1,cell=new Cell(4,3)},new DeviceData{channel=0,cell=new Cell(6,6)},new DeviceData{channel=1,cell=new Cell(7,4)},new DeviceData{channel=2,cell=new Cell(7,6)}},
            });
            // sun:sun_36067
            yield return new DraftPreset("room_27","日照换岗",(DraftMechanic)7,(DraftComplexity)0,new LevelData
            {
                rows=new[]{"#########","#.......#","#....####","#..#....#","#....####","#.......#","#########"},
                player=new Cell(2,5),coffin=new Cell(7,3),sunEnabled=true,sunFrom=(SunSide)1,
                solution="URRURRR",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(2,3)},new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(4,3)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(2,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(2,2)},new DeviceData{channel=0,cell=new Cell(4,3)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(6,3)}},
            });
            // sun:sun_7416
            yield return new DraftPreset("room_28","双光门厅",(DraftMechanic)7,(DraftComplexity)1,new LevelData
            {
                rows=new[]{"##########","####.#.###","####.#.###","#........#","#......###","#.#..#####","##########"},
                player=new Cell(4,4),coffin=new Cell(8,3),sunEnabled=true,sunFrom=(SunSide)1,
                solution="DLULURDRURDRURR",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(4,1)},new EntityData{id="M",kind=(PieceKind)1,cell=new Cell(1,5)},new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(4,3)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(3,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,3)},new DeviceData{channel=0,cell=new Cell(6,3)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,2)},new DeviceData{channel=0,cell=new Cell(6,2)},new DeviceData{channel=0,cell=new Cell(7,3)}},
            });
            // sun:sun_11571
            yield return new DraftPreset("room_29","借灯运箱",(DraftMechanic)7,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"#########","#...#.###","#...#..##","#...#...#","#.......#","#...#...#","##.######","#########"},
                player=new Cell(1,2),coffin=new Cell(2,6),sunEnabled=true,sunFrom=(SunSide)2,
                solution="RURDLDLDRRLUURDLDRRLDLD",
                pieces=new EntityData[]{new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(2,4)},new EntityData{id="B",kind=(PieceKind)0,cell=new Cell(2,2)},new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(5,1)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(6,4)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(2,5)}},
            });
            // expansion:candidate_205
            yield return new DraftPreset("room_30","两束反光",(DraftMechanic)10,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"#########","#.......#","#...#...#","#.......#","#...#...#","#.......#","#########"},
                player=new Cell(1,4),coffin=new Cell(6,2),sunEnabled=false,sunFrom=(SunSide)0,
                solution="DRRRRLLULLURRUURRRD",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(5,5)},new EntityData{id="K",kind=(PieceKind)1,cell=new Cell(7,3)},new EntityData{id="M",kind=(PieceKind)2,cell=new Cell(2,5)},new EntityData{id="N",kind=(PieceKind)3,cell=new Cell(2,3)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // sun:sun_6784
            yield return new DraftPreset("room_31","向阳移镜",(DraftMechanic)11,(DraftComplexity)0,new LevelData
            {
                rows=new[]{"########","##...#.#","#.#....#","#......#","#......#","#......#","########"},
                player=new Cell(4,2),coffin=new Cell(3,1),sunEnabled=true,sunFrom=(SunSide)0,
                solution="DLUU",
                pieces=new EntityData[]{new EntityData{id="piece-0",kind=(PieceKind)1,cell=new Cell(1,3)},new EntityData{id="piece-1",kind=(PieceKind)2,cell=new Cell(3,3)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // sun:sun_14803
            yield return new DraftPreset("room_32","镜后阴影",(DraftMechanic)11,(DraftComplexity)1,new LevelData
            {
                rows=new[]{"#########","###...###","##....###","#.......#","##.....##","##...#.##","#########"},
                player=new Cell(4,3),coffin=new Cell(1,3),sunEnabled=true,sunFrom=(SunSide)0,
                solution="UULDDULDL",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(3,4)},new EntityData{id="M",kind=(PieceKind)2,cell=new Cell(2,3)},new EntityData{id="N",kind=(PieceKind)3,cell=new Cell(3,2)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // sun:sun_20252
            yield return new DraftPreset("room_33","借墙送镜",(DraftMechanic)11,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"#########","#.......#","#..###..#","#.......#","#..#.#..#","#...#...#","#########"},
                player=new Cell(2,5),coffin=new Cell(6,2),sunEnabled=true,sunFrom=(SunSide)2,
                solution="ULURUURRRRD",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(7,3)},new EntityData{id="M",kind=(PieceKind)0,cell=new Cell(3,1)},new EntityData{id="N",kind=(PieceKind)3,cell=new Cell(2,3)}},
                plates=new DeviceData[]{},
                doors=new DeviceData[]{},
            });
            // gate:gate_1902
            yield return new DraftPreset("room_34","镜前开门",(DraftMechanic)14,(DraftComplexity)0,new LevelData
            {
                rows=new[]{"#######","###...#","#.....#","#...#.#","#.....#","#.....#","#######"},
                player=new Cell(2,5),coffin=new Cell(3,1),sunEnabled=false,sunFrom=(SunSide)0,
                solution="URDRURUUULL",
                pieces=new EntityData[]{new EntityData{id="piece-0",kind=(PieceKind)1,cell=new Cell(1,3)},new EntityData{id="piece-1",kind=(PieceKind)2,cell=new Cell(3,3)},new EntityData{id="piece-2",kind=(PieceKind)2,cell=new Cell(2,4)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,5)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,4)}},
            });
            // expansion:candidate_210
            yield return new DraftPreset("room_35","绕镜压板",(DraftMechanic)14,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"#########","#.......#","#..###..#","#.......#","#..#....#","#.......#","#########"},
                player=new Cell(3,1),coffin=new Cell(2,2),sunEnabled=false,sunFrom=(SunSide)0,
                solution="RRRRDDDDLLLURRDRUULLLLRRRUULLLLD",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(2,5)},new EntityData{id="M",kind=(PieceKind)2,cell=new Cell(4,5)},new EntityData{id="N",kind=(PieceKind)3,cell=new Cell(5,4)},new EntityData{id="A",kind=(PieceKind)0,cell=new Cell(6,3)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(7,2)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(5,3)}},
            });
            // gate:gate_14110
            yield return new DraftPreset("room_36","光庭门槛",(DraftMechanic)15,(DraftComplexity)0,new LevelData
            {
                rows=new[]{"#########","###...###","##....###","#....####","##..#####","##..#####","#########"},
                player=new Cell(3,1),coffin=new Cell(1,3),sunEnabled=true,sunFrom=(SunSide)0,
                solution="RLDDULDL",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(3,4)},new EntityData{id="M",kind=(PieceKind)2,cell=new Cell(2,3)},new EntityData{id="N",kind=(PieceKind)3,cell=new Cell(3,2)},new EntityData{id="plate-box",kind=(PieceKind)0,cell=new Cell(4,1)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(5,1)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(3,3)}},
            });
            // gate:gate_21162
            yield return new DraftPreset("room_37","回身留门",(DraftMechanic)15,(DraftComplexity)1,new LevelData
            {
                rows=new[]{"#########","###...###","##....###","#.......#","##.....##","##....###","#########"},
                player=new Cell(3,1),coffin=new Cell(1,3),sunEnabled=true,sunFrom=(SunSide)3,
                solution="DURDDULDULDL",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(3,4)},new EntityData{id="M",kind=(PieceKind)2,cell=new Cell(2,3)},new EntityData{id="N",kind=(PieceKind)3,cell=new Cell(3,2)},new EntityData{id="plate-box",kind=(PieceKind)0,cell=new Cell(4,2)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(4,4)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(2,2)}},
            });
            // gate:gate_22651
            yield return new DraftPreset("room_38","夹光转运",(DraftMechanic)15,(DraftComplexity)2,new LevelData
            {
                rows=new[]{"#########","###...###","##.....##","#.......#","##...#.##","##.....##","#########"},
                player=new Cell(4,3),coffin=new Cell(1,3),sunEnabled=true,sunFrom=(SunSide)0,
                solution="UURDLULDDULDL",
                pieces=new EntityData[]{new EntityData{id="L",kind=(PieceKind)1,cell=new Cell(3,4)},new EntityData{id="M",kind=(PieceKind)2,cell=new Cell(2,3)},new EntityData{id="N",kind=(PieceKind)3,cell=new Cell(3,2)},new EntityData{id="plate-box",kind=(PieceKind)0,cell=new Cell(5,2)}},
                plates=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(5,3)}},
                doors=new DeviceData[]{new DeviceData{channel=0,cell=new Cell(3,1)}},
            });
        }
    }
}
