using DraculasBox.Core;
using UnityEngine;
using UnityEngine.UI;
namespace DraculasBox.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LevelThumbnail:MaskableGraphic
    {
        private LevelData data;private LightField field;private State preview;
        private static Texture2D atlas;private static Rect[] art;private static int atlasTheme=-1;
        private static readonly Texture2D[] themeAtlases=new Texture2D[5];private static readonly Rect[][] themeRects=new Rect[5][];
        public override Texture mainTexture=>atlas!=null?atlas:Texture2D.whiteTexture;
        public void Configure(GameAssets assets)
        {
            int index=GameThemes.Current.Index;if(atlas!=null&&atlasTheme==index)return;if(themeAtlases[index]!=null){atlas=themeAtlases[index];art=themeRects[index];atlasTheme=index;return;}var images=new Texture2D[8];images[0]=new Texture2D(2,2,TextureFormat.RGBA32,false);images[0].SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});images[0].Apply();
            string[] names={"vampire","Box_active_white","coffin-open","lamp","cape","mirror-slash","mirror-backslash"};
            for(int i=0;i<names.Length;i++)
            {
                var sprite=assets.Sprite(names[i]);var source=sprite.texture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
                try{Graphics.Blit(source,rt);RenderTexture.active=rt;var rect=sprite.textureRect;var image=new Texture2D((int)rect.width,(int)rect.height,TextureFormat.RGBA32,false);image.ReadPixels(rect,0,0);image.Apply();images[i+1]=image;}
                finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
            }
            atlas=new Texture2D(1024,1024,TextureFormat.RGBA32,false){name="Level thumbnail art "+index,hideFlags=HideFlags.DontSave};art=atlas.PackTextures(images,3,1024,false);atlas.filterMode=FilterMode.Bilinear;atlas.wrapMode=TextureWrapMode.Clamp;foreach(var image in images)Destroy(image);themeAtlases[index]=atlas;themeRects[index]=art;atlasTheme=index;
        }
        public void Set(LevelData level){data=level?.Copy();preview=data==null?null:Definition.Preview(data).Initial();field=preview==null?null:LightField.Compute(preview);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(data==null)return;var rect=GetPixelAdjustedRect();int h=data.rows.Length,w=data.rows[0].Length;float cell=Mathf.Min((rect.width-12)/w,(rect.height-12)/h);var origin=rect.center+new Vector2(-w,h)*cell*.5f;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                char tile=data.rows[y][x];if(tile==' ')continue;var r=new Rect(origin.x+x*cell,origin.y-(y+1)*cell,cell-.5f,cell-.5f);var p=new Cell(x,y);
                if(tile=='#')
                {
                    r.width=r.height=cell;Quad(vh,r,GameThemes.Current.WallTop,0);float line=Mathf.Max(.65f,cell*.055f);
                    if(y==0||data.rows[y-1][x]!='#')Quad(vh,new Rect(r.xMin,r.yMax-line,cell,line),GameThemes.Current.WallLight,0);
                    if(y==h-1||data.rows[y+1][x]!='#')Quad(vh,new Rect(r.xMin,r.yMin,cell,cell*.19f),GameThemes.Current.WallFront,0);
                    if(x==0||data.rows[y][x-1]!='#')Quad(vh,new Rect(r.xMin,r.yMin,line,cell),GameThemes.Current.WallLight,0);
                    if(x==w-1||data.rows[y][x+1]!='#')Quad(vh,new Rect(r.xMax-line,r.yMin,line,cell),GameThemes.Current.WallDark,0);
                }
                else Quad(vh,r,field.Lit(p)?BoardView.LitColor:BoardView.SafeColor,0);
                for(int i=0;i<data.plates.Length;i++)if(data.plates[i].cell==p){Quad(vh,Inset(r,.08f),BoardView.CircuitColors[data.plates[i].channel],0);Quad(vh,Inset(r,.20f),preview.PlatePressed(i)?BoardView.CircuitColors[data.plates[i].channel]*.75f:BoardView.SafeColor,0);}
                for(int i=0;i<data.doors.Length;i++)if(data.doors[i].cell==p){var tint=BoardView.CircuitColors[data.doors[i].channel];Quad(vh,Inset(r,.03f),tint,0);Quad(vh,Inset(r,.17f),preview.DoorOpen(i)?BoardView.SafeColor:GameThemes.IsClassic?new Color32(110,128,140,255):GameThemes.Current.WallTop,0);}
                if(data.coffin==p)Quad(vh,Inset(r,.08f),Color.white,3);
                foreach(var entity in data.pieces)if(entity.cell==p)Quad(vh,Inset(r,.08f),Color.white,entity.kind==PieceKind.Box?2:entity.kind==PieceKind.Lamp?4:entity.kind==PieceKind.MirrorSlash?6:7);
                if(data.player==p)Quad(vh,Inset(r,.05f),Color.white,1);
            }
        }
        private static void Quad(VertexHelper mesh,Rect rect,Color color,int icon)
        {
            var uv=art==null?new Rect(0,0,1,1):art[icon];if(icon==0)uv=new Rect(uv.center,Vector2.zero);int i=mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin,rect.yMin),color,new Vector2(uv.xMin,uv.yMin));mesh.AddVert(new Vector3(rect.xMin,rect.yMax),color,new Vector2(uv.xMin,uv.yMax));mesh.AddVert(new Vector3(rect.xMax,rect.yMax),color,new Vector2(uv.xMax,uv.yMax));mesh.AddVert(new Vector3(rect.xMax,rect.yMin),color,new Vector2(uv.xMax,uv.yMin));mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
        private static Rect Inset(Rect r,float a)=>new Rect(r.x+r.width*a,r.y+r.height*a,r.width*(1-a*2),r.height*(1-a*2));
    }
}
