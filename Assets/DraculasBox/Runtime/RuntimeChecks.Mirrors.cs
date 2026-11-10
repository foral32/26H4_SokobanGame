using System;
using System.Collections;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private IEnumerator MirrorLevelChecks(string id,ReplayPlan plan)
        {
            if(!id.StartsWith("c4_",StringComparison.Ordinal))yield break;
            string initial=app.Session.State.Key();var field=LightField.Compute(app.Session.State);var data=app.Session.State.Level.Export();
            Require(!data.sunEnabled,"mirror chapter isolates cardinal reflection "+id);
            for(int i=0;i<data.pieces.Length;i++)if(data.pieces[i].kind==PieceKind.MirrorSlash||data.pieces[i].kind==PieceKind.MirrorBackslash)
            {
                var art=app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Piece "+i);
                Require(art.sprite.name==(data.pieces[i].kind==PieceKind.MirrorSlash?"Mirror slash":"Mirror backslash"),"each mirror retains its authored orientation "+id);
                var ray=app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Mirror bend "+i);var p=app.Session.State.Position(i);
                Require(ray.sprite.name=="Mirror light "+field.ReflectionEdges[p.y*data.rows[0].Length+p.x],"mirror elbow shows incoming and reflected edges "+id);
            }
            if(string.IsNullOrEmpty(plan.wrong))yield break;
            yield return Route(plan.wrong);Require(app.Session.State.Dead&&app.Page==GamePage.Dead,"the chosen unsafe approach exposes the vampire "+id);report.deathsRecovered++;
            Capture(id+"_wrong_order");yield return Undo(plan.wrong.Length);Require(app.Session.State.Key()==initial&&!app.Session.State.Dead,"undo restores complete reflected light layout "+id);
        }
        private IEnumerator MirrorEditorChecks()
        {
            var source=app.assets.Level("c4_01");source.id=Guid.NewGuid().ToString("N");app.Edit(source);yield return Page(GamePage.Editing);var doc=app.Creator.Document;
            foreach(Brush brush in Enum.GetValues(typeof(Brush)))if(brush!=Brush.Erase){yield return Click("brush-"+(int)brush);Require(app.Creator.Selected==brush,"scrolled material remains raycast clickable "+brush);}
            yield return Click("brush-10");var slash=app.Board.Screen(new Cell(5,5));Require(app.Creator.PointerSample(slash,true,true,false),"slash mirror is paintable");app.Creator.PointerSample(slash,false,false,true);
            yield return Click("brush-11");var back=app.Board.Screen(new Cell(4,5));Require(app.Creator.PointerSample(back,true,true,false),"backslash mirror is paintable");app.Creator.PointerSample(back,false,false,true);yield return null;
            Require(doc.Data.pieces.Count(p=>p.kind==PieceKind.MirrorSlash)==2&&doc.Data.pieces.Count(p=>p.kind==PieceKind.MirrorBackslash)==1,"mirror brushes never overwrite orientation or relocate an existing mirror");
            Require(app.Creator.PickAt(back)==Brush.MirrorBackslash,"eyedropper preserves backslash mirror kind");
            string file=app.Library.Export(doc.Data);var imported=app.Library.PreviewImport(file);Require(imported.pieces.Select(p=>p.kind).SequenceEqual(doc.Data.pieces.Select(p=>p.kind)),"mirror kinds survive export and import");
            yield return Click("edit-undo");Require(doc.Data.pieces.Length==3,"mirror placement undoes independently");yield return Click("edit-redo");Capture("editor_mirrors");
            string original=JsonUtility.ToJson(doc.Data);yield return Click("test");yield return Page(GamePage.Playing);yield return Route("ULUU");yield return Page(GamePage.Editing);
            Require(JsonUtility.ToJson(doc.Data)==original&&doc.Data.pieces.Length==4,"mirror trial completion returns the full authored layout");yield return Click("leave-editor");yield return Click("discard-leave");
        }
    }
}
