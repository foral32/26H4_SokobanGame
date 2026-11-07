using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BoxOrVoid.Core;
using UnityEngine;

namespace BoxOrVoid.Study.LevelEditing
{
    [Serializable] public sealed class LevelDocument
    {
        public int schemaVersion=1;
        public string id,name;
        public int theme=1;
        public long updatedUtc;
        public LevelData level;
        public string solvedHash="",solution="",rulesVersion="";
        public const string CurrentRules="box-void-study-1";
        public bool IsPlayedThrough=>rulesVersion==CurrentRules && solvedHash==LayoutHash();
        public string LayoutHash()
        {
            using(var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(level)))).Replace("-","");
        }
        public LevelDocument Clone()=>JsonUtility.FromJson<LevelDocument>(JsonUtility.ToJson(this));
        public static LevelDocument New(int width=9,int height=7,string title="新的一关")
        {
            if(width<3||height<3||width>LevelValidation.MaxSide||height>LevelValidation.MaxSide) throw new ArgumentOutOfRangeException("width");
            return new LevelDocument{id=Guid.NewGuid().ToString("N"),name=title,level=new LevelData{width=width,height=height,grid=Enumerable.Range(0,width*height).Select(_=>new CellData{Objects=Array.Empty<CellObjectData>()}).ToArray()}};
        }
    }
    public sealed class LevelProblem
    {
        public string Title,Detail;
        public int Cell=-1;
        public ObjectKind Kind;
        public Tone Tone;
    }
    public static class LevelValidation
    {
        public const int MaxSide=64,MaxObjectsPerCell=64;
        public static void CheckEnvelope(LevelDocument d)
        {
            if(d==null||d.schemaVersion!=1) throw new ArgumentException("这份关卡的格式版本暂不支持。");
            if(d.level==null||d.level.width<1||d.level.height<1||d.level.width>MaxSide||d.level.height>MaxSide||d.level.grid==null||d.level.grid.Length!=d.level.width*d.level.height) throw new ArgumentException("棋盘尺寸或格子数据不完整。");
            if(string.IsNullOrWhiteSpace(d.name)||d.name.Length>60) throw new ArgumentException("关卡名字需要 1 到 60 个字。");
            if(d.theme<0||d.theme>1) throw new ArgumentException("这份关卡使用了尚未支持的主题。");
            foreach(var cell in d.level.grid)
            {
                if(cell==null||cell.Objects==null||cell.Objects.Length>MaxObjectsPerCell) throw new ArgumentException("格子里的物件数据不完整或过多。");
                foreach(var o in cell.Objects)
                    if(o==null||!Enum.IsDefined(typeof(ObjectKind),o.ObjectType)||!Enum.IsDefined(typeof(Tone),o.Color)||o.ObjectType==ObjectKind.Empty||o.Color==Tone.None||(o.ObjectType!=ObjectKind.Wall&&o.Color==Tone.Gray)) throw new ArgumentException("发现不认识的物件或颜色。");
            }
        }
        public static List<LevelProblem> ForPlay(LevelDocument d)
        {
            var result=new List<LevelProblem>();
            try {CheckEnvelope(d);}catch(ArgumentException ex){result.Add(new LevelProblem{Title="这份关卡还不能打开",Detail=ex.Message});return result;}
            foreach(var tone in new[]{Tone.White,Tone.Black})
            {
                var positions=new List<int>();
                for(int i=0;i<d.level.grid.Length;i++) foreach(var o in d.level.grid[i].Objects) if(o.ObjectType==ObjectKind.Player&&o.Color==tone) positions.Add(i);
                if(positions.Count!=1) result.Add(new LevelProblem{Title=positions.Count==0?"还少一个"+ToneName(tone)+"色角色":ToneName(tone)+"色角色有点多",Detail="白色和黑色角色各一位，才能轮流行动。",Kind=ObjectKind.Player,Tone=tone,Cell=positions.Count>1?positions[1]:-1});
            }
            if(!d.level.grid.Any(c=>c.Objects.Any(o=>o.ObjectType==ObjectKind.BoxGoal||o.ObjectType==ObjectKind.PlayerGoal))) result.Add(new LevelProblem{Title="还没有要到达的地方",Detail="摆一个目标，让玩家知道这一关要去哪儿。",Kind=ObjectKind.PlayerGoal,Tone=Tone.White});
            for(int i=0;i<d.level.grid.Length;i++)
            {
                var objects=d.level.grid[i].Objects;
                if(objects.Count(o=>o.ObjectType==ObjectKind.Wall)>1 || objects.Skip(1).Any(o=>o.ObjectType==ObjectKind.Wall)) result.Add(new LevelProblem{Title="这一格的墙需要整理",Detail="每格保留一面底墙，把墙放在物件下面。",Cell=i});
                if(objects.Any(o=>o.ObjectType==ObjectKind.Wall&&o.Color==Tone.Gray)&&objects.Length>1) result.Add(new LevelProblem{Title="灰墙占住了这个位置",Detail="先把灰墙上的角色、箱子或目标拿走。",Cell=i});
            }
            if(result.Count==0) try{PuzzleDefinition.FromData(d.level);}catch(ArgumentException){result.Add(new LevelProblem{Title="还有一处摆放需要调整",Detail="检查墙、角色和目标的摆放后，再玩一下。"});}
            return result;
        }
        public static string ToneName(Tone t)=>t==Tone.White?"白":t==Tone.Black?"黑":"灰";
        public static string KindName(ObjectKind k)=>k==ObjectKind.Wall?"墙":k==ObjectKind.Box?"箱子":k==ObjectKind.Player?"角色":k==ObjectKind.BoxGoal?"箱子目标":"角色目标";
        public static string Name(CellObjectData o)=>ToneName(o.Color)+KindName(o.ObjectType);
    }
}
