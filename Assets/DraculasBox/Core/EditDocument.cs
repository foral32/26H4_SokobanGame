using System;
using System.Collections.Generic;
using System.Linq;

namespace DraculasBox.Core
{
    public enum Brush { Floor,Wall,Box,Player,Coffin,Lamp,Void,Erase,Plate,Door,MirrorSlash,MirrorBackslash }
    public sealed partial class EditDocument
    {
        private readonly Stack<LevelData> undo=new Stack<LevelData>(),redo=new Stack<LevelData>();
        private LevelData transaction;
        private LevelData savedData;private bool hasSaved;
        public LevelData Data {get;private set;}
        public int UndoCount=>undo.Count;
        public bool Dirty {get;private set;}
        public int RedoCount=>redo.Count;
        public EditDocument(LevelData data,bool saved=true){string error=Definition.DraftError(data);if(error!=null)throw new ArgumentException(error);Data=data.Copy();hasSaved=saved;savedData=saved?data.Copy():null;RefreshDirty();}
        private void RefreshDirty(){Dirty=!hasSaved||!Data.ContentEquals(savedData);}
        public void Begin(){if(transaction==null)transaction=Data.Copy();}
        public void End(bool cancel=false)
        {
            if(transaction==null)return;
            if(cancel)Data=transaction;
            else if(!Data.ContentEquals(transaction)){undo.Push(transaction);redo.Clear();}
            transaction=null;RefreshDirty();
        }
        public bool Paint(Cell p,Brush brush,int channel=0)
        {
            if(p.x<0||p.y<0||p.y>=Data.rows.Length||p.x>=Data.rows[0].Length)return false;
            bool device=Data.plates.Any(i=>i.cell==p)||Data.doors.Any(i=>i.cell==p);
            bool entity=Data.player==p||Data.coffin==p||Data.pieces.Any(i=>i.cell==p);
            if(brush==Brush.Wall||brush==Brush.Void||brush==Brush.Floor)
            {
                if((entity||device)&&brush!=Brush.Floor)return false;
                var row=Data.rows[p.y].ToCharArray();row[p.x]=brush==Brush.Wall?'#':brush==Brush.Void?' ':'.';Data.rows[p.y]=new string(row);return true;
            }
            if(brush==Brush.Erase)
            {
                if(Data.player==p)Data.player=new Cell(-1,-1);
                else if(Data.coffin==p)Data.coffin=new Cell(-1,-1);
                else if(Data.pieces.Any(i=>i.cell==p))Data.pieces=Data.pieces.Where(i=>i.cell!=p).ToArray();
                else if(Data.plates.Any(i=>i.cell==p))Data.plates=Data.plates.Where(i=>i.cell!=p).ToArray();
                else if(Data.doors.Any(i=>i.cell==p))Data.doors=Data.doors.Where(i=>i.cell!=p).ToArray();
                else {var row=Data.rows[p.y].ToCharArray();row[p.x]='.';Data.rows[p.y]=new string(row);}return true;
            }
            if(Data.rows[p.y][p.x]!='.')return false;
            if(brush==Brush.Plate||brush==Brush.Door)
            {
                if(channel<0||channel>3||Data.coffin==p)return false;
                var own=brush==Brush.Plate?Data.plates:Data.doors;var other=brush==Brush.Plate?Data.doors:Data.plates;
                if(other.Any(i=>i.cell==p))return false;var existing=own.FirstOrDefault(i=>i.cell==p);if(existing!=null){existing.channel=channel;return true;}if(own.Length>=32)return false;
                var added=own.Concat(new[]{new DeviceData{cell=p,channel=channel}}).ToArray();if(brush==Brush.Plate)Data.plates=added;else Data.doors=added;return true;
            }
            if(brush==Brush.Player){if(Data.coffin==p||Data.pieces.Any(i=>i.cell==p))return false;Data.player=p;return true;}
            if(brush==Brush.Coffin){if(device||Data.player==p||Data.pieces.Any(i=>i.cell==p))return false;Data.coffin=p;return true;}
            if(entity)return false;
            if(Data.pieces.Length>=128)return false;
            var kind=brush==Brush.Lamp?PieceKind.Lamp:brush==Brush.MirrorSlash?PieceKind.MirrorSlash:brush==Brush.MirrorBackslash?PieceKind.MirrorBackslash:PieceKind.Box;
            Data.pieces=Data.pieces.Concat(new[]{new EntityData{id=Guid.NewGuid().ToString("N"),kind=kind,cell=p}}).ToArray();return true;
        }
        public void ConfigureSun(bool enabled,SunSide from){Begin();Data.sunEnabled=enabled;Data.sunFrom=from;End();}
        public void Rename(string name){Begin();Data.title=string.IsNullOrWhiteSpace(name)?"我的关卡":name.Trim();End();}
        public bool Resize(int w,int h)
        {
            if(ResizeError(w,h)!=null)return false;
            Begin();var rows=new string[h];
            for(int y=0;y<h;y++){var row=new char[w];for(int x=0;x<w;x++)row[x]=y<Data.rows.Length&&x<Data.rows[0].Length?Data.rows[y][x]:'.';rows[y]=new string(row);}Data.rows=rows;End();return true;
        }
        public string ResizeError(int w,int h)
        {
            if(w<3||h<3||w>32||h>32)return "宽、高须为 3–32 格。";
            Func<Cell,bool> outside=p=>p.x>=w||p.y>=h;
            if(outside(Data.player))return "吸血鬼在裁切区域，请先移开。";
            if(outside(Data.coffin))return "棺材在裁切区域，请先移开。";
            var blocked=Data.pieces.FirstOrDefault(i=>outside(i.cell));if(blocked!=null)return (blocked.kind==PieceKind.Box?"箱子":blocked.kind==PieceKind.Lamp?"四向灯":"镜子")+"在裁切区域，请先移开。";
            if(Data.plates.Any(i=>outside(i.cell))||Data.doors.Any(i=>outside(i.cell)))return "机关在裁切区域，请先移开。";
            return null;
        }
        public bool Undo(){End();if(undo.Count==0)return false;redo.Push(Data.Copy());Data=undo.Pop();RefreshDirty();return true;}
        public bool Redo(){End();if(redo.Count==0)return false;undo.Push(Data.Copy());Data=redo.Pop();RefreshDirty();return true;}
        public void Saved(){hasSaved=true;savedData=Data.Copy();RefreshDirty();}
        public void SavedBaseline(LevelData baseline){hasSaved=baseline!=null;savedData=baseline?.Copy();RefreshDirty();}
        public string Validation(){try{new Definition(Data);return null;}catch(ArgumentException e){return e.Message;}}
    }
}
