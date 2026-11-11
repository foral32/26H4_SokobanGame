using UnityEngine;
using DraculasBox.Core;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private SpriteRenderer[] plateFaces,plateRims,doorLeft,doorRight,doorMarks;
        public int EditorChannel {get;set;}
        public static readonly Color[] CircuitColors={new Color32(119,198,207,255),new Color32(185,147,199,255),new Color32(223,168,112,255),new Color32(153,192,139,255)};
        private SpriteRenderer RectSprite(string name,Cell cell,Vector2 offset,Vector2 size,int order,Color color)
        {var r=Make(name,"solid",order,Position(cell)+(Vector3)offset,1);r.transform.localScale=new Vector3(size.x,size.y,1);r.color=color;return r;}
        private void Rivets(string name,Cell cell,int channel,float y,int order)
        {for(int i=0;i<=channel;i++)RectSprite(name,cell,new Vector2((i-channel*.5f)*.12f,y),new Vector2(.06f,.06f),order,CircuitColors[channel]);}
        private void BuildDevices()
        {
            var d=state.Level;plateFaces=new SpriteRenderer[d.PlateCount];plateRims=new SpriteRenderer[d.PlateCount];doorLeft=new SpriteRenderer[d.DoorCount];doorRight=new SpriteRenderer[d.DoorCount];doorMarks=new SpriteRenderer[d.DoorCount];
            plateVisualStarts=new int[d.PlateCount];plateVisualCounts=new int[d.PlateCount];doorVisualStarts=new int[d.DoorCount];doorVisualCounts=new int[d.DoorCount];
            for(int i=0;i<d.PlateCount;i++)
            {
                plateVisualStarts[i]=renderers.Count;
                var p=d.PlateCell(i);var color=CircuitColors[d.PlateChannel(i)];
                RectSprite("Plate base "+i,p,new Vector2(.025f,-.035f),Vector2.one*.94f,-17,(GameThemes.IsClassic?new Color32(27,34,44,255):GameThemes.Current.WallFoot));
                plateRims[i]=RectSprite("Plate rim "+i,p,Vector2.zero,Vector2.one*.92f,-16,color);
                plateFaces[i]=RectSprite("Plate face "+i,p,Vector2.zero,Vector2.one*.78f,-15,(GameThemes.IsClassic?new Color32(65,82,94,255):GameThemes.Current.WallFront));
                Rivets("Plate group "+i,p,d.PlateChannel(i),-.36f,16);
                plateVisualCounts[i]=renderers.Count-plateVisualStarts[i];
            }
            for(int i=0;i<d.DoorCount;i++)
            {
                doorVisualStarts[i]=renderers.Count;
                var p=d.DoorCell(i);var color=CircuitColors[d.DoorChannel(i)];
                // Leave the opening transparent so its floor and dangerous light remain visible.
                RectSprite("Door top frame "+i,p,new Vector2(0,.46f),new Vector2(.98f,.06f),1,(GameThemes.IsClassic?new Color32(31,41,51,255):GameThemes.Current.WallFoot));
                RectSprite("Door bottom frame "+i,p,new Vector2(0,-.46f),new Vector2(.98f,.06f),1,(GameThemes.IsClassic?new Color32(31,41,51,255):GameThemes.Current.WallFoot));
                for(int side=-1;side<=1;side+=2){RectSprite("Door rail "+i,p,new Vector2(side*.45f,0),new Vector2(.10f,.98f),4,color);RectSprite("Door rail highlight "+i,p,new Vector2(side*.45f+.017f,.01f),new Vector2(.025f,.90f),5,new Color(1,1,1,.32f));}
                doorLeft[i]=RectSprite("Door left "+i,p,new Vector2(-.21f,0),new Vector2(.40f,.88f),3,(GameThemes.IsClassic?new Color32(110,128,140,255):GameThemes.Current.WallTop));
                doorRight[i]=RectSprite("Door right "+i,p,new Vector2(.21f,0),new Vector2(.40f,.88f),3,(GameThemes.IsClassic?new Color32(88,107,120,255):GameThemes.Current.WallFront));
                doorMarks[i]=RectSprite("Door seam "+i,p,Vector2.zero,new Vector2(.04f,.82f),4,color);
                Rivets("Door group "+i,p,d.DoorChannel(i),-.39f,16);
                doorVisualCounts[i]=renderers.Count-doorVisualStarts[i];
            }
        }
        private void BlendDevices(float t)
        {
            var d=state.Level;
            for(int i=0;i<d.PlateCount;i++)
            {
                float down=Mathf.Lerp(previous.PlatePressed(i)?1:0,state.PlatePressed(i)?1:0,t);var color=CircuitColors[d.PlateChannel(i)];
                plateFaces[i].transform.localScale=Vector3.one*Mathf.Lerp(.78f,.75f,down);plateFaces[i].color=Color.Lerp((GameThemes.IsClassic?new Color32(65,82,94,255):GameThemes.Current.WallFront),color*.72f,down);plateFaces[i].color=new Color(plateFaces[i].color.r,plateFaces[i].color.g,plateFaces[i].color.b,1);plateRims[i].color=Color.Lerp(color*.65f,color,down);plateRims[i].color=new Color(plateRims[i].color.r,plateRims[i].color.g,plateRims[i].color.b,1);
            }
            for(int i=0;i<d.DoorCount;i++)
            {
                float open=Mathf.Lerp(previous.DoorOpen(i)?1:0,state.DoorOpen(i)?1:0,t);var pos=Position(d.DoorCell(i));float x=Mathf.Lerp(.21f,.385f,open),width=Mathf.Lerp(.40f,.07f,open);
                doorLeft[i].transform.localPosition=pos+Vector3.left*x;doorRight[i].transform.localPosition=pos+Vector3.right*x;doorLeft[i].transform.localScale=doorRight[i].transform.localScale=new Vector3(width,.88f,1);doorMarks[i].color=new Color(CircuitColors[d.DoorChannel(i)].r,CircuitColors[d.DoorChannel(i)].g,CircuitColors[d.DoorChannel(i)].b,1-open);
            }
        }
        public void DeviceGhost(Brush brush,bool valid)
        {if(ghost==null||brush!=Brush.Plate&&brush!=Brush.Door)return;SetSprite(ghost,SurfaceSprites.DeviceIcon(brush==Brush.Door));if(valid)ghost.color=new Color(CircuitColors[EditorChannel].r,CircuitColors[EditorChannel].g,CircuitColors[EditorChannel].b,.65f);}
    }
}
