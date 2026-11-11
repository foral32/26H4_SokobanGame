using UnityEngine;

namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static Sprite softCard,softCardShadow;
        public static Sprite SoftCard=>softCard!=null?softCard:(softCard=CardTexture(false));
        public static Sprite SoftCardShadow=>softCardShadow!=null?softCardShadow:(softCardShadow=CardTexture(true));
        private static Sprite CardTexture(bool shadow)
        {
            const int size=96;float radius=14,padding=shadow?12:0;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var q=new Vector2(Mathf.Abs(x-47.5f),Mathf.Abs(y-47.5f))-Vector2.one*(48-padding-radius);
                float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
                float alpha=shadow?Mathf.Exp(-Mathf.Pow(Mathf.Max(0,distance)/5.5f,2)*.5f):Mathf.Clamp01(.5f-distance);
                pixels[y*size+x]=new Color(1,1,1,alpha);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=shadow?"Card shadow":"Soft card",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
            texture.SetPixels(pixels);texture.Apply(false,true);
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*30);sprite.hideFlags=HideFlags.DontSave;return sprite;
        }
    }
}
