using System.Collections.Generic;
using BoxOrVoid.Core;
using UnityEngine;

namespace BoxOrVoid.Study
{
    /// <summary>Projects discrete state to sprites. Interpolation never mutates puzzle state.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        private static readonly Cell[] NeighborDirections = { new Cell(0,1), new Cell(1,0), new Cell(0,-1), new Cell(-1,0) };
        private static readonly string[] MaskNames = { "None", "Up", "Right", "UpRight", "Down", "UpDown", "RightDown", "UpRightDown", "Left", "UpLeft", "RightLeft", "UpRightLeft", "DownLeft", "UpDownLeft", "RightDownLeft", "Uprightdownleft" };
        private static readonly string[] FacingNames = { "Up", "Right", "Down", "Left" };
        private static readonly string[] CornerNames = { "UpRight", "DownRight", "DownLeft", "UpLeft" };
        private readonly List<SpriteRenderer> all = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> tileShadows = new List<SpriteRenderer>();
        private SpriteRenderer[] walls, pieces, under, goals, checks;
        private SpriteRenderer[,] corners;
        private StudyBackdrop backdrop;
        private Material spriteMaterial;
        private StudyAssets assets;
        private Camera viewCamera;
        private PuzzleState state, previous;
        private int chapter;
        private float elapsed, duration;
        private ActionKind lastKind;
        private float completion=-1;
        private float lastAspect;
        public bool IsAnimating => elapsed < duration;
        public bool BlocksMovement=>IsAnimating && (lastKind==ActionKind.Moved || lastKind==ActionKind.Pushed);
        public Camera Camera => viewCamera;

        public void Initialize(StudyAssets source, Camera camera)
        {
            assets = source; viewCamera = camera;
            spriteMaterial = new Material(source.TileMaterial) { name = "BoxOrVoid Unlit" };
            var backdropObject=new GameObject("Backdrop");backdropObject.transform.SetParent(transform,false);
            backdrop=backdropObject.AddComponent<StudyBackdrop>();backdrop.Initialize(assets,viewCamera,spriteMaterial);
        }

        public void Load(PuzzleState initial, int chapterIndex)
        {
            Clear(); state = previous = initial; chapter = chapterIndex; elapsed = duration = 0;completion=-1;
            var d = state.Definition;
            viewCamera.orthographic = true;
            FitToAspect();
            viewCamera.transform.position = new Vector3(0,0,-10);
            backdrop.Show(chapter,state.ActiveTone);
            walls = new SpriteRenderer[d.Width*d.Height]; corners = new SpriteRenderer[walls.Length,4];
            for (int y=0;y<d.Height;y++) for(int x=0;x<d.Width;x++)
            {
                int index=y*d.Width+x; var cell=new Cell(x,y);
                if (d.WallAt(cell)==Tone.None) continue;
                walls[index]=NewSprite("Wall "+cell,0); walls[index].transform.localPosition=Position(cell);
                for(int c=0;c<4;c++) { corners[index,c]=NewSprite("Corner "+cell+" "+c,1); corners[index,c].transform.localPosition=Position(cell); }
                if(d.WallAt(cell)==Tone.Gray)
                {
                    var shadow=NewSprite("Wall shadow "+cell,-10); shadow.transform.localPosition=Position(cell)+new Vector3(.10f,-.13f,0);
                    SetSprite(shadow,assets.ThemeSprite(chapter,"grayWallInactiveSprite")); shadow.color=new Color(0,0,0,.48f);tileShadows.Add(shadow);
                }
            }
            pieces=new SpriteRenderer[state.PieceCount];under=new SpriteRenderer[state.PieceCount];
            for(int i=0;i<pieces.Length;i++) { pieces[i]=NewSprite("Piece "+i,10);under[i]=NewSprite("Covered piece "+i,80); }
            goals=new SpriteRenderer[d.Goals.Count];checks=new SpriteRenderer[goals.Length];
            for(int i=0;i<goals.Length;i++) { goals[i]=NewSprite("Goal "+i,70); checks[i]=NewSprite("Goal complete "+i,90); }
            RefreshStatic(); RefreshPieces(1f);
        }

