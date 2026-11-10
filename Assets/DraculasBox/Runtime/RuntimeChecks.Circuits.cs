using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private IEnumerator CircuitChecks(string id,string spawn)
        {
            var d=app.Session.State.Level;
            if(d.DoorCount==0)yield break;
            Require(app.Board.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name.StartsWith("Door left "))==d.DoorCount,"doors have visible sliding panels "+id);
            Require(app.Board.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name.StartsWith("Plate rim "))==d.PlateCount,"plates have matching visible rims "+id);
            string wrong=id=="c3_02"?"RRR":id=="c3_03"?"RUR":id=="c3_04"?"DRRRDRRDD":null;
            if(wrong==null)yield break;
            if(id=="c3_04"){yield return Route("DRRRD");CheckLitDoor();Capture("c3_04_lamp_weight");yield return Route("RRDD");}else yield return Route(wrong);
            Require(app.Session.State.Dead&&app.Page==GamePage.Dead,"unsafe circuit ordering is lethal "+id);Capture(id+"_wrong_order");
            yield return Undo(wrong.Length);Require(app.Session.State.Key()==spawn&&!app.Session.State.Dead,"undo restores all circuit dependencies "+id);
        }
        private void CheckLitDoor()
        {
            var camera=app.viewCamera;var before=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var a=app.Board.Screen(new Cell(6,5));pixel.ReadPixels(new Rect(Mathf.RoundToInt(a.x),Mathf.RoundToInt(a.y),1,1),0,0);pixel.Apply();Color door=pixel.GetPixel(0,0);
                var b=app.Board.Screen(new Cell(7,5));pixel.ReadPixels(new Rect(Mathf.RoundToInt(b.x),Mathf.RoundToInt(b.y),1,1),0,0);pixel.Apply();Color floor=pixel.GetPixel(0,0);
                Require(Vector4.Distance(door,floor)<.10f&&door.maxColorComponent>.3f,"open door visibly transmits the same dangerous light as adjacent floor");
            }
            finally{camera.targetTexture=before;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(pixel);}
        }
        private IEnumerator CircuitEditorChecks()
        {
            var legacy=new ProgressStore(Path.Combine(Application.temporaryCachePath,"DraculaLegacy-"+Guid.NewGuid().ToString("N")));
            for(int i=1;i<=7;i++)legacy.Complete("c1_"+i.ToString("00"));for(int i=1;i<=6;i++)legacy.Complete("c2_"+i.ToString("00"));legacy.Reconcile(app.assets.Catalog);
            Require(legacy.Data.lastLevel=="c2_08"&&legacy.Done("c2_03")&&!legacy.ChapterUnlocked(app.assets.Catalog.chapters[2],app.assets.Catalog),"retired node resumes at new main content and retains old receipts");legacy.Complete("c2_07");Require(!legacy.ChapterUnlocked(app.assets.Catalog.chapters[2],app.assets.Catalog),"a newly completed old finale does not bypass new main dependencies");
            var oldComplete=new ProgressStore(Path.Combine(Application.temporaryCachePath,"OldComplete-"+Guid.NewGuid().ToString("N")));for(int i=1;i<=7;i++){oldComplete.Complete("c1_"+i.ToString("00"));oldComplete.Complete("c2_"+i.ToString("00"));}oldComplete.Reconcile(app.assets.Catalog);Require(oldComplete.ChapterUnlocked(app.assets.Catalog.chapters[2],app.assets.Catalog),"previously unlocked third chapter is preserved during migration");
            var source=app.assets.Level("c3_01");source.id=Guid.NewGuid().ToString("N");app.Edit(source);yield return Page(GamePage.Editing);var doc=app.Creator.Document;
            yield return Click("brush-8");yield return Click("channel-2");var a=app.Board.Screen(new Cell(1,1));Require(app.Creator.PointerSample(a,true,true,false),"paint third-channel plate");app.Creator.PointerSample(a,false,false,true);yield return null;
            yield return Click("brush-9");a=app.Board.Screen(new Cell(2,1));Require(app.Creator.PointerSample(a,true,true,false),"paint matching third-channel door");app.Creator.PointerSample(a,false,false,true);yield return null;
            Require(doc.Data.plates.Last().channel==2&&doc.Data.doors.Last().channel==2&&doc.Validation()==null,"editor pairs devices with chosen channel");
            Require(app.Creator.PickAt(a)==Brush.Door&&app.Creator.Channel==2,"eyedropper retains device channel");Capture("editor_circuits");yield return Click("edit-undo");yield return Click("edit-undo");Require(doc.Data.ContentEquals(source),"device edits undo without losing original level");
            yield return Click("test");yield return Page(GamePage.Playing);yield return Route("RRDRRR");yield return Page(GamePage.Editing);Require(app.Creator.Document==doc&&doc.Data.ContentEquals(source),"device trial shares core and returns intact");yield return Click("leave-editor");yield return Click("discard-leave");
        }
    }
}
