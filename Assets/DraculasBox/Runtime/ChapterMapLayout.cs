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
        private readonly string[] mainIds;

        public ChapterMapLayout(Chapter chapter,string[] columns=null,string pin=null,float pinX=0)
        {
            columns??=chapter.levels.Where(l=>l.main).Select(l=>l.id).ToArray();
            var branches=chapter.levels.Where(l=>!l.main).ToLookup(l=>l.requires[0]);
            var main=chapter.levels.Where(l=>l.main).ToArray();
            mainIds=main.Select(l=>l.id).ToArray();
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
        public MapInsertion? DropTarget(Vector2 center,string moving,MapInsertion? previous=null,bool allowBranch=true)
        {
            float height=Mathf.Abs(center.y);
            if(height>ViewHeight*.5f+18)return null;
            bool previousBranch=previous.HasValue&&!previous.Value.Main&&previous.Value.above==(center.y>0);
            bool main=height<=70||height<100&&!previousBranch;
            if(!main&&!allowBranch)return null;
            if(main)
            {
                int source=Array.IndexOf(mainIds,moving);
                // Dropping over a node means moving to its place, rather than
                // requiring the pointer to find a narrow inter-node gap.
                for(int j=0;j<mainIds.Length;j++)
                {
                    if(mainIds[j]==moving||!positions.TryGetValue(mainIds[j],out var pos))continue;
                    int index=source>=0?(j<source?j:j+1):j+(center.x>=pos.x?1:0);
                    bool keeping=previous.HasValue&&previous.Value.Main&&
                        (source>=0?previous.Value.index==index:previous.Value.index==j||previous.Value.index==j+1);
                    if(Mathf.Abs(center.x-pos.x)>(keeping?68:54))continue;
                    if(source<0&&previous.HasValue&&previous.Value.Main&&Mathf.Abs(center.x-pos.x)<=12&&
                        (previous.Value.index==j||previous.Value.index==j+1))return previous;
                    return new MapInsertion(index);
                }
            }
            MapInsertion? best=null;float distance=float.MaxValue;
            foreach(var slot in slots)
            {
                if(slot.target.Main!=main||!main&&(slot.target.above!=(center.y>0)||slot.target.anchor==moving))continue;
                float value=main?Mathf.Abs(center.x-(slot.left+slot.right)*.5f):Mathf.Max(0,Mathf.Max(slot.left-center.x,center.x-slot.right));
                if(value<distance){best=slot.target;distance=value;}
            }
            if(!best.HasValue||distance>(main?112:72))return null;
            if(previous.HasValue&&previous.Value.Main==main&&(main||previous.Value.above==(center.y>0)))
                foreach(var slot in slots)
                    if(slot.target.Equals(previous.Value))
                    {
                        float value=main?Mathf.Abs(center.x-(slot.left+slot.right)*.5f):Mathf.Max(0,Mathf.Max(slot.left-center.x,center.x-slot.right));
                        if(value<=distance+12)return previous;
                        break;
                    }
            return best;
        }
        private static string Suffix(int n)
        {
            string result="";
            for(n++;n>0;n=(n-1)/26)result=(char)('A'+(n-1)%26)+result;
            return result;
        }
    }
}
