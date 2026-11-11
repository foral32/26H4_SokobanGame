using UnityEngine;

namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static Sprite sunlightHaze;
        public static Sprite SunlightHaze
        {
            get
            {
                if(sunlightHaze!=null)return sunlightHaze;
                const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Soft incoming sunlight",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};var pixels=new Color32[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1,r=Mathf.Sqrt(u*u+v*v);
                    float alpha=Mathf.Exp(-2.7f*r*r)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,r)));
                    pixels[y*size+x]=new Color(1,1,1,alpha);
                }
                texture.SetPixels32(pixels);texture.Apply(false,true);sunlightHaze=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,size);sunlightHaze.name="Soft incoming sunlight";sunlightHaze.hideFlags=HideFlags.DontSave;return sunlightHaze;
            }
        }
    }
}
