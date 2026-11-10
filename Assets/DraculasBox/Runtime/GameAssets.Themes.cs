using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class GameAssets
    {
        internal Sprite ThemedSprite(string name,Sprite source)
        {
            if(GameThemes.IsClassic||source==null||!(name.StartsWith("Player_active_white_")||name=="Box_active_white"||name=="vampire"||name=="cape"||name=="coffin-open"||name=="coffin-lid"||name=="lamp"))return source;
            var theme=GameThemes.Current;string key=theme.Id+"/"+name+"/"+source.GetInstanceID();
            if(themedSprites.TryGetValue(key,out var cached)&&cached!=null)return cached;
            // Imported artwork can be non-readable. Copy only this sprite region;
            // preserve alpha, pivot and silhouette, never change the source texture.
            var texture=source.texture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
            Texture2D copy=null;
            try
            {
                Graphics.Blit(texture,rt);RenderTexture.active=rt;var rect=source.textureRect;
                copy=new Texture2D((int)rect.width,(int)rect.height,TextureFormat.RGBA32,false){name=name+" "+theme.Id,filterMode=texture.filterMode,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
                copy.ReadPixels(rect,0,0);var pixels=copy.GetPixels();
                for(int i=0;i<pixels.Length;i++)pixels[i]=RecolorPixel(name,pixels[i],theme);
                copy.SetPixels(pixels);copy.Apply(false,true);
                var sprite=UnityEngine.Sprite.Create(copy,new Rect(0,0,copy.width,copy.height),source.pivot/source.rect.size,source.pixelsPerUnit,0,SpriteMeshType.FullRect,source.border);
                sprite.name=source.name+" "+theme.Id;sprite.hideFlags=HideFlags.DontSave;themedSprites[key]=sprite;return sprite;
            }
            catch{if(copy!=null)Destroy(copy);throw;}
            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
        }
        private static Color RecolorPixel(string name,Color pixel,GameTheme theme)
        {
            if(pixel.a==0)return pixel;float alpha=pixel.a;Color result=pixel;
            if(name=="cape"||name=="vampire"&&pixel.r>pixel.g*1.12f&&pixel.b>pixel.g*.95f&&pixel.r<.8f)result=theme.Cape;
            else if(name=="coffin-open"||name=="coffin-lid")
            {
                float r=pixel.r*255;
                result=r>215?theme.ObjectLight:r>181?theme.CoffinLight:r>125?theme.CoffinRim:name=="coffin-open"?theme.CoffinInside:theme.CoffinFace;
            }
            else if(name=="lamp")
            {
                // Keep the golden core and four bright ports, recolor only metal.
                if(pixel.b>=pixel.r*.98f)result=Color.Lerp(theme.WallDark,theme.WallLight,Mathf.Clamp01((pixel.grayscale-.27f)/.49f));
            }
            else if(name=="Box_active_white")
            {
                float value=pixel.grayscale;result=value<.65f?Color.Lerp(theme.ObjectShade*.60f,theme.ObjectShade,Mathf.Clamp01((value-.30f)/.35f)):Color.Lerp(theme.ObjectShade,theme.ObjectLight,Mathf.Clamp01((value-.65f)/.33f));
            }
            else if(pixel.grayscale>.45f)result=Color.Lerp(theme.ObjectShade,theme.ObjectLight,Mathf.Clamp01((pixel.grayscale-.65f)/.35f));
            result.a=alpha;return result;
        }
    }
}
