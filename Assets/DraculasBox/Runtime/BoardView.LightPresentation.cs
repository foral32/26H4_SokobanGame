using System.Collections.Generic;
using DraculasBox.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private Transform lightRoot;
        private Mesh sunTerrainMesh,sunObjectsMesh,lampFieldMesh;
        private MeshRenderer sunTerrainRenderer,lampFieldRenderer;
        private Material shadeMaterial,beamMaterial;
        private Texture2D fieldBefore,fieldAfter;
        private Color32[] fieldPixels;
        private readonly List<Vector3> shadeVertices=new List<Vector3>();
        private readonly List<Vector2> shadeUV=new List<Vector2>();
        private readonly List<int> shadeIndices=new List<int>();
        private Vector3[] casterPositions;
        private float[] casterWeights;
        private bool castersDirty;
        private SpriteRenderer sunlightIcon,sunlightHaze;
        private Color dayFloor;
        private static readonly int LightBlend=Shader.PropertyToID("_LightBlend");
        private int lightMeshUploads;

        private void BuildLightPresentation()
        {
            if(lightRoot==null)
            {
                var shader=Resources.Load<Shader>("Shaders/BoardLight");
                if(shader==null||!shader.isSupported)throw new System.InvalidOperationException("Board lighting shader is unavailable.");
                lightRoot=new GameObject("Light presentation").transform;lightRoot.SetParent(boardRoot,false);
                shadeMaterial=new Material(shader){name="Board solar shade",hideFlags=HideFlags.DontSave};
                beamMaterial=new Material(shader){name="Board lamp beams",hideFlags=HideFlags.DontSave};shadeMaterial.SetFloat("_BlendOp",(float)BlendOp.Min);
                beamMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);beamMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);beamMaterial.SetFloat("_BlendOp",(float)BlendOp.Add);beamMaterial.SetFloat("_Mode",1);
                lampFieldMesh=LightMesh("Cardinal lamp shafts",beamMaterial,-18,out lampFieldRenderer);
                var shade=new GameObject("Unified solar shade",typeof(MeshFilter),typeof(MeshRenderer));shade.transform.SetParent(lightRoot,false);
                shade.GetComponent<MeshFilter>().sharedMesh=lampFieldMesh;sunTerrainRenderer=shade.GetComponent<MeshRenderer>();sunTerrainRenderer.sharedMaterial=shadeMaterial;sunTerrainRenderer.sortingOrder=-19;sunTerrainRenderer.shadowCastingMode=ShadowCastingMode.Off;sunTerrainRenderer.receiveShadows=false;
                sunTerrainMesh=new Mesh{name="Terrain solar shade",hideFlags=HideFlags.DontSave};sunTerrainMesh.MarkDynamic();
                sunObjectsMesh=new Mesh{name="Moving solar shade",hideFlags=HideFlags.DontSave};sunObjectsMesh.MarkDynamic();
            }
            lightRoot.gameObject.SetActive(true);
            var d=state.Level;
            if(fieldAfter==null||fieldAfter.width!=d.Width||fieldAfter.height!=d.Height)
            {
                ReleaseLightObject(fieldBefore);ReleaseLightObject(fieldAfter);
                fieldBefore=LightFieldTexture(d.Width,d.Height);fieldAfter=LightFieldTexture(d.Width,d.Height);
                fieldPixels=new Color32[d.Width*d.Height];
            }
            dayFloor=LitColor*new Color(1.05f,.93f,.88f,1);
            BindLightMaterials();
            BeginShadeMesh();
            if(state.SunEnabled)
            {
                for(int y=0;y<d.Height;y++)for(int x=0;x<d.Width;x++)if(d.Terrain(new Cell(x,y))=='#')AddSunCaster(Position(new Cell(x,y)),1);
            }
            else AddBoardQuad();
            UploadShadeMesh(sunTerrainMesh);
            BeginShadeMesh();AddBoardQuad();UploadShadeMesh(lampFieldMesh);
            casterPositions=new Vector3[d.PieceCount+d.DoorCount];casterWeights=new float[casterPositions.Length];castersDirty=true;
            if(state.SunEnabled)EnsureShadowField();
        }
        private void BindLightMaterials()
        {
            // These bindings also apply when a board is rebuilt at the same size.
            // Reusing a texture does not imply its material slot survived an editor refresh.
            shadeMaterial.SetTexture("_Before",fieldBefore);shadeMaterial.SetTexture("_After",fieldAfter);
            beamMaterial.SetTexture("_Before",fieldBefore);beamMaterial.SetTexture("_After",fieldAfter);
            beamMaterial.SetFloat("_Mode",1);
            // Porcelain's pale menu is not a suitable shadow pigment. Keep its blue safe-floor ink.
            Color shadeTint=GameThemes.Current.LightMenu?SafeColor:GameThemes.Current.Background;
            shadeMaterial.SetTexture("_FloorTex",SurfaceSprites.Floor.texture);shadeMaterial.SetColor("_BaseTint",dayFloor);
            shadeMaterial.SetColor("_ShadeTint",state.SunEnabled?shadeTint:SafeColor);shadeMaterial.SetFloat("_Mode",state.SunEnabled?0:2);
            var size=new Vector4(state.Level.Width,state.Level.Height,0,0);shadeMaterial.SetVector("_Size",size);beamMaterial.SetVector("_Size",size);
        }
        private bool LightBindingsReady()
        {
            var size=new Vector4(state.Level.Width,state.Level.Height,0,0);
            return shadeMaterial.GetVector("_Size")==size&&beamMaterial.GetVector("_Size")==size&&
                shadeMaterial.GetFloat("_Mode")== (state.SunEnabled?0:2)&&beamMaterial.GetFloat("_Mode")==1&&
                shadeMaterial.GetTexture("_Before")==fieldBefore&&shadeMaterial.GetTexture("_After")==fieldAfter&&
                beamMaterial.GetTexture("_Before")==fieldBefore&&beamMaterial.GetTexture("_After")==fieldAfter&&
                (!state.SunEnabled||shadeMaterial.GetTexture("_RawShadow")==shadowRaw&&shadeMaterial.GetTexture("_SoftShadow")==shadowSoft);
        }
        private Mesh LightMesh(string name,Material material,int order,out MeshRenderer renderer)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(lightRoot,false);
            var mesh=new Mesh{name=name,hideFlags=HideFlags.DontSave};mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=mesh;
            renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            return mesh;
        }
        private static Texture2D LightFieldTexture(int width,int height)
        {return new Texture2D(width,height,TextureFormat.RGBA32,false,true){name="Board light cells",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};}
        private void UploadLightField(Texture2D texture,LightField light)
        {
            var d=state.Level;
            for(int y=0;y<d.Height;y++)for(int x=0;x<d.Width;x++)
            {
                int i=y*d.Width+x;fieldPixels[(d.Height-1-y)*d.Width+x]=new Color32(d.Floor(new Cell(x,y))?(byte)255:(byte)0,light.Lamp[i]?(byte)255:(byte)0,(light.LampAxes[i]&1)!=0?(byte)255:(byte)0,(light.LampAxes[i]&2)!=0?(byte)255:(byte)0);
            }
            texture.SetPixels32(fieldPixels);texture.Apply(false,false);
        }
        private void UpdateLightFields(LightField current)
        {
            UploadLightField(fieldAfter,current);
            UploadLightField(fieldBefore,ReferenceEquals(state,previous)?current:LightField.Compute(previous));
        }
        private void BeginShadeMesh(){shadeVertices.Clear();shadeUV.Clear();shadeIndices.Clear();}
        private void AddShadeQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float length,float weight)
        {
            int i=shadeVertices.Count;shadeVertices.Add(a);shadeVertices.Add(b);shadeVertices.Add(c);shadeVertices.Add(d);
            shadeUV.Add(new Vector2(0,weight));shadeUV.Add(new Vector2(0,weight));shadeUV.Add(new Vector2(length,weight));shadeUV.Add(new Vector2(length,weight));
            shadeIndices.Add(i);shadeIndices.Add(i+1);shadeIndices.Add(i+2);shadeIndices.Add(i);shadeIndices.Add(i+2);shadeIndices.Add(i+3);
        }
        private void AddBoardQuad()
        {
            float x=state.Level.Width*.5f,y=state.Level.Height*.5f;
            AddShadeQuad(new Vector3(-x,-y),new Vector3(-x,y),new Vector3(x,y),new Vector3(x,-y),1,1);
        }
        private void AddSunCaster(Vector3 center,float weight)
        {
            if(weight<=0)return;var cell=LightField.SunTravel[(int)state.SunFrom];var dir=new Vector3(cell.x,-cell.y,0);var across=new Vector3(-dir.y,dir.x,0)*.5f;
            var back=center-dir*.5f;var tip=back+dir*3;
            AddShadeQuad(back-across,back+across,tip+across,tip-across,3,weight);
        }
        private void UploadShadeMesh(Mesh mesh)
        {mesh.Clear();mesh.SetVertices(shadeVertices);mesh.SetUVs(0,shadeUV);mesh.SetTriangles(shadeIndices,0);mesh.RecalculateBounds();lightMeshUploads++;}
        private void RefreshLightPresentation(float travel)
        {
            if(lightRoot==null)return;
            if(!LightBindingsReady())
            {
                BindLightMaterials();
                if(state.SunEnabled){EnsureShadowField();castersDirty=true;}
            }
            shadeMaterial.SetFloat(LightBlend,travel);beamMaterial.SetFloat(LightBlend,travel);
            var rect=new Vector4(normalFrame.x-EditorViewWidth*.5f*unitsPerUI,normalFrame.y-EditorHeight*.5f*unitsPerUI,normalFrame.x+EditorViewWidth*.5f*unitsPerUI,normalFrame.y+EditorHeight*.5f*unitsPerUI);
            shadeMaterial.SetFloat("_ClipEnabled",editing?1:0);beamMaterial.SetFloat("_ClipEnabled",editing?1:0);shadeMaterial.SetVector("_ClipRect",rect);beamMaterial.SetVector("_ClipRect",rect);
            if(state.SunEnabled)
            {
                if(!ShadowFieldReady()){EnsureShadowField();castersDirty=true;}
                bool changed=castersDirty;var d=state.Level;
                for(int i=0;i<casterPositions.Length;i++)
                {
                    var pos=i<d.PieceCount?pieces[i].transform.localPosition:Position(d.DoorCell(i-d.PieceCount));
                    float weight=i<d.PieceCount?1:1-Mathf.Lerp(previous.DoorOpen(i-d.PieceCount)?1:0,state.DoorOpen(i-d.PieceCount)?1:0,travel);
                    if(casterPositions[i]!=pos||!Mathf.Approximately(casterWeights[i],weight))changed=true;
                    casterPositions[i]=pos;casterWeights[i]=weight;
                }
                if(changed){BeginShadeMesh();for(int i=0;i<casterPositions.Length;i++)AddSunCaster(casterPositions[i],casterWeights[i]);UploadShadeMesh(sunObjectsMesh);RenderShadowField();castersDirty=false;}
                PlaceSunlightSource();
            }
        }
        private void PlaceSunlightSource()
        {
            if(sunlightIcon==null||sunlightHaze==null)return;
            var v=LightField.SunTravel[(int)state.SunFrom];var dir=new Vector3(v.x,-v.y,0);bool horizontal=v.x!=0;
            float frameScale=editing?editorZoom:1,extent=(horizontal?state.Level.Width:state.Level.Height)*.5f*frameScale;
            var center=normalFrame+(editing?editorPan:Vector3.zero);var icon=center-dir*(extent+(editing?28:38)*unitsPerUI);
            float halfH=cameraView.orthographicSize,halfW=halfH*cameraView.aspect;
            var min=editing?normalFrame-new Vector3(EditorViewWidth,EditorHeight)*(.5f*unitsPerUI):new Vector3(-halfW,-halfH);
            var max=editing?normalFrame+new Vector3(EditorViewWidth,EditorHeight)*(.5f*unitsPerUI):new Vector3(halfW,halfH);
            if(!editing)
            {
                // Read as a screen-edge daylight marker, well separated from board objects.
                if(horizontal)icon.x=dir.x>0?min.x+48*unitsPerUI:max.x-48*unitsPerUI;
                else icon.y=dir.y>0?min.y+24*unitsPerUI:max.y-48*unitsPerUI;
                if(state.SunFrom==SunSide.Top)icon.x=64*unitsPerUI;
            }
            // When a zoomed/panned board edge is offscreen, hide its source rather than
            // clamping it onto a playable cell and making it look like another lamp.
            float margin=21*unitsPerUI;sunlightIcon.enabled=icon.x>=min.x+margin&&icon.x<=max.x-margin&&icon.y>=min.y+margin&&icon.y<=max.y-margin;
            sunlightIcon.transform.localPosition=boardRoot.InverseTransformPoint(icon);sunlightIcon.transform.localScale=Vector3.one*(40*unitsPerUI/boardRoot.lossyScale.x);
            // The source sits beyond the viewport: only the quiet edge of a broad haze is visible.
            var haze=editing?normalFrame:Vector3.zero;
            if(horizontal)haze.x=dir.x>0?min.x-80*unitsPerUI:max.x+80*unitsPerUI;else haze.y=dir.y>0?min.y-80*unitsPerUI:max.y+80*unitsPerUI;
            sunlightHaze.transform.localPosition=boardRoot.InverseTransformPoint(haze);
            sunlightHaze.transform.localScale=(horizontal?new Vector3(1120,1500,1):new Vector3(1700,1000,1))*(unitsPerUI/boardRoot.lossyScale.x);
        }
        private void HideLightPresentation()
        {if(lightRoot!=null)lightRoot.gameObject.SetActive(false);sunlightIcon=sunlightHaze=null;}
        private static void ReleaseLightObject(Object value)
        {if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()
        {
            ReleaseLightObject(sunTerrainMesh);ReleaseLightObject(sunObjectsMesh);ReleaseLightObject(lampFieldMesh);
            ReleaseLightObject(shadeMaterial);ReleaseLightObject(beamMaterial);ReleaseLightObject(fieldBefore);ReleaseLightObject(fieldAfter);
            ReleaseLightObject(burnMaterial);
            ReleaseShadowField();
        }
    }
}
