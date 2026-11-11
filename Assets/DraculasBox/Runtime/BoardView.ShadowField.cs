using UnityEngine;
using UnityEngine.Rendering;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private Material shadowMaskMaterial,shadowBlurMaterial;
        private RenderTexture shadowRaw,shadowScratch,shadowSoft;
        private CommandBuffer shadowCommands;
        private int shadowFieldUpdates;
        private void EnsureShadowField()
        {
            if(shadowMaskMaterial==null)
            {
                shadowMaskMaterial=new Material(shadeMaterial.shader){name="Solar mask merge",hideFlags=HideFlags.DontSave};
                var blur=Resources.Load<Shader>("Shaders/ShadowBlur");
                if(blur==null||!blur.isSupported)throw new System.InvalidOperationException("Shadow blur shader is unavailable.");
                shadowBlurMaterial=new Material(blur){name="Solar field filter",hideFlags=HideFlags.DontSave};
                shadowCommands=new CommandBuffer{name="Merge solar shadows"};
            }
            int width=state.Level.Width*32,height=state.Level.Height*32;
            if(shadowRaw==null||shadowRaw.width!=width||shadowRaw.height!=height)
            {
                ReleaseShadowTexture(shadowRaw);ReleaseShadowTexture(shadowScratch);ReleaseShadowTexture(shadowSoft);
                shadowRaw=ShadowTexture("Solar raw union",width,height);shadowScratch=ShadowTexture("Solar horizontal filter",width,height);shadowSoft=ShadowTexture("Solar filtered union",width,height);
            }
            if(!shadowRaw.IsCreated())shadowRaw.Create();if(!shadowScratch.IsCreated())shadowScratch.Create();if(!shadowSoft.IsCreated())shadowSoft.Create();
            shadowMaskMaterial.SetVector("_Size",new Vector4(state.Level.Width,state.Level.Height,0,0));
            shadeMaterial.SetTexture("_RawShadow",shadowRaw);shadeMaterial.SetTexture("_SoftShadow",shadowSoft);
        }
        private bool ShadowFieldReady()=>shadowRaw!=null&&shadowScratch!=null&&shadowSoft!=null&&shadowRaw.IsCreated()&&shadowScratch.IsCreated()&&shadowSoft.IsCreated();
        private static RenderTexture ShadowTexture(string name,int width,int height)
        {
            var texture=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){name=name,hideFlags=HideFlags.DontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};texture.Create();return texture;
        }
        private void RenderShadowField()
        {
            var prior=RenderTexture.active;
            try
            {
                shadowCommands.Clear();shadowCommands.SetRenderTarget(shadowRaw);shadowCommands.ClearRenderTarget(false,true,Color.clear);
                if(sunTerrainMesh.vertexCount>0)shadowCommands.DrawMesh(sunTerrainMesh,Matrix4x4.identity,shadowMaskMaterial,0,1);
                if(sunObjectsMesh.vertexCount>0)shadowCommands.DrawMesh(sunObjectsMesh,Matrix4x4.identity,shadowMaskMaterial,0,1);
                Graphics.ExecuteCommandBuffer(shadowCommands);
                // Filter the merged field, never each caster independently. Green is
                // geometric coverage; blue fades doors without a last-frame contour pop.
                shadowBlurMaterial.SetVector("_BlurStep",new Vector4(2f/shadowRaw.width,0,0,0));Graphics.Blit(shadowRaw,shadowScratch,shadowBlurMaterial);
                shadowBlurMaterial.SetVector("_BlurStep",new Vector4(0,2f/shadowRaw.height,0,0));Graphics.Blit(shadowScratch,shadowSoft,shadowBlurMaterial);
                shadowFieldUpdates++;
            }
            finally{RenderTexture.active=prior;}
        }
        private static void ReleaseShadowTexture(RenderTexture texture)
        {if(texture==null)return;texture.Release();ReleaseLightObject(texture);}
        private void ReleaseShadowField()
        {
            ReleaseShadowTexture(shadowRaw);ReleaseShadowTexture(shadowScratch);ReleaseShadowTexture(shadowSoft);
            ReleaseLightObject(shadowMaskMaterial);ReleaseLightObject(shadowBlurMaterial);shadowCommands?.Release();shadowCommands=null;
        }
    }
}