        public void Present(ActionResult action)
        {
            previous=action.Before; state=action.After; elapsed=0; lastKind=action.Kind;
            duration=(action.Kind==ActionKind.Moved || action.Kind==ActionKind.Pushed || action.Kind==ActionKind.Blocked) ? .10f : .10f;
            RefreshStatic(); RefreshPieces(0);
        }

        private void Update()
        {
            if(state==null) return;
            if(!Mathf.Approximately(lastAspect,viewCamera.aspect)) FitToAspect();
            elapsed+=Time.unscaledDeltaTime;
            RefreshPieces(duration<=0 ? 1 : Mathf.Clamp01(elapsed/duration));
            if(completion>0)
                foreach(var renderer in all)
                {
                    var p=renderer.transform.localPosition;
                    int x=Mathf.RoundToInt(p.x+(state.Definition.Width-1)*.5f),y=Mathf.RoundToInt(p.y+(state.Definition.Height-1)*.5f);
                    int hash=((x*73856093)^(y*19349663))&int.MaxValue;
                    if(hash%997/997f<completion) renderer.enabled=false;
                }
        }
        public void SetCompletion(float fraction) {completion=fraction;}
        public void FitToAspect()
        {
            lastAspect=viewCamera.aspect;
            if(state!=null) viewCamera.orthographicSize=Mathf.Clamp(Mathf.Max(state.Definition.Width/(2f*lastAspect),state.Definition.Height/2f)+1f,3f,15f);
        }
        public void ShowMenu(int chapterIndex,string background="Main Menu Background")
        {
            Clear();state=previous=null;viewCamera.orthographicSize=5;viewCamera.transform.position=new Vector3(0,0,-10);
            backdrop.ShowMenu(chapterIndex,background);completion=-1;
        }

        private void RefreshStatic()
        {
            var d=state.Definition;
            backdrop.SetTone(state.ActiveTone);
            for(int y=0;y<d.Height;y++) for(int x=0;x<d.Width;x++)
            {
                int index=y*d.Width+x; if(walls[index]==null) continue;
                var cell=new Cell(x,y); var tone=d.WallAt(cell); string prefix=Prefix(tone);
                bool active=tone==Tone.Gray || tone==state.ActiveTone;
                int missing=0;
                for(int n=0;n<4;n++) if(!ConnectedWall(cell+NeighborDirections[n],tone)) missing|=1<<n;
                SetSprite(walls[index],assets.ThemeSprite(chapter,prefix+(active?"WallActiveVariantSet."+MaskNames[missing]:"WallInactiveSprite")));
                for(int c=0;c<4;c++)
                {
                    var a=NeighborDirections[c]; var b=NeighborDirections[(c+1)%4];
                    bool visible=active && ConnectedWall(cell+a,tone) && ConnectedWall(cell+b,tone) && !ConnectedWall(cell+a+b,tone);
                    SetSprite(corners[index,c],visible?assets.ThemeSprite(chapter,prefix+"WallCornerOverlays."+CornerNames[c]):null);
                }
            }
        }

