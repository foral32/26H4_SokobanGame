using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public readonly struct MapInsertion : IEquatable<MapInsertion>
    {
        public readonly string anchor;
        public readonly bool above;
        public readonly int index;
        public bool Main => anchor == null;
        public MapInsertion(int index, string anchor = null, bool above = false)
        { this.index = index; this.anchor = anchor; this.above = above; }
        public bool Equals(MapInsertion other) => index == other.index && anchor == other.anchor && above == other.above;
    }

    // Local coordinates start at the left edge of the scrollable map, with the main path at y=0.
    public sealed class ChapterMapLayout
    {
        public const float ViewWidth=1168, ViewHeight=488, BranchY=158, BranchSpacing=108;
        public readonly Dictionary<string,Vector2> positions=new();
        public readonly Dictionary<string,string> numbers=new();
        public readonly List<(string anchor,string[] siblings,bool above)> lanes=new();
        public readonly float width;
        private readonly List<(MapInsertion target,float left,float right)> slots=new();
        public readonly float[] centers;

        public ChapterMapLayout(Chapter chapter,string[] columns=null,string pin=null,float pinX=0)
        {
            columns??=chapter.levels.Where(l=>l.main).Select(l=>l.id).ToArray();
            var branches=chapter.levels.Where(l=>!l.main).ToLookup(l=>l.requires[0]);
            var main=chapter.levels.Where(l=>l.main).ToArray();
            for(int i=0;i<main.Length;i++)
            {
                string ordinal=(i+1).ToString();numbers[main[i].id]=ordinal;int n=0;
                foreach(var child in branches[main[i].id])numbers[child.id]=ordinal+Suffix(n++);
            }
            float step=Mathf.Clamp((ViewWidth-76)/columns.Length,146,172);
            var widths=columns.Select(id=>{
                if(id==null)return step;
                int up=branches[id].Count(l=>l.branchAbove);
                return Mathf.Max(step,Math.Max(up,branches[id].Count()-up)*BranchSpacing+28);
            }).ToArray();
            float total=widths.Sum(),left=Mathf.Max(38,(ViewWidth-total)*.5f);
            int at=pin==null?-1:Array.IndexOf(columns,pin);
            if(at>=0)left=Mathf.Max(30,pinX-widths.Take(at).Sum()-widths[at]*.5f);
            width=Mathf.Max(ViewWidth,left+total+32);centers=new float[columns.Length];
            for(int i=0;i<columns.Length;i++)
            {
                string id=columns[i];float size=widths[i],x=left+size*.5f;centers[i]=x;
                float gap=i==0?left+8:(centers[i-1]+x)*.5f;
                slots.Add((new MapInsertion(i),gap-45,gap+45));
                if(id!=null)
                {
                    positions[id]=new Vector2(x,0);
                    foreach(bool above in new[]{false,true})
                    {
                        string[] kids=branches[id].Where(n=>n.branchAbove==above).Select(n=>n.id).ToArray();
                        if(kids.Length>0)lanes.Add((id,kids,above));
                        float prior=left+2;
                        for(int j=0;j<=kids.Length;j++)
                        {
                            float end=j==kids.Length?left+size-2:x+(j-(kids.Length-1)*.5f)*BranchSpacing;
                            slots.Add((new MapInsertion(j,id,above),prior,end));
                            if(j<kids.Length)positions[kids[j]]=new Vector2(end,above?BranchY:-BranchY);
                            prior=end;
                        }
                    }
                }
                left+=size;
            }
            slots.Add((new MapInsertion(columns.Length),left-53,left+37));
        }
        public MapInsertion? Target(Vector2 p)
        {
            bool main=Mathf.Abs(p.y)<=55;
            if(!main&&(Mathf.Abs(p.y)<72||Mathf.Abs(p.y)>ViewHeight*.5f-9))return null;
            foreach(var slot in slots)
                if(slot.target.Main==main&&(main||slot.target.above==(p.y>0))&&p.x>=slot.left&&p.x<=slot.right)return slot.target;
            return null;
        }
        private static string Suffix(int n)
        {
            string result="";
            for(n++;n>0;n=(n-1)/26)result=(char)('A'+(n-1)%26)+result;
            return result;
        }
    }
}
