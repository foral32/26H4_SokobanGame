using System;
using System.Collections.Generic;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView:MonoBehaviour
    {
        private GameAssets assets;private Camera cameraView;private Transform boardRoot;
        private MaterialPropertyBlock spriteProperties;private static readonly int MainTexture=Shader.PropertyToID("_MainTex");
        private readonly List<SpriteRenderer> renderers=new List<SpriteRenderer>();private readonly Stack<SpriteRenderer> spare=new Stack<SpriteRenderer>();
        private SpriteMask editMask;private Vector3 editorPan;private float editorZoom=1;
        private SpriteRenderer[] illumination,dust,embers,lampGlows,mirrorRays;private SpriteRenderer ghost;private float deathAt=-1;private float[] shineFrom,shineTo;
        public const float DeathImpactSeconds=.115f,DeathSeconds=.60f;public float EditorZoom=>editorZoom;
        public const float EditorWidth=1080,EditorHeight=512,EditorX=54,EditorY=-28;
        private bool paletteCollapsed;private float EditorViewWidth=>paletteCollapsed?1188:EditorWidth;private float EditorViewX=>paletteCollapsed?0:EditorX;
        public void EditorPaletteCollapsed(bool value){paletteCollapsed=value;if(state!=null&&editing)Fit();}
        private SpriteRenderer[] floors,pieces,pieceShadows;private SpriteRenderer player,cape,fangs,coffin,lid,warning;
        private Color[] lightFrom,lightTo;
        private State state,previous;private ActionResult action;private float elapsed,duration,wonAt=-1,aspect;private bool editing;
        private Vector3 normalFrame;private float unitsPerUI;
        public int HighestSortingOrder=>37;
        public bool IsAnimating=>state!=null&&(elapsed<duration||!editing&&state.Dead&&Time.unscaledTime-deathAt<DeathSeconds);
        public Camera Camera=>cameraView;
        public State State=>state;
        public static Color SafeColor=>GameThemes.Current.FloorSafe;public static Color LitColor=>GameThemes.Current.FloorLit;
        public void RefreshTheme()
        {
            if(state==null)return;var current=state;bool edit=editing;float won=wonAt,death=deathAt;
            Load(current,edit);wonAt=won;deathAt=death;Refresh(1);
        }
        public void Initialize(GameAssets a,Camera c){assets=a;cameraView=c;spriteProperties=new MaterialPropertyBlock();boardRoot=new GameObject("Board").transform;boardRoot.SetParent(transform,false);var g=new GameObject("Editor viewport",typeof(SpriteMask));g.transform.SetParent(transform,false);editMask=g.GetComponent<SpriteMask>();editMask.sprite=a.Sprite("solid");editMask.isCustomRangeActive=true;editMask.backSortingOrder=-100;editMask.frontSortingOrder=90;editMask.enabled=false;}
        public void Clear()
        {Recycle(true);}
        private void Recycle(bool hide)
        {ResetDeathBackdrop();for(int i=renderers.Count-1;i>=0;i--){var r=renderers[i];if(r==null)continue;if(hide)r.gameObject.SetActive(false);spare.Push(r);}renderers.Clear();state=previous=null;if(editMask!=null)editMask.enabled=false;}
        public void Load(State s,bool edit=false)
        {
            Recycle(false);state=previous=s;action=null;editing=edit;elapsed=duration=0;wonAt=deathAt=-1;Fit();var d=s.Level;
            floors=new SpriteRenderer[d.Width*d.Height];
            lightFrom=new Color[floors.Length];lightTo=new Color[floors.Length];illumination=new SpriteRenderer[floors.Length];shineFrom=new float[floors.Length];shineTo=new float[floors.Length];
            for(int y=0;y<d.Height;y++)for(int x=0;x<d.Width;x++)
            {
                var p=new Cell(x,y);var pos=Position(p);char terrain=d.Terrain(p);
                if(terrain=='.')
                {
                    int cell=y*d.Width+x;floors[cell]=MakeSprite("Floor "+p,SurfaceSprites.Floor,-20,pos,1);illumination[cell]=MakeSprite("Illumination "+p,SurfaceSprites.Light(0),-19,pos,.99f);
                }
                if(terrain=='#')
                {
                    BuildStoneWall(p,pos);
                }
                if(terrain==' '&&edit){var v=Make("Void "+p,"solid",-25,pos,.96f);v.color=GameThemes.IsClassic?new Color32(30,36,43,255):GameThemes.Current.Background;}
            }
            if(s.SunEnabled)BuildSunIndicator();
            BuildDevices();
            coffin=Make("Coffin entrance","coffin-open",3,Position(d.Coffin),.9f);
            coffinExposure=MakeSprite("Coffin illumination",SurfaceSprites.CoffinLight,4,Position(d.Coffin),.9f);
            lid=Make("Coffin lid","coffin-lid",30,Position(d.Coffin),.9f);lid.enabled=false;
            pieces=new SpriteRenderer[d.PieceCount];lampGlows=new SpriteRenderer[d.PieceCount];mirrorRays=new SpriteRenderer[d.PieceCount];for(int i=0;i<pieces.Length;i++){pieces[i]=Make("Piece "+i,d.Kind(i)==PieceKind.Box?"Box_active_white":d.Kind(i)==PieceKind.Lamp?"lamp":d.Kind(i)==PieceKind.MirrorSlash?"mirror-slash":"mirror-backslash",10,Position(s.Position(i)),d.Kind(i)==PieceKind.Box?.9f:.84f);if(d.Kind(i)==PieceKind.Lamp){lampGlows[i]=MakeSprite("Lamp glow "+i,SurfaceSprites.Glow,9,Position(s.Position(i)),1.05f);lampGlows[i].color=new Color(1,.84f,.45f,.40f);}else if(d.Kind(i)==PieceKind.MirrorSlash||d.Kind(i)==PieceKind.MirrorBackslash){mirrorRays[i]=MakeSprite("Mirror bend "+i,SurfaceSprites.MirrorRay(0),11,Position(s.Position(i)),.99f);}}
            pieceShadows=new SpriteRenderer[pieces.Length];for(int i=0;i<pieces.Length;i++)if(d.Kind(i)==PieceKind.Box){SetSprite(pieces[i],SurfaceSprites.RaisedBoxForSun(state.SunEnabled?(int)state.SunFrom:-1));pieceShadows[i]=Make("Box contact shadow "+i,"solid",8,Position(state.Position(i))+ContactOffset(),.87f);pieceShadows[i].color=new Color(0,0,0,.22f);}
            cape=MakeSprite("Cape",SurfaceSprites.VampireCape,11,Position(s.Player),.94f);
            player=Make("Vampire","Player_active_white_right",12,Position(s.Player),.72f);
            fangs=MakeSprite("Fangs",SurfaceSprites.VampireFangs,13,Position(s.Player),.72f);
            bool hasPlayer=d.Floor(s.Player);player.enabled=cape.enabled=fangs.enabled=hasPlayer;
            coffin.enabled=d.Floor(d.Coffin);
            coffinExposure.enabled=coffin.enabled;
            warning=Make("Rejected light cell","outline",7,Vector3.zero,.98f);warning.enabled=false;
            ghost=Make("Brush preview","solid",35,Vector3.zero,.88f);ghost.enabled=false;
            BuildRegionPreview();
            dust=new SpriteRenderer[6];embers=new SpriteRenderer[2];for(int i=0;i<dust.Length;i++){dust[i]=Make("Push dust "+i,"solid",8,Vector3.zero,.05f);dust[i].enabled=false;}for(int i=0;i<embers.Length;i++){embers[i]=Make("Burn ash "+i,"solid",20,Vector3.zero,.055f);embers[i].enabled=false;}
            UpdateFloor();Refresh(1);
            foreach(var unused in spare)if(unused.gameObject.activeSelf)unused.gameObject.SetActive(false);
        }
        public void Preview(LevelData data){Load(Definition.Preview(data).Initial(),true);}
        private void Fit()
        {
            aspect=cameraView.aspect;var d=state.Level;
            // Reserve the same logical space as the expanding 1280x720 UI canvas.
            // Fitting against the whole window would overlap the controls on taller windows.
            float pixelHeight=cameraView.pixelHeight;
            float uiScale=Mathf.Min(pixelHeight*aspect/1280f,pixelHeight/720f);
            float cellPixels=Mathf.Min((editing?EditorViewWidth:960f)*uiScale/d.Width,(editing?EditorHeight:470f)*uiScale/d.Height);
            cameraView.orthographicSize=pixelHeight/(2f*cellPixels);
            cameraView.transform.position=new Vector3(0,0,-10);
            unitsPerUI=uiScale/cellPixels;normalFrame=new Vector3((editing?EditorViewX:-60f)*unitsPerUI,editing?EditorY*unitsPerUI:0,0);editMask.enabled=editing;editMask.transform.localPosition=normalFrame;editMask.transform.localScale=new Vector3(EditorViewWidth,EditorHeight,1)*unitsPerUI;ClampPan();ApplyFrame();
        }
        private void ApplyFrame()
        {DeathFrame(out var offset,out float scale);boardRoot.localPosition=normalFrame+(editing?editorPan:offset);boardRoot.localScale=Vector3.one*(editing?editorZoom:scale);}
        public void ResetEditorView(){editorZoom=1;editorPan=Vector3.zero;if(state!=null)Fit();}
        private void ClampPan()
        {
            if(state==null)return;
            // Let a fitted board move too, retaining at least one cell inside the canvas.
            float x=(state.Level.Width*editorZoom+EditorViewWidth*unitsPerUI)*.5f-editorZoom;
            float y=(state.Level.Height*editorZoom+EditorHeight*unitsPerUI)*.5f-editorZoom;
            editorPan=new Vector3(Mathf.Clamp(editorPan.x,-x,x),Mathf.Clamp(editorPan.y,-y,y));
        }
        public bool InViewport(Vector2 screen){if(!editing)return true;var w=cameraView.ScreenToWorldPoint(screen);return Mathf.Abs(w.x-normalFrame.x)<=EditorViewWidth*.5f*unitsPerUI&&Mathf.Abs(w.y-normalFrame.y)<=EditorHeight*.5f*unitsPerUI;}
        public void ZoomAt(Vector2 screen,float multiplier){if(!editing||!InViewport(screen))return;var w=cameraView.ScreenToWorldPoint(screen);w.z=0;var p=boardRoot.InverseTransformPoint(w);editorZoom=Mathf.Clamp(editorZoom*multiplier,.65f,4);editorPan=w-normalFrame-p*editorZoom;ClampPan();ApplyFrame();}
        public void Pan(Vector2 pixels){if(!editing)return;editorPan+=(Vector3)(pixels*cameraView.orthographicSize*2/cameraView.pixelHeight);ClampPan();ApplyFrame();}
        public void BrushPreview(Cell? cell,Brush brush,bool valid){if(ghost==null)return;ghost.enabled=editing&&cell.HasValue;if(!ghost.enabled)return;string icon=brush==Brush.Box?"Box_active_white":brush==Brush.Player?"vampire":brush==Brush.Coffin?"coffin-open":brush==Brush.Lamp?"lamp":brush==Brush.MirrorSlash?"mirror-slash":brush==Brush.MirrorBackslash?"mirror-backslash":"solid";SetSprite(ghost,assets.Sprite(icon));ghost.transform.localPosition=Position(cell.Value);ghost.color=valid?new Color(.85f,.91f,1,.48f):new Color(1,.35f,.28f,.5f);}
        public void HighlightInvalid(Cell? cell){if(warning==null)return;warning.enabled=editing&&cell.HasValue;if(cell.HasValue){warning.transform.localPosition=Position(cell.Value);warning.color=new Color(1,.35f,.3f,.8f);}}
        public void FitForCapture(){if(state!=null)Fit();}
        public Vector3 Position(Cell p)=>new Vector3(p.x-(state.Level.Width-1)*.5f,(state.Level.Height-1)*.5f-p.y,0);
        public bool CellAt(Vector2 screen,out Cell cell)
        {
            cell=new Cell(-1,-1);if(state==null||!InViewport(screen))return false;
            var world=boardRoot.InverseTransformPoint(cameraView.ScreenToWorldPoint(screen));
            cell=new Cell(Mathf.FloorToInt(world.x+(state.Level.Width-1)*.5f+.5f),Mathf.FloorToInt((state.Level.Height-1)*.5f-world.y+.5f));
            return state.Level.Inside(cell);
        }
        public Vector2 Screen(Cell p)=>cameraView.WorldToScreenPoint(boardRoot.TransformPoint(Position(p)));
        private SpriteRenderer Make(string name,string sprite,int order,Vector3 pos,float size)
        {var source=assets.Sprite(sprite);if(source==null)throw new InvalidOperationException("Missing board sprite: "+sprite);return MakeSprite(name,source,order,pos,size);}
        private SpriteRenderer MakeSprite(string name,Sprite source,int order,Vector3 pos,float size)
        {
            SpriteRenderer r;if(spare.Count>0)r=spare.Pop();else{var go=new GameObject(name);go.transform.SetParent(boardRoot,false);r=go.AddComponent<SpriteRenderer>();}
            if(r.gameObject.name!=name)r.gameObject.name=name;if(!r.gameObject.activeSelf)r.gameObject.SetActive(true);if(!r.enabled)r.enabled=true;if(r.drawMode!=SpriteDrawMode.Simple)r.drawMode=SpriteDrawMode.Simple;if(order>-18)r.color=Color.white;if(r.sharedMaterial!=assets.material)r.sharedMaterial=assets.material;SetSprite(r,source);if(r.sortingOrder!=order)r.sortingOrder=order;var mask=editing?SpriteMaskInteraction.VisibleInsideMask:SpriteMaskInteraction.None;if(r.maskInteraction!=mask)r.maskInteraction=mask;var t=r.transform;if(t.localPosition!=pos)t.localPosition=pos;var scale=Vector3.one*size;if(t.localScale!=scale)t.localScale=scale;if(t.localRotation!=Quaternion.identity)t.localRotation=Quaternion.identity;renderers.Add(r);return r;
        }
        private void SetSprite(SpriteRenderer renderer,Sprite sprite,bool force=false)
        {
            if(!force&&renderer.sprite==sprite)return;renderer.sprite=sprite;
            // Explicit binding prevents stale texture sampling when procedural surfaces and
            // imported art share the 2D material across viewport rebuilds and pooled renderers.
            spriteProperties.Clear();spriteProperties.SetTexture(MainTexture,sprite.texture);renderer.SetPropertyBlock(spriteProperties);
        }
        public void Present(ActionResult result)
        {
            previous=result.Before;state=result.After;action=result;elapsed=0;duration=result.Kind==ActionKind.Blocked?.08f:result.Kind==ActionKind.Undone?.085f:.115f;deathAt=state.Dead?Time.unscaledTime:-1;
            UpdateFloor();warning.enabled=false;
            if(warning.enabled){warning.transform.localPosition=Position(result.RejectedCell);warning.color=new Color(1,.40f,.30f,.9f);}
            if(state.Won)wonAt=Time.unscaledTime;
            else{wonAt=-1;lid.enabled=false;player.enabled=cape.enabled=fangs.enabled=true;}
            Refresh(0);
        }
        private void UpdateFloor()
        {
            var light=LightField.Compute(state);var d=state.Level;
            coffinFrom=coffinExposure.color.a;coffinTo=light.Lit(d.Coffin)?.43f:0;
            for(int j=0;j<mirrorRays.Length;j++)if(mirrorRays[j]!=null){var p=state.Position(j);SetSprite(mirrorRays[j],SurfaceSprites.MirrorRay(light.ReflectionEdges[p.y*d.Width+p.x]));mirrorRays[j].color=new Color(1,.86f,.52f,.85f);}
            for(int y=0;y<d.Height;y++)for(int x=0;x<d.Width;x++)
            {
                int i=y*d.Width+x;if(floors[i]==null)continue;var p=new Cell(x,y);
                lightFrom[i]=floors[i].color;lightTo[i]=light.Lit(p)?LitColor:SafeColor;shineFrom[i]=illumination[i].color.a;shineTo[i]=light.Lit(p)?.045f:0;int edges=0;if(!light.Lit(new Cell(x-1,y)))edges|=1;if(!light.Lit(new Cell(x+1,y)))edges|=2;if(!light.Lit(new Cell(x,y-1)))edges|=4;if(!light.Lit(new Cell(x,y+1)))edges|=8;SetSprite(illumination[i],SurfaceSprites.Light(edges));
            }
        }
        private void BlendLighting(float t)
        {
            BlendCoffinLight(t);for(int i=0;i<floors.Length;i++)if(floors[i]!=null)
            {
                floors[i].color=Color.Lerp(lightFrom[i],lightTo[i],t);
                illumination[i].color=new Color(1,.94f,.80f,Mathf.Lerp(shineFrom[i],shineTo[i],t));
            }
        }
        private void Update()
        {
            if(state==null)return;if(!Mathf.Approximately(aspect,cameraView.aspect))Fit();
            ApplyFrame();
            elapsed+=Time.unscaledDeltaTime;Refresh(duration<=0?1:Mathf.Clamp01(elapsed/duration));
            
        }
        private void Refresh(float t)
        {
            if(state==null||player==null)return;
            float travel=1-Mathf.Pow(1-t,3);BlendLighting(travel);BlendDevices(travel);
            for(int i=0;i<pieces.Length;i++){pieces[i].transform.localPosition=Vector3.Lerp(Position(previous.Position(i)),Position(state.Position(i)),travel);if(mirrorRays[i]!=null)mirrorRays[i].transform.localPosition=pieces[i].transform.localPosition;if(lampGlows[i]!=null){lampGlows[i].transform.localPosition=pieces[i].transform.localPosition;lampGlows[i].color=new Color(1,.84f,.45f,.40f);pieces[i].color=Color.Lerp(new Color(.93f,.93f,.93f),Color.white,.5f+.5f*Mathf.Sin(Time.unscaledTime*2));}}
            for(int i=0;i<pieces.Length;i++)if(pieceShadows[i]!=null)pieceShadows[i].transform.localPosition=pieces[i].transform.localPosition+ContactOffset();
            var face=action!=null&&action.Kind==ActionKind.Blocked&&action.Command<=Command.Left?action.Command:state.Facing;
            string[] names={"up","right","down","left"};SetSprite(player,assets.Sprite("Player_active_white_"+names[(int)face]));
            var pos=Vector3.Lerp(Position(previous.Player),Position(state.Player),travel);
            float wave=(action==null?0:Mathf.Sin(t*Mathf.PI));
            if(action!=null&&action.Kind==ActionKind.Blocked){var dir=LightField.Directions[(int)face];pos+=new Vector3(dir.x,-dir.y)*wave*.06f;}
            float deform=wave*(action!=null&&action.Kind==ActionKind.Blocked?.055f:.16f);bool horiz=face==Command.Left||face==Command.Right;
            player.enabled=cape.enabled=state.Level.Floor(state.Player);player.color=cape.color=fangs.color=Color.white;cape.transform.localScale=Vector3.one*.94f;player.transform.localPosition=pos+Vector3.up*.015f;player.transform.localScale=new Vector3(.72f*(1+(horiz?deform:-deform)),.72f*(1+(horiz?-deform:deform)),1);
            cape.transform.localPosition=Vector3.Lerp(Position(previous.Player),pos,Mathf.Clamp01(t*1.2f)) + new Vector3(-wave*.035f,-.025f,0);
            fangs.transform.localPosition=player.transform.localPosition+Vector3.down*.035f;fangs.transform.localScale=player.transform.localScale;
            fangs.enabled=player.enabled&&face!=Command.Up;
            PushDust();foreach(var bit in embers)bit.enabled=false;
            if(!editing&&state.Dead)DeathPose();
            if(wonAt>=0)
            {
                float q=Time.unscaledTime-wonAt;float shrink=1-Mathf.Clamp01((q-.13f)/.18f);
                player.transform.localScale*=shrink;cape.transform.localScale=Vector3.one*(.94f*shrink);fangs.transform.localScale*=shrink;
                if(q>.31f){player.enabled=cape.enabled=fangs.enabled=false;lid.enabled=true;lid.transform.localPosition=Position(state.Level.Coffin)+Vector3.right*.65f*(1-Mathf.SmoothStep(0,1,(q-.29f)/.18f));}
            }
        }
        private void PushDust()
        {
            bool pushed=action!=null&&action.PushedCount>0&&elapsed<.27f;int moved=-1;if(pushed)for(int i=0;i<pieces.Length;i++)if(previous.Position(i)!=state.Position(i)){moved=i;break;}
            for(int i=0;i<dust.Length;i++){dust[i].enabled=pushed&&moved>=0;if(!dust[i].enabled)continue;float t=Mathf.Clamp01(elapsed/.27f),a=i*2.39996f;dust[i].transform.localPosition=Vector3.Lerp(Position(previous.Position(moved)),Position(state.Position(moved)),.55f)+new Vector3(Mathf.Cos(a)*(.28f+t*.2f),Mathf.Sin(a)*(.27f+t*.1f),0);dust[i].color=new Color(.72f,.77f,.78f,(1-t)*.30f);dust[i].transform.localScale=Vector3.one*(.045f*(1-t*.5f));}
        }
    }
}