        private void RefreshPieces(float t)
        {
            for(int i=0;i<pieces.Length;i++)
            {
                var p=state.GetPiece(i); var old=previous.GetPiece(i);
                bool active=p.Tone==state.ActiveTone;
                string prefix=Prefix(p.Tone);
                string key=p.Kind==ObjectKind.Player ? prefix+"Player"+(active?"ActiveFacing"+FacingNames[(int)p.Facing]+"Sprite":"InactiveSprite") : prefix+"Box"+(active?"ActiveSprite":"InactiveSprite");
                SetSprite(pieces[i],assets.ThemeSprite(chapter,key));
                pieces[i].sortingOrder=10+p.Depth*5;
                bool moved=p.Cell!=old.Cell;
                pieces[i].transform.localPosition=Vector3.Lerp(Position(old.Cell),Position(p.Cell),t);
                if(moved && t<1) pieces[i].sortingOrder=60;
                if(p.Kind==ObjectKind.Player && active && (moved || lastKind==ActionKind.Blocked) && t<1)
                {
                    float amount=Mathf.Sin(t*Mathf.PI)*(lastKind==ActionKind.Blocked?.06f:.2f);
                    bool horizontal=p.Facing==Command.Left || p.Facing==Command.Right;
                    pieces[i].transform.localScale=new Vector3(1+(horizontal?amount:-amount),1+(horizontal?-amount:amount),1);
                }
                bool covered=state.UnderTopAt(p.Cell)==i;
                SetSprite(under[i],covered?assets.ThemeSprite(chapter,prefix+(p.Kind==ObjectKind.Player?"Player":"Box")+"UnderSprite"):null);
                under[i].transform.localPosition=Position(p.Cell);
                SetMarkerAmount(under[i],MarkerAmount(p.Cell));
                if(covered && p.Kind==ObjectKind.Player && active && lastKind==ActionKind.Covered && t<1)
                    under[i].transform.localPosition+=new Vector3(Mathf.Sin(t*Mathf.PI*4)*.04f,0,0);
            }
            for(int i=0;i<goals.Length;i++)
            {
                var goal=state.Definition.Goals[i];bool occupied=state.TopAt(goal.Cell)>=0,fulfilled=state.GoalFulfilled(i);
                string prefix=Prefix(goal.Tone)+(goal.RequiredKind==ObjectKind.Player?"Player":"Box");
                bool goalVisible=!fulfilled && state.UnderTopAt(goal.Cell)<0;
                SetSprite(goals[i],goalVisible?assets.ThemeSprite(chapter,prefix+(occupied?"GoalUnderSprite":"GoalSprite")):null);
                goals[i].transform.localPosition=Position(goal.Cell);
                SetSprite(checks[i],fulfilled?assets.ThemeSprite(chapter,"goalFulfillmentSprite"):null);
                checks[i].transform.localPosition=Position(goal.Cell);
                SetMarkerAmount(goals[i],occupied?MarkerAmount(goal.Cell):1);
                SetMarkerAmount(checks[i],MarkerAmount(goal.Cell));
            }
        }

        private float MarkerAmount(Cell cell)
        {
            for(int i=0;i<state.PieceCount;i++)
            {
                var current=state.GetPiece(i);var prior=previous.GetPiece(i);
                if((current.Cell==cell || prior.Cell==cell) && (current.Cell!=prior.Cell || current.Depth!=prior.Depth))
                    return Mathf.Clamp01((elapsed-duration)/.3f);
            }
            return 1;
        }
        private static void SetMarkerAmount(SpriteRenderer renderer,float amount)
        {
            renderer.color=new Color(1,1,1,amount);renderer.transform.localScale=Vector3.one*Mathf.Lerp(.65f,1,amount);
            renderer.enabled=renderer.sprite!=null && amount>0;
        }

        private bool ConnectedWall(Cell cell,Tone tone) => state.Definition.WallAt(cell)==tone && state.TopAt(cell)<0;
        private static string Prefix(Tone tone) => tone==Tone.White?"white":tone==Tone.Black?"black":"gray";
        private Vector3 Position(Cell cell) => new Vector3(cell.X-(state.Definition.Width-1)*.5f,cell.Y-(state.Definition.Height-1)*.5f,0);
        private SpriteRenderer NewSprite(string label,int order)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sharedMaterial=spriteMaterial;renderer.sortingOrder=order;all.Add(renderer);return renderer;
        }
        private static void SetSprite(SpriteRenderer renderer,Sprite sprite,float size=1)
        {
            renderer.sprite=sprite;renderer.enabled=sprite!=null;
            if(sprite!=null) renderer.transform.localScale=Vector3.one*size;
        }
        private void Clear()
        {
            foreach(var renderer in all) if(renderer!=null) {renderer.gameObject.SetActive(false);Destroy(renderer.gameObject);}
            all.Clear();tileShadows.Clear();
        }
        private void OnDestroy() { if(spriteMaterial!=null) Destroy(spriteMaterial); }
    }
}
