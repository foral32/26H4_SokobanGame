using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;
using TMPro;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        private IEnumerator OptionalChecks(Dictionary<string,ReplayPlan> plans)
        {
            var catalog=app.assets.Catalog;var second=catalog.chapters[1];var optional=second.levels.First(l=>!l.main);
            var isolated=new ProgressStore(Path.Combine(Application.temporaryCachePath,"BranchRules-"+Guid.NewGuid().ToString("N")));isolated.Reconcile(catalog);
            foreach(var node in catalog.chapters[0].levels.Where(l=>l.main))isolated.Complete(node.id);
            Require(!isolated.Unlocked(optional),"branch starts locked before its main anchor");
            foreach(var node in second.levels.Where(l=>l.main)){isolated.Complete(node.id);if(node.id==optional.requires[0])break;}
            Require(isolated.Unlocked(optional)&&!isolated.ChapterDone(second),"the main anchor independently unlocks its branch");isolated.Complete(optional.id);
            Require(!isolated.ChapterDone(second)&&!isolated.ChapterUnlocked(catalog.chapters[2],catalog),"branch completion cannot replace remaining main levels");
            foreach(var node in second.levels.Where(l=>l.main))isolated.Complete(node.id);
            Require(isolated.ChapterUnlocked(catalog.chapters[2],catalog)&&!isolated.Done(second.levels.Last(l=>!l.main).id),"next chapter unlocks without every optional completion");
            var replay=new ProgressStore(Path.Combine(Application.temporaryCachePath,"OldReplay-"+Guid.NewGuid().ToString("N")));foreach(var node in catalog.chapters[0].levels)replay.Complete(node.id);foreach(string id in new[]{"c2_01","c2_04","c2_02","c2_07","c3_01","c3_02"})replay.Complete(id);replay.Reconcile(catalog);
            Require(replay.ChapterUnlocked(catalog.chapters[2],catalog)&&replay.Unlocked(second.levels.First(l=>l.id=="c2_02"))&&replay.Unlocked(catalog.chapters[2].levels.First(l=>l.id=="c3_02")),"completed old nodes remain replayable after new prerequisites are added");
            foreach(var chapter in catalog.chapters.Where(c=>c.levels.Any(l=>!l.main)))
            {
                app.ShowLevels(chapter.id);yield return Ready();Require(app.UI.GetComponentsInChildren<BranchFrameGraphic>().Length==chapter.levels.Count(l=>!l.main),"only optional nodes have a persistent red frame");Capture(chapter.id+"_branches_unlocked");
                foreach(var node in chapter.levels.Where(l=>!l.main))
                {
                    int completed=chapter.levels.Count(l=>l.main&&app.Progress.Done(l.id));yield return Click("level-"+node.id);yield return Level(node.id);Capture(node.id+"_initial");string initial=app.Session.State.Key();
                    foreach(char direction in "URDL"){yield return Tap(Key(direction));report.exploredBranches++;if(app.Session.UndoCount>0)yield return Tap(KeyCode.Z);Require(app.Session.State.Key()==initial,"optional probe completely restores state "+node.id);}
                    yield return MirrorLevelChecks(node.id,plans[node.id]);yield return Route(plans[node.id].route);yield return Page(GamePage.Levels);yield return Ready();
                    Require(app.Progress.Done(node.id)&&app.ChapterId==chapter.id,"optional completion returns to its own selection screen "+node.id);Require(chapter.levels.Count(l=>l.main&&app.Progress.Done(l.id))==completed,"optional result does not change main completion");
                    Require(app.UI.GetComponentsInChildren<BranchFrameGraphic>().All(g=>g.color==BranchFrameGraphic.Red),"completed optional nodes retain red borders");report.levels.Add(node.id);
                }
                Require(app.UI.Root.Find("Chapter progress").GetComponent<TMP_Text>().text==chapter.levels.Count(l=>l.main)+" / "+chapter.levels.Count(l=>l.main),"chapter progress counts main levels only");Capture(chapter.id+"_branches_complete");
            }
            app.ShowChapters();yield return Ready();
        }
        private IEnumerator MultiLampEditorChecks()
        {
            var source=app.assets.Level("c1_01");source.id=Guid.NewGuid().ToString("N");app.Edit(source);yield return Page(GamePage.Editing);var doc=app.Creator.Document;
            yield return Click("brush-5");var a=app.Board.Screen(new Cell(5,3));var b=app.Board.Screen(new Cell(6,3));Require(app.Creator.PointerSample(a,true,true,false)&&app.Creator.PointerSample(b,false,true,false),"drag stamps two independent lamps");app.Creator.PointerSample(b,false,false,true);yield return null;
            Require(doc.Data.pieces.Count(p=>p.kind==PieceKind.Lamp)==2&&doc.UndoCount==1,"multiple lamp brush never relocates an earlier stamp");
            string file=app.Library.Export(doc.Data);Require(app.Library.PreviewImport(file).pieces.Count(p=>p.kind==PieceKind.Lamp)==2,"multiple lamps survive JSON export and import");
            yield return Click("edit-undo");Require(doc.Data.pieces.Length==1,"multi-lamp drag undo is grouped");yield return Click("edit-redo");Capture("editor_multiple_lamps");
            yield return Click("test");yield return Page(GamePage.Playing);yield return Route("RRR");
            var state=app.Session.State;Require(state.Position(1)==new Cell(6,3)&&state.Position(2)==new Cell(7,3),"trial pushes both lamps through the same chain");
            foreach(var glow in app.Board.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Lamp glow "))){int i=int.Parse(glow.name.Substring("Lamp glow ".Length));Require(Vector3.Distance(glow.transform.localPosition,app.Board.Position(state.Position(i)))<.001f,"each lamp glow follows its own piece");}
            yield return Tap(KeyCode.Z);Require(app.Session.State.Position(1)==new Cell(5,3)&&app.Session.State.Position(2)==new Cell(6,3),"trial undo restores both lamp locations");
            yield return Tap(KeyCode.F5);yield return Page(GamePage.Editing);Require(doc.Data.pieces.Count(p=>p.kind==PieceKind.Lamp)==2,"returning to editor restores original multi-lamp layout");yield return Click("leave-editor");yield return Click("discard-leave");
        }
    }
}
