using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEngine;

namespace DraculasBox.Editor
{
    // A review is a snapshot. Selection composes a candidate from the source, never
    // removes unchecked work from the recoverable editing document.
    internal sealed class CampaignPublication
    {
        internal enum Kind { ChapterAdd, ChapterRemove, ChapterTitle, ChapterOrder, LevelAdd, LevelRemove, Placement, LevelOrder, Title, Board, Route }
        internal sealed class Item
        {
            public string key,id,title,detail;
            public Kind kind;
            public bool selected=true;
        }
        public readonly List<Item> Items=new();
        public readonly CampaignContent Baseline,Working;
        internal static T Copy<T>(T value)=>value is null?default:JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        internal static bool SameContent(CampaignContent a,CampaignContent b)=>JsonUtility.ToJson(a.catalog)==JsonUtility.ToJson(b.catalog)&&
            a.boards.OrderBy(x=>x.id,StringComparer.Ordinal).Select(x=>JsonUtility.ToJson(x)).SequenceEqual(b.boards.OrderBy(x=>x.id,StringComparer.Ordinal).Select(x=>JsonUtility.ToJson(x)))&&
            a.replays.levels.OrderBy(x=>x.id,StringComparer.Ordinal).Select(x=>JsonUtility.ToJson(x)).SequenceEqual(b.replays.levels.OrderBy(x=>x.id,StringComparer.Ordinal).Select(x=>JsonUtility.ToJson(x)));
        private static Chapter Owner(CampaignContent data,string id)=>data.catalog.chapters.FirstOrDefault(c=>c.levels.Any(l=>l.id==id));
        private static bool SamePlace(ChapterLink a,ChapterLink b)=>a.main==b.main&&(a.main||a.branchAbove==b.branchAbove&&a.requires.SequenceEqual(b.requires));
        public CampaignPublication(CampaignContent baseline,CampaignContent working)
        {
            Baseline=Copy(baseline);Working=CampaignProject.PrepareForPublication(working);
            void Add(Kind kind,string id,string title,string detail)=>Items.Add(new Item{key=kind+":"+id,kind=kind,id=id,title=title,detail=detail});
            var old=Baseline.catalog.chapters;
            var now=Working.catalog.chapters;
            foreach(var c in now)
            {
                var prior=old.FirstOrDefault(x=>x.id==c.id);
                if(prior==null){Add(Kind.ChapterAdd,c.id,c.title,"新增章节");continue;}
                if(prior.title!=c.title)Add(Kind.ChapterTitle,c.id,c.title,"章节改名："+prior.title+" → "+c.title);
                var stable=prior.levels.Where(l=>c.levels.Any(n=>n.id==l.id&&SamePlace(l,n))).Select(l=>l.id).ToHashSet();
                if(!prior.levels.Where(l=>stable.Contains(l.id)).Select(l=>l.id).SequenceEqual(c.levels.Where(l=>stable.Contains(l.id)).Select(l=>l.id)))
                    Add(Kind.LevelOrder,c.id,c.title,"调整关卡顺序");
            }
            foreach(var c in old.Where(c=>!now.Any(n=>n.id==c.id)))Add(Kind.ChapterRemove,c.id,c.title,"删除章节 · 含 "+c.levels.Length+" 关");
            var common=new HashSet<string>(old.Select(c=>c.id).Intersect(now.Select(c=>c.id)));
            if(!old.Where(c=>common.Contains(c.id)).Select(c=>c.id).SequenceEqual(now.Where(c=>common.Contains(c.id)).Select(c=>c.id)))
                Add(Kind.ChapterOrder,"order","章节顺序",string.Join(" → ",now.Select(c=>c.title)));
            foreach(var c in now)foreach(var node in c.levels)
            {
                var owner=Owner(Baseline,node.id);
                string name=CampaignLabels.Name(Working.catalog,node.id,Working.Board(node.id).title);
                if(owner==null)
                {Add(Kind.LevelAdd,node.id,name,"新增关卡");continue;}
                var before=owner.levels.First(l=>l.id==node.id);
                if(owner.id!=c.id||!SamePlace(before,node))
                    Add(Kind.Placement,node.id,name,"编排："+CampaignLabels.Prefix(Baseline.catalog,node.id)+" → "+CampaignLabels.Prefix(Working.catalog,node.id));
            }
            foreach(var c in old.Where(c=>common.Contains(c.id)))foreach(var node in c.levels)
                if(Owner(Working,node.id)==null)Add(Kind.LevelRemove,node.id,CampaignLabels.Name(Baseline.catalog,node.id,Baseline.Board(node.id).title),"删除关卡");
            foreach(var b in Working.boards)
            {
                var prior=Baseline.boards.FirstOrDefault(x=>x.id==b.id);
                if(prior==null)continue; // New boards travel with the individual add-level item.
                string name=CampaignLabels.Name(Working.catalog,b.id,b.title);
                if(b.title!=prior.title)Add(Kind.Title,b.id,name,"改名："+prior.title+" → "+b.title);
                if(!b.LayoutEquals(prior)||b.hint!=prior.hint)Add(Kind.Board,b.id,name,"修改棋盘");
                else if(JsonUtility.ToJson(Working.Plan(b.id))!=JsonUtility.ToJson(Baseline.Plan(b.id)))
                {
                    var before=Baseline.Plan(b.id);var after=Working.Plan(b.id);
                    Add(Kind.Route,b.id,name,before?.route==after?.route&&after!=null?"校正推数："+before.pushes+" → "+after.pushes:"更新关卡解");
                }
            }
        }
        public CampaignContent Compose()
        {
            var result=Copy(Baseline);
            var chapters=result.catalog.chapters.ToList();
            foreach(var removal in Items.Where(i=>i.selected&&i.kind==Kind.ChapterRemove))
                foreach(var level in Baseline.Chapter(removal.id).levels)
                    if(Owner(Working,level.id)!=null&&!Items.Any(i=>i.selected&&i.kind==Kind.Placement&&i.id==level.id))
                        throw new InvalidOperationException("「"+Working.Board(level.id).title+"」已移至其他章节，请同时勾选它的编排修改。");
            foreach(var item in Items.Where(i=>i.selected))
            {
                if(item.kind==Kind.ChapterRemove)chapters.RemoveAll(c=>c.id==item.id);
                if(item.kind==Kind.ChapterAdd)
                {
                    var added=Copy(Working.Chapter(item.id));added.levels=Array.Empty<ChapterLink>();
                    int wi=Array.FindIndex(Working.catalog.chapters,c=>c.id==item.id);
                    string previous=Working.catalog.chapters.Take(wi).LastOrDefault(c=>chapters.Any(p=>p.id==c.id))?.id;
                    chapters.Insert(previous==null?0:chapters.FindIndex(c=>c.id==previous)+1,added);
                }
                if(item.kind==Kind.ChapterTitle)chapters.First(c=>c.id==item.id).title=Working.Chapter(item.id).title;
            }
            // Membership is selected per level. Rebuild predecessor links after all
            // insertions so an unfinished neighbouring addition is never required.
            var placements=Items.Where(i=>i.selected&&(i.kind==Kind.LevelAdd||i.kind==Kind.LevelRemove||i.kind==Kind.Placement)).ToArray();
            foreach(var item in placements)foreach(var c in chapters)c.levels=c.levels.Where(l=>l.id!=item.id).ToArray();
            foreach(var wanted in Working.catalog.chapters)foreach(var node in wanted.levels)
            {
                if(!placements.Any(i=>i.id==node.id&&i.kind!=Kind.LevelRemove))continue;
                var target=chapters.FirstOrDefault(c=>c.id==wanted.id);
                if(target==null)throw new InvalidOperationException("请同时勾选新增章节「"+wanted.title+"」。");
                var list=target.levels.ToList();
                var previous=wanted.levels.TakeWhile(l=>l.id!=node.id).LastOrDefault(l=>list.Any(n=>n.id==l.id));
                list.Insert(previous==null?0:list.FindIndex(l=>l.id==previous.id)+1,Copy(node));target.levels=list.ToArray();
            }
            foreach(var item in Items.Where(i=>i.selected&&i.kind==Kind.LevelOrder))
            {
                var c=chapters.First(x=>x.id==item.id);var desired=Working.Chapter(item.id).levels.Select(l=>l.id).ToList();
                var sorted=c.levels.Where(l=>desired.Contains(l.id)).OrderBy(l=>desired.IndexOf(l.id)).ToList();
                foreach(var extra in c.levels.Where(l=>!desired.Contains(l.id)))sorted.Insert(Math.Min(Array.IndexOf(c.levels,extra),sorted.Count),extra);
                c.levels=sorted.ToArray();
            }
            foreach(var c in chapters)
            {
                var main=c.levels.Where(l=>l.main).ToArray();
                for(int i=0;i<main.Length;i++)main[i].requires=i==0?Array.Empty<string>():new[]{main[i-1].id};
                c.levels=main.Concat(c.levels.Where(l=>!l.main)).ToArray();
            }
            if(Items.Any(i=>i.selected&&i.kind==Kind.ChapterOrder))
            {
                var order=Working.catalog.chapters.Select(c=>c.id).ToList();
                var sorted=chapters.Where(c=>order.Contains(c.id)).OrderBy(c=>order.IndexOf(c.id)).ToList();
                foreach(var extra in chapters.Where(c=>!order.Contains(c.id)))sorted.Insert(Math.Min(chapters.IndexOf(extra),sorted.Count),extra);
                chapters=sorted;
            }
            for(int i=0;i<chapters.Count;i++)chapters[i].requiresChapter=i==0?"":chapters[i-1].id;
            result.catalog.chapters=chapters.ToArray();
            var active=new HashSet<string>(chapters.SelectMany(c=>c.levels).Select(l=>l.id));
            result.boards=Baseline.boards.Where(b=>active.Contains(b.id)).Select(Copy).Concat(Working.boards.Where(b=>active.Contains(b.id)&&!Baseline.boards.Any(old=>old.id==b.id)).Select(Copy)).ToArray();
            result.replays.levels=Baseline.replays.levels.Where(p=>active.Contains(p.id)).Select(Copy).Concat(Working.replays.levels.Where(p=>active.Contains(p.id)&&!Baseline.boards.Any(b=>b.id==p.id)).Select(Copy)).ToArray();
            foreach(var item in Items.Where(i=>i.selected))
            {
                if(item.kind!=Kind.Title&&item.kind!=Kind.Board&&item.kind!=Kind.Route)continue;
                if(!active.Contains(item.id))throw new InvalidOperationException(item.title+"：请同时勾选所属章节的关卡编排。");
                var b=result.Board(item.id);var w=Working.Board(item.id);
                if(item.kind==Kind.Title)b.title=w.title;
                else
                {
                    if(item.kind==Kind.Board){var replacement=w.Copy();replacement.title=b.title;result.boards[Array.IndexOf(result.boards,b)]=replacement;}
                    var plans=result.replays.levels.ToList();int index=plans.FindIndex(p=>p.id==item.id);var plan=Working.Plan(item.id);
                    if(plan==null){if(index>=0)plans.RemoveAt(index);}
                    else if(index>=0)plans[index]=Copy(plan);else plans.Add(Copy(plan));
                    result.replays.levels=plans.ToArray();
                }
            }
            foreach(var c in chapters)
            {
                if(!c.levels.Any(l=>l.main))throw new InvalidOperationException("「"+c.title+"」至少需要一关主线，请勾选要保留的主线关卡。");
                foreach(var l in c.levels.Where(l=>!l.main))
                    if(l.requires.Length!=1||!c.levels.Any(m=>m.main&&m.id==l.requires[0]))
                        throw new InvalidOperationException("「"+result.Board(l.id).title+"」缺少所属主线，请同时勾选主线的新增或移动。");
            }
            CampaignDocument.ValidateGraph(result);
            return result;
        }
        public string Check(out CampaignContent candidate,out List<CampaignProject.PublicationIssue> issues)
        {
            candidate=null;issues=new();
            if(!Items.Any(i=>i.selected))return "请选择要写回的修改。";
            try
            {
                candidate=Compose();issues=CampaignProject.PublicationIssues(candidate,CampaignProject.ChangedProofs(Baseline,candidate));
                var catalog=candidate.catalog;
                return issues.Count==0?null:string.Join("\n",issues.Select(i=>CampaignLabels.Name(catalog,i.id,i.title)+"："+i.message));
            }
            catch(Exception e){return "编排关联未完整勾选："+e.Message;}
        }
    }
}
