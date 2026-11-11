using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private static readonly BindingFlags LightPrivate=BindingFlags.Instance|BindingFlags.NonPublic;
        private object LightMember(string name)=>typeof(BoardView).GetField(name,LightPrivate).GetValue(app.Board);
        public void BeginLightPresentation(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report.runtime="Unity Editor production light presentation";report.width=Screen.width;report.height=Screen.height;
            folder=Path.GetFullPath("DraculasBox/Reviews/SunIntegration-20261111/Play");Directory.CreateDirectory(folder);
            Application.logMessageReceived+=Log;StartCoroutine(Guard(LightPresentationChecks()));
        }
        private static LevelData LightRoom()=>new LevelData
        {
            id="light-presentation-check",title="投影",rows=new[]{".........",".........",".#.......",".........",".........",".........","........."},
            sunEnabled=true,sunFrom=SunSide.Left,player=new Cell(4,3),coffin=new Cell(8,0),
            pieces=new[]{new EntityData{id="shade",cell=new Cell(3,3)},new EntityData{id="source",kind=PieceKind.Lamp,cell=new Cell(6,6)}}
        };
        private IEnumerator LightPresentationChecks()
        {
            Require(app.Automated&&Path.GetFullPath(app.Library.Folder).StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase),"lighting checks use isolated input and profile");report.progressIsolated=true;yield return Ready();
            yield return LightTheme("wine");
            var room=LightRoom();app.StartEditorPlay(room);yield return Ready();
            var samples=LightPixels(new[]{new Cell(2,2),new Cell(3,2),new Cell(4,2)});
            Require(samples[1].grayscale>samples[0].grayscale+.06f&&samples[2].grayscale>samples[1].grayscale+.06f,"actual GPU output grades from wall contact through two shade cells into sunlight");
            Require(samples[0].grayscale<.52f&&samples[2].grayscale>.6f,"safe shade remains visibly distinct from lethal sun");
            Capture("sunlight-production");
            int uploads=(int)LightMember("lightMeshUploads");var material=(Material)LightMember("shadeMaterial");int materialId=material.GetInstanceID();
            for(int i=0;i<24;i++)yield return null;
            Require((int)LightMember("lightMeshUploads")==uploads,"idle rendering does not rebuild shadow meshes or rasterize CPU masks");
            string spawn=app.Session.State.Key();app.Act(Command.Left);yield return Ready();
            Require(!app.Session.State.Dead&&app.Session.State.Position(0)==new Cell(2,3),"box transport retains safe ordinary push");
            Require((int)LightMember("lightMeshUploads")>uploads&&((Material)LightMember("shadeMaterial")).GetInstanceID()==materialId,"moving shadow updates geometry while retaining its material");
            Capture("sunlight-pushed");yield return Tap(KeyCode.Z);
            Require(app.Session.State.Key()==spawn&&LightingMatches(),"undo restores exact puzzle and rendered light field");
            app.Act(Command.Left);yield return Ready();app.Act(Command.Reset);yield return Ready();
            Require(app.Session.State.Key()==spawn&&LightingMatches(),"reset restores lighting with native bat presentation");
            foreach(SunSide side in Enum.GetValues(typeof(SunSide)))
            {
                room=LightRoom();room.sunFrom=side;room.player=room.pieces[0].cell+LightField.SunTravel[(int)side];
                app.StartEditorPlay(room);yield return Ready();var sun=(SpriteRenderer)LightMember("sunlightIcon");
                var root=app.Board.transform.Find("Board");var v=LightField.SunTravel[(int)side];var direction=new Vector3(v.x,-v.y,0);
                float extent=(v.x!=0?room.rows[0].Length:room.rows.Length)*.5f;
                Require(Vector3.Dot(sun.transform.position-root.position,-direction)>extent,"sun icon is outside the incoming board edge "+side);
                Require(!app.Board.GetComponentsInChildren<SpriteRenderer>().Any(s=>s.name=="Sun direction"&&s.enabled),"sun direction uses no floating arrow "+side);
                var haze=(SpriteRenderer)LightMember("sunlightHaze");Require(haze.color.a<=.31f&&haze.color.r-haze.color.g<.20f,"incoming haze is restrained and desaturated "+side);
                Require(LightingMatches(),"direction change retains authoritative field "+side);Capture("direction-"+side);
            }
            room=LightRoom();room.sunEnabled=false;app.StartEditorPlay(room);yield return Ready();
            Require(LightMember("sunlightIcon")==null&&LightMember("sunlightHaze")==null,"night removes the sun marker and atmospheric haze");
            samples=LightPixels(new[]{new Cell(6,4),new Cell(5,4)});
            Require(samples[0].grayscale>samples[1].grayscale+.2f,"night lamp shaft lights only its cardinal cells");Capture("night-production");
            room=LightRoom();room.sunEnabled=false;room.player=new Cell(2,4);
            room.pieces=new[]{new EntityData{id="weight",cell=new Cell(2,3)},new EntityData{id="source",kind=PieceKind.Lamp,cell=new Cell(6,6)},new EntityData{id="mirror",kind=PieceKind.MirrorSlash,cell=new Cell(6,1)},new EntityData{id="source-2",kind=PieceKind.Lamp,cell=new Cell(8,4)}};
            room.plates=new[]{new DeviceData{cell=new Cell(2,2)}};room.doors=new[]{new DeviceData{cell=new Cell(6,4)}};
            app.StartEditorPlay(room);yield return Ready();
            samples=LightPixels(new[]{new Cell(6,3),new Cell(8,2),new Cell(7,1)});
            Require(samples[0].grayscale<.45f&&samples[1].grayscale>.65f&&samples[2].grayscale<.45f,"closed door blocks one lamp while the second source stays active");Capture("multi-lamp-door-closed");
            yield return Tap(KeyCode.UpArrow);Require(app.Session.State.DoorOpen(0),"plate opens the ordinary linked door");
            samples=LightPixels(new[]{new Cell(6,3),new Cell(7,1)});
            Require(samples[0].grayscale>.65f&&samples[1].grayscale>.65f&&LightingMatches(),"opening a door propagates lamp light through the mirror");Capture("multi-lamp-door-open");
            yield return Tap(KeyCode.Z);Require(!app.Session.State.DoorOpen(0)&&LightingMatches(),"undo restores closed-door light occlusion");
            foreach(string id in ThemeIds)
            {
                yield return LightTheme(id);
                app.StartEditorPlay(LightRoom());yield return Ready();Require(LightingMatches(),"production lighting supports palette "+id);Capture("theme-"+id);
                app.Edit(app.assets.Level("c2_04"));yield return Ready();Capture("editor-"+id);
                var camera=app.Board.Camera;var point=app.Board.Screen(app.Creator.Document.Data.player);
                app.Board.ZoomAt(point,1.6f);app.Board.Pan(new Vector2(110,45));yield return null;Capture("editor-zoom-"+id,1024,768);
                Require(app.Board.CellAt(app.Board.Screen(app.Creator.Document.Data.player),out var picked)&&picked==app.Creator.Document.Data.player,"sun presentation preserves editor picking under pan and zoom "+id);
                Require(((Material)LightMember("shadeMaterial")).GetFloat("_ClipEnabled")>.5f,"editor meshes enable viewport clipping "+id);
                app.Board.ResetEditorView();yield return null;
            }
            foreach(SunSide side in Enum.GetValues(typeof(SunSide)))
            {
                var editorRoom=LightRoom();editorRoom.sunFrom=side;app.Edit(editorRoom);yield return Ready();
                var source=(SpriteRenderer)LightMember("sunlightIcon");var root=app.Board.transform.Find("Board");var direction=LightField.SunTravel[(int)side];
                float extent=(direction.x!=0?9:7)*.5f;
                Require(source.enabled&&Vector3.Dot(source.transform.position-root.position,new Vector3(-direction.x,direction.y,0))>extent,"editor fit keeps the sun visible outside its incoming edge "+side);
            }
            var zoomRoom=LightRoom();zoomRoom.sunFrom=SunSide.Top;app.Edit(zoomRoom);yield return Ready();
            var editorSun=(SpriteRenderer)LightMember("sunlightIcon");Require(editorSun.enabled,"default editor fit reserves space for an incoming top sun");
            app.Board.ZoomAt(app.Board.Screen(new Cell(4,3)),4);yield return null;
            var worldRoot=app.Board.transform.Find("Board");Require(!editorSun.enabled||editorSun.transform.position.y>worldRoot.position.y+zoomRoom.rows.Length*.5f*app.Board.EditorZoom,"zoom never clamps the sun source onto a playable cell");
            Capture("editor-source-zoom");app.Board.ResetEditorView();yield return null;Require(editorSun.enabled,"fit restores an offscreen sun marker");
            yield return LightTheme("wine");
            yield return WorkshopOnly();
        }
        private IEnumerator LightTheme(string id)
        {
            yield return Ready();app.ShowTitle();yield return Ready();app.Settings();yield return Ready();
            Require(app.Page==GamePage.Settings,"theme test reaches actual settings "+id);app.SelectTheme(id);
            Require(GameThemes.Current.Id==id,"theme is applied before capturing "+id);app.CloseSettings();yield return Ready();
        }
        private Color[] LightPixels(Cell[] cells)
        {
            var camera=app.viewCamera;var before=camera.targetTexture;float aspect=camera.aspect;var active=RenderTexture.active;
            var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var result=new Color[cells.Length];
            try
            {
                camera.targetTexture=target;camera.aspect=1280f/720;app.Board.FitForCapture();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                for(int i=0;i<cells.Length;i++){var point=app.Board.Screen(cells[i]);result[i]=image.GetPixel(Mathf.RoundToInt(point.x),Mathf.RoundToInt(point.y));}
            }
            finally{camera.targetTexture=before;camera.aspect=aspect;RenderTexture.active=active;app.Board.FitForCapture();target.Release();Destroy(target);Destroy(image);}
            return result;
        }
    }
}
