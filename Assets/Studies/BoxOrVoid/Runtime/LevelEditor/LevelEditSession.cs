using System;
using System.Collections.Generic;
using System.Linq;
using BoxOrVoid.Core;
using UnityEngine;

namespace BoxOrVoid.Study.LevelEditing
{
    public enum EditTool { Place,Look,Erase,Line,Rectangle,Fill,Pick,Select }
    public sealed class SelectionStamp
    {
        public int Width,Height;
        public CellData[] Cells;
        public bool[] Included;
        public SelectionStamp Transform(bool rotate,bool mirrorX,bool mirrorY)
        {
            int w=rotate?Height:Width,h=rotate?Width:Height;
            var result=new SelectionStamp{Width=w,Height=h,Cells=new CellData[w*h],Included=Included==null?null:new bool[w*h]};
            for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
            {
                int nx=rotate?y:x,ny=rotate?Width-1-x:y;
                if(mirrorX) nx=w-1-nx;if(mirrorY) ny=h-1-ny;
                result.Cells[ny*w+nx]=LevelEditSession.CloneCell(Cells[y*Width+x]);
                if(Included!=null)result.Included[ny*w+nx]=Included[y*Width+x];
            }
            return result;
        }
    }
    /// <summary>A document and its edit history never share mutable objects with a play session or clipboard.</summary>
    public sealed class LevelEditSession
    {
        private readonly List<LevelDocument> undo=new List<LevelDocument>(),redo=new List<LevelDocument>();
        private LevelDocument before;
        private string savedJson;
        private bool dirty;
        public LevelDocument Document {get;private set;}
        public EditTool Tool=EditTool.Place;
        public ObjectKind Brush=ObjectKind.Box;
        public Tone Tone=Tone.White,ViewTone=Tone.White;
        public bool Repeat,Grid;
        public int SelectedCell=-1,SelectedLayer=-1;
        public RectInt Selection;
        public bool HasSelection;
        private HashSet<int> selectedCells;
        public IEnumerable<int> SelectedIndices=>selectedCells!=null?(IEnumerable<int>)selectedCells:Indices(Selection);
        public float Zoom=1;
        public Vector2 Pan;
        public string LastMessage {get;private set;}="";
        public int Revision {get;private set;}
        public bool CanUndo=>undo.Count>0;
        public bool CanRedo=>redo.Count>0;
        public bool IsDrawing=>before!=null;
        public bool Dirty=>dirty;
        public event Action Changed;
        public LevelEditSession(LevelDocument document,bool saved=true)
        {Document=document.Clone();dirty=!saved;if(saved) MarkSaved();}
        private static string ContentJson(LevelDocument value){var copy=value.Clone();copy.updatedUtc=0;return JsonUtility.ToJson(copy);}
        public void MarkSaved(){savedJson=ContentJson(Document);dirty=false;}
        public void RecordCompletion(string hash,string commands)
        {Document.solvedHash=hash;Document.rulesVersion=LevelDocument.CurrentRules;Document.solution=commands;dirty=savedJson!=ContentJson(Document);Changed?.Invoke();}
        public void Begin(){if(before==null) before=Document.Clone();LastMessage="";}
        public bool Commit()
        {
            if(before==null)return false;
            var prior=before;before=null;
            if(JsonUtility.ToJson(prior)==JsonUtility.ToJson(Document)) return false;
            undo.Add(prior);if(undo.Count>128)undo.RemoveAt(0);redo.Clear();Notify();return true;
        }
        public void Cancel(){if(before==null)return;Document=before;before=null;Notify();}
        public void Undo(){if(before!=null){Cancel();return;}if(!CanUndo)return;redo.Add(Document.Clone());Document=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);SafeSelection();Notify();}
        public void Redo(){Cancel();if(!CanRedo)return;undo.Add(Document.Clone());Document=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);SafeSelection();Notify();}
        private void SafeSelection(){SelectedCell=-1;SelectedLayer=-1;HasSelection=false;selectedCells=null;}
        private void Notify(){Revision++;dirty=savedJson!=ContentJson(Document);Changed?.Invoke();}
        public void SetName(string name){name=(name??"").Trim();if(name.Length<1||name.Length>60)throw new ArgumentException("名字需要 1 到 60 个字。");Begin();Document.name=name;Commit();}
        public void SetTheme(int theme){Begin();Document.theme=Mathf.Clamp(theme,0,1);Commit();}
        public static CellData CloneCell(CellData c)=>new CellData{Objects=c.Objects.Select(o=>new CellObjectData{ObjectType=o.ObjectType,Color=o.Color}).ToArray()};
        public bool Place(int index,bool erase=false)
        {
            if(index<0||index>=Document.level.grid.Length)return false;
            var list=Document.level.grid[index].Objects.ToList();
            if(erase){int at=list.FindLastIndex(o=>o.ObjectType==Brush&&o.Color==Tone);if(at<0)return false;list.RemoveAt(at);}
            else
            {
                if(Brush==ObjectKind.Wall&&Tone==Tone.Gray&&list.Any(o=>o.ObjectType!=ObjectKind.Wall)){LastMessage="此格已有物件，请先移除再放置灰墙";return false;}
                if(Brush!=ObjectKind.Wall&&list.Any(o=>o.ObjectType==ObjectKind.Wall&&o.Color==Tone.Gray)){LastMessage="灰墙无法与其他物件重叠";return false;}
                if(!Repeat&&list.Any(o=>o.ObjectType==Brush&&o.Color==Tone))return false;
                if(list.Count>=LevelValidation.MaxObjectsPerCell){LastMessage="此格已达到叠放上限";return false;}
                if(Brush==ObjectKind.Player)
                {
                    foreach(var c in Document.level.grid)c.Objects=c.Objects.Where(o=>o.ObjectType!=ObjectKind.Player||o.Color!=Tone).ToArray();
                    list=Document.level.grid[index].Objects.ToList();
                }
                var item=new CellObjectData{ObjectType=Brush,Color=Tone};
                if(Brush==ObjectKind.Wall){list.RemoveAll(o=>o.ObjectType==ObjectKind.Wall);list.Insert(0,item);}else list.Add(item);
            }
            Document.level.grid[index].Objects=list.ToArray();return true;
        }
        public void RemoveLayer(int cell,int layer)
        {if(cell<0||layer<0||cell>=Document.level.grid.Length||layer>=Document.level.grid[cell].Objects.Length)return;Begin();var list=Document.level.grid[cell].Objects.ToList();list.RemoveAt(layer);Document.level.grid[cell].Objects=list.ToArray();SelectedLayer=-1;Commit();}
        public void MoveLayer(int cell,int layer,int direction)
        {
            if(cell<0||cell>=Document.level.grid.Length)return;
            var list=Document.level.grid[cell].Objects.ToList();if(layer<0||layer>=list.Count||!Movable(list[layer]))return;
            var indices=Enumerable.Range(0,list.Count).Where(i=>Movable(list[i])).ToArray();int slot=Array.IndexOf(indices,layer),next=Mathf.Clamp(slot+direction,0,indices.Length-1);if(next==slot)return;
            Begin();int other=indices[next];var item=list[layer];list[layer]=list[other];list[other]=item;Document.level.grid[cell].Objects=list.ToArray();SelectedLayer=other;Commit();
        }
        public void RecolorLayer(int cell,int layer)
        {
            if(cell<0||layer<0||cell>=Document.level.grid.Length||layer>=Document.level.grid[cell].Objects.Length)return;
            var o=Document.level.grid[cell].Objects[layer];if(o.Color==Tone.Gray)return;
            var target=o.Color==Tone.White?Tone.Black:Tone.White;
            if(o.ObjectType==ObjectKind.Player&&Document.level.grid.Any(c=>c.Objects.Any(v=>v.ObjectType==ObjectKind.Player&&v.Color==target))){LastMessage="另一种颜色已有角色，先拿走它再换色";return;}
            Begin();o.Color=target;Commit();
        }
        public void ClearSelected(){if(!HasSelection)return;Begin();foreach(int i in SelectedIndices)Document.level.grid[i].Objects=Array.Empty<CellObjectData>();Commit();}
        private static bool Movable(CellObjectData o)=>o.ObjectType==ObjectKind.Box||o.ObjectType==ObjectKind.Player;
        public void Shape(int start,int end,bool rectangle,bool solid=false)
        {
            int w=Document.level.width,x0=start%w,y0=start/w,x1=end%w,y1=end/w;
            if(rectangle){for(int y=Math.Min(y0,y1);y<=Math.Max(y0,y1);y++)for(int x=Math.Min(x0,x1);x<=Math.Max(x0,x1);x++)if(solid||x==x0||x==x1||y==y0||y==y1)Place(y*w+x);}
            else{int steps=Math.Max(Math.Abs(x1-x0),Math.Abs(y1-y0));for(int n=0;n<=steps;n++){float t=steps==0?0:(float)n/steps;Place(Mathf.RoundToInt(y0+(y1-y0)*t)*w+Mathf.RoundToInt(x0+(x1-x0)*t));}}
        }
        public List<int> FillArea(int start)
        {
            int w=Document.level.width,h=Document.level.height;var result=new List<int>();var visited=new HashSet<int>();var queue=new Queue<int>();
            Tone Wall(int i)=>Document.level.grid[i].Objects.FirstOrDefault(o=>o.ObjectType==ObjectKind.Wall)?.Color??Tone.None;
            Tone target=Wall(start);queue.Enqueue(start);
            while(queue.Count>0){int i=queue.Dequeue();if(!visited.Add(i)||Wall(i)!=target)continue;result.Add(i);int x=i%w,y=i/w;if(x>0)queue.Enqueue(i-1);if(x<w-1)queue.Enqueue(i+1);if(y>0)queue.Enqueue(i-w);if(y<h-1)queue.Enqueue(i+w);}
            return result;
        }
        public void Select(int start,int end,IEnumerable<int> additive=null)
        {
            int w=Document.level.width;Selection=new RectInt(Math.Min(start%w,end%w),Math.Min(start/w,end/w),Math.Abs(start%w-end%w)+1,Math.Abs(start/w-end/w)+1);selectedCells=null;
            if(additive!=null){selectedCells=new HashSet<int>(additive);selectedCells.UnionWith(Indices(Selection));int x0=selectedCells.Min(i=>i%w),y0=selectedCells.Min(i=>i/w),x1=selectedCells.Max(i=>i%w),y1=selectedCells.Max(i=>i/w);Selection=new RectInt(x0,y0,x1-x0+1,y1-y0+1);}
            HasSelection=true;
        }
        public IEnumerable<int> Indices(RectInt rect)
        {for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)if(x>=0&&y>=0&&x<Document.level.width&&y<Document.level.height)yield return y*Document.level.width+x;}
        public SelectionStamp CopySelection()
        {
            if(!HasSelection)return null;
            var indices=Indices(Selection).ToArray();var included=indices.Select(i=>selectedCells==null||selectedCells.Contains(i)).ToArray();
            return new SelectionStamp{Width=Selection.width,Height=Selection.height,Included=included,Cells=indices.Select((i,n)=>included[n]?CloneCell(Document.level.grid[i]):new CellData{Objects=Array.Empty<CellObjectData>()}).ToArray()};
        }
        public bool CanPaste(SelectionStamp stamp,int x,int y,out string reason)
        {
            reason="";if(stamp==null)return false;
            if(x<0||y<0||x+stamp.Width>Document.level.width||y+stamp.Height>Document.level.height){reason="这一片超出了棋盘，移动一下再放下";return false;}
            foreach(var tone in new[]{Tone.White,Tone.Black})if(stamp.Cells.Sum(c=>c.Objects.Count(o=>o.ObjectType==ObjectKind.Player&&o.Color==tone))>1){reason="这一片有重复角色，先整理再摆放";return false;}
            return true;
        }
        public bool Paste(SelectionStamp stamp,int x,int y,bool move=false)
        {
            if(!CanPaste(stamp,x,y,out var reason)){LastMessage=reason;return false;}
            Begin();if(move&&HasSelection)foreach(int i in SelectedIndices)Document.level.grid[i].Objects=Array.Empty<CellObjectData>();
            foreach(var tone in new[]{Tone.White,Tone.Black})if(stamp.Cells.Any(c=>c.Objects.Any(o=>o.ObjectType==ObjectKind.Player&&o.Color==tone)))foreach(var c in Document.level.grid)c.Objects=c.Objects.Where(o=>o.ObjectType!=ObjectKind.Player||o.Color!=tone).ToArray();
            selectedCells=new HashSet<int>();
            for(int yy=0;yy<stamp.Height;yy++)for(int xx=0;xx<stamp.Width;xx++)if(stamp.Included==null||stamp.Included[yy*stamp.Width+xx]){int index=(y+yy)*Document.level.width+x+xx;Document.level.grid[index]=CloneCell(stamp.Cells[yy*stamp.Width+xx]);selectedCells.Add(index);}
            Selection=new RectInt(x,y,stamp.Width,stamp.Height);HasSelection=true;return Commit();
        }
        public LevelData PreviewResize(int width,int height,int anchor,out int cropped)
        {
            if(width<3||height<3||width>LevelValidation.MaxSide||height>LevelValidation.MaxSide)throw new ArgumentException("棋盘每边支持 3 到 64 格。");
            var old=Document.level;int dx=anchor%3==0?0:anchor%3==2?width-old.width:Mathf.FloorToInt((width-old.width)/2f);
            int row=2-anchor/3,dy=row==0?0:row==2?height-old.height:Mathf.FloorToInt((height-old.height)/2f);
            var result=LevelDocument.New(width,height).level;cropped=0;
            for(int y=0;y<old.height;y++)for(int x=0;x<old.width;x++){int nx=x+dx,ny=y+dy;var cell=old.grid[y*old.width+x];if(nx<0||ny<0||nx>=width||ny>=height)cropped+=cell.Objects.Length;else result.grid[ny*width+nx]=CloneCell(cell);}
            return result;
        }
        public void Resize(int width,int height,int anchor){var resized=PreviewResize(width,height,anchor,out _);Begin();Document.level=resized;SafeSelection();Zoom=1;Pan=Vector2.zero;Commit();}
    }
}
