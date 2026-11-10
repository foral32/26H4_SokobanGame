using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private SpriteRenderer regionFill;private SpriteRenderer[] regionEdges;
        private void BuildSunIndicator()
        {
            var travel=LightField.SunTravel[(int)state.SunFrom];var direction=new Vector3(travel.x,-travel.y,0);bool horizontal=travel.x!=0;
            float extent=(horizontal?state.Level.Width:state.Level.Height)*.5f;
            var edge=-direction*extent;var rotation=SunRotation(state.SunFrom);
            var sun=MakeSprite("Incoming sunlight",SurfaceSprites.SunSource,-16,edge-direction*57*unitsPerUI,42*unitsPerUI);sun.color=new Color32(245,211,142,255);
            var arrow=MakeSprite("Sun direction",SurfaceSprites.SourceArrow,-16,edge-direction*18*unitsPerUI,28*unitsPerUI);arrow.transform.localRotation=rotation;arrow.color=new Color32(245,211,142,255);
        }
        private void BuildRegionPreview()
        {
            regionFill=Make("Rectangle preview","solid",36,Vector3.zero,1);regionFill.enabled=false;regionEdges=new SpriteRenderer[4];
            for(int i=0;i<4;i++){regionEdges[i]=Make("Rectangle edge "+i,"solid",37,Vector3.zero,1);regionEdges[i].enabled=false;}
        }
        public void RegionPreview(Cell? start,Cell? end,bool erase=false)
        {
            bool visible=editing&&state!=null&&start.HasValue&&end.HasValue;
            if(regionFill==null)return;regionFill.enabled=visible;foreach(var edge in regionEdges)edge.enabled=visible;if(!visible)return;
            var a=start.Value;var b=end.Value;float width=Mathf.Abs(a.x-b.x)+1,height=Mathf.Abs(a.y-b.y)+1;var center=(Position(a)+Position(b))*.5f;
            var color=GameUI.WorkshopAccent;SetSprite(regionFill,erase?SurfaceSprites.Hatch:assets.Sprite("solid"));regionFill.drawMode=erase?SpriteDrawMode.Tiled:SpriteDrawMode.Simple;regionFill.size=new Vector2(width,height);regionFill.color=new Color(color.r,color.g,color.b,erase?.30f:.12f);regionFill.transform.localPosition=center;regionFill.transform.localScale=erase?Vector3.one:new Vector3(width,height,1);
            float line=Mathf.Clamp(unitsPerUI*1.8f,.014f,.12f);
            for(int i=0;i<4;i++){var edge=regionEdges[i];edge.color=color;bool horizontal=i<2;edge.transform.localPosition=center+(horizontal?Vector3.up*(i==0?1:-1)*(height-line)*.5f:Vector3.right*(i==2?1:-1)*(width-line)*.5f);edge.transform.localScale=horizontal?new Vector3(width,line,1):new Vector3(line,height,1);}
        }
    }
}
