using System.Collections;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;
using TMPro;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private IEnumerator RectangleRegression()
        {
            yield return Click("new");yield return Click("save");var doc=app.Creator.Document;var saved=doc.Data.Copy();
            Require(app.UI.FindButton("channel-0")==null,"connection groups hidden for ordinary objects");
            yield return Click("shape-rectangle");var a=app.Board.Screen(new Cell(1,1));var b=app.Board.Screen(new Cell(3,2));
            Require(app.Creator.PointerSample(a,true,true,false),"begin rectangle");app.Creator.PointerSample(b,false,true,false);yield return null;
            Require(doc.Data.ContentEquals(saved)&&doc.UndoCount==0&&app.Creator.RegionActive,"rectangle previews without modifying document");Capture("editor_rectangle_preview");
            app.Creator.PointerSample(new Vector2(-100,-100),false,false,true);yield return null;
            Require(doc.Data.pieces.Length==saved.pieces.Length+6&&doc.UndoCount==1&&!app.Creator.RegionActive,"release applies visible rectangle as one undo record");yield return Click("edit-undo");Require(doc.Data.ContentEquals(saved)&&!doc.Dirty,"rectangle undo returns to saved baseline");
            app.Creator.PointerSample(a,true,true,false);app.Creator.PointerSample(b,false,true,false);Require(app.Creator.CancelRectangle()&&!app.Creator.RegionActive&&doc.Data.ContentEquals(saved)&&doc.UndoCount==0,"cancel rectangle leaves no data or history");
            yield return Click("brush-3");Require(app.UI.FindButton("shape-rectangle").interactable,"rectangle remains available for right-button erasing with a unique role brush");
            yield return Click("brush-8");Require(app.UI.FindButton("channel-0")!=null&&app.UI.Root.Find("Materials/Connection group").GetComponent<TMP_Text>().text=="连接组","device brushes explain connection groups");yield return Click("channel-2");
            app.Creator.PointerSample(a,true,true,false);app.Creator.PointerSample(b,false,true,false);app.Creator.PointerSample(b,false,false,true);yield return null;
            Require(doc.Data.plates.Length==6&&doc.Data.plates.All(p=>p.channel==2)&&doc.UndoCount==1,"rectangle plate placement retains selected group");
            yield return Click("brush-9");var door=app.Board.Screen(new Cell(4,2));app.Creator.PointerSample(door,true,true,false);app.Creator.PointerSample(door,false,false,true);yield return null;Require(doc.Data.doors.Length==1&&doc.Data.doors[0].channel==2,"door uses the same named group");Capture("editor_rectangle_connections");
            var layered=doc.Data.Copy();int before=doc.UndoCount;yield return Click("brush-2");Require(app.UI.FindButton("channel-0")==null,"group controls hide again outside device tools");
            a=app.Board.Screen(new Cell(0,0));b=app.Board.Screen(new Cell(10,6));app.Creator.PointerSample(a,true,true,false,true);app.Creator.PointerSample(b,false,true,false,true);Capture("editor_erase_rectangle_preview");app.Creator.PointerSample(b,false,false,true,true);yield return null;
            Require(doc.Data.pieces.Length==0&&doc.Data.plates.Length==0&&doc.Data.doors.Length==0&&doc.Data.player==new Cell(-1,-1)&&doc.Data.coffin==new Cell(-1,-1)&&doc.UndoCount==before+1,"area erase clears mixed layers in one operation");yield return Click("edit-undo");Require(doc.Data.ContentEquals(layered),"area erase undo restores all roles and connection groups");
            yield return Click("edit-undo");yield return Click("edit-undo");Require(doc.Data.ContentEquals(saved)&&!doc.Dirty,"area history preserves complete original state");
            yield return Click("brush-2");a=app.Board.Screen(new Cell(1,1));b=app.Board.Screen(new Cell(2,2));app.Creator.PointerSample(a,true,true,false);app.Creator.PointerSample(b,false,true,false);Require(app.Creator.Save()&&!doc.Dirty&&doc.Data.pieces.Length==saved.pieces.Length+4,"save commits pending rectangle before writing");yield return Click("edit-undo");Require(doc.Dirty&&doc.Data.ContentEquals(saved),"undo after saving rectangle correctly marks old version dirty");
            yield return Click("leave-editor");yield return Click("discard-leave");
        }
    }
}
