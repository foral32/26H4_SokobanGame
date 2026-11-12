using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private Sprite canvasGridSprite;private Texture2D canvasGridTexture;
        private int gridWidth,gridHeight;
        private SpriteRenderer linePreview;private SpriteRenderer[] expansionEdges;
        private void BuildEditorCanvas()
        {
            int width=state.Level.Width,height=state.Level.Height;
            if(canvasGridSprite==null||gridWidth!=width||gridHeight!=height)
            {
                if(canvasGridSprite!=null)ReleaseLightObject(canvasGridSprite);
                if(canvasGridTexture!=null)ReleaseLightObject(canvasGridTexture);
                gridWidth=width;gridHeight=height;
                const int padding=8,pixels=32;
                int w=(width+padding*2)*pixels,h=(height+padding*2)*pixels;
                var colors=new Color32[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    if(x%pixels!=0&&y%pixels!=0)continue;
                    float px=x/(float)pixels-padding,py=y/(float)pixels-padding;
                    float dx=Mathf.Max(0,Mathf.Max(-px,px-width)),dy=Mathf.Max(0,Mathf.Max(-py,py-height));
                    float distance=Mathf.Sqrt(dx*dx+dy*dy);
                    byte alpha=(byte)(38*Mathf.Pow(Mathf.Clamp01(1-distance/7),2));
                    colors[y*w+x]=new Color32(255,255,255,alpha);
                }
                canvasGridTexture=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Editor fading grid",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
                canvasGridTexture.SetPixels32(colors);canvasGridTexture.Apply(false,true);
                canvasGridSprite=Sprite.Create(canvasGridTexture,new Rect(0,0,w,h),Vector2.one*.5f,pixels,0,SpriteMeshType.FullRect);
                canvasGridSprite.hideFlags=HideFlags.DontSave;
            }
            var grid=MakeSprite("Canvas grid",canvasGridSprite,-80,Vector3.zero,1);grid.color=GameUI.WorkshopInk;
            linePreview=Make("Row column preview","solid",38,Vector3.zero,1);linePreview.enabled=false;
            expansionEdges=new SpriteRenderer[4];
            for(int i=0;i<4;i++){expansionEdges[i]=Make("Expansion edge "+i,"solid",38,Vector3.zero,1);expansionEdges[i].enabled=false;}
        }
        public void LinePreview(bool column,int index,bool insert)
        {
            if(linePreview==null)return;
            linePreview.enabled=editing&&index>=0;if(!linePreview.enabled)return;
            var d=state.Level;float thickness=Mathf.Clamp(unitsPerUI*2/editorZoom,.025f,.13f);
            var color=insert?GameUI.Gold:(Color)new Color32(186,76,93,255);
            linePreview.color=new Color(color.r,color.g,color.b,insert?.9f:.23f);
            linePreview.transform.localPosition=column?new Vector3(index-(d.Width-1)*.5f-(insert?.5f:0),0,0):new Vector3(0,(d.Height-1)*.5f-index+(insert?.5f:0),0);
            linePreview.transform.localScale=column?new Vector3(insert?thickness:1,d.Height,1):new Vector3(d.Width,insert?thickness:1,1);
        }
        public void ExpansionPreview(Cell? cell,bool valid)
        {
            if(expansionEdges==null)return;
            bool show=editing&&cell.HasValue&&state!=null&&!state.Level.Inside(cell.Value);
            foreach(var edge in expansionEdges)edge.enabled=show;if(!show)return;
            var p=cell.Value;var a=new Cell(Mathf.Min(0,p.x),Mathf.Min(0,p.y));
            var b=new Cell(Mathf.Max(state.Level.Width-1,p.x),Mathf.Max(state.Level.Height-1,p.y));
            float width=b.x-a.x+1,height=b.y-a.y+1;var center=(Position(a)+Position(b))*.5f;
            float thickness=Mathf.Clamp(unitsPerUI*1.4f/editorZoom,.018f,.1f);
            var color=valid?GameUI.WorkshopAccent:(Color)new Color32(186,76,93,255);color.a=.65f;
            for(int i=0;i<4;i++)
            {
                var edge=expansionEdges[i];bool horizontal=i<2;edge.color=color;
                edge.transform.localPosition=center+(horizontal?Vector3.up*(i==0?1:-1)*height*.5f:Vector3.right*(i==2?1:-1)*width*.5f);
                edge.transform.localScale=horizontal?new Vector3(width,thickness,1):new Vector3(thickness,height,1);
            }
        }
        private void ClearCanvasResources(){if(canvasGridSprite!=null)ReleaseLightObject(canvasGridSprite);if(canvasGridTexture!=null)ReleaseLightObject(canvasGridTexture);}
    }
}
