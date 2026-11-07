using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoxOrVoid.Core;
using BoxOrVoid.Study;
using BoxOrVoid.Study.LevelEditing;
using NUnit.Framework;
using UnityEngine;

namespace BoxOrVoid.Tests
{
    public sealed class LevelEditorTests
    {
        private string folder;
        [SetUp]public void Setup(){folder=Path.Combine(Path.GetTempPath(),"BoxVoidEditorTests-"+Guid.NewGuid().ToString("N"));}
        [TearDown]public void Cleanup(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
        private static void Put(LevelDocument d,int cell,ObjectKind kind,Tone tone)
        {d.level.grid[cell].Objects=d.level.grid[cell].Objects.Concat(new[]{new CellObjectData{ObjectType=kind,Color=tone}}).ToArray();}
        private static LevelDocument Playable()
        {var d=LevelDocument.New(7,7,"往返测试");Put(d,8,ObjectKind.Player,Tone.White);Put(d,15,ObjectKind.Player,Tone.Black);Put(d,10,ObjectKind.PlayerGoal,Tone.White);return d;}
        [Test]public void StrokeIsOneUndoAndRepeatingOverCellsDoesNotStack()
        {
            var s=new LevelEditSession(LevelDocument.New());s.Begin();s.Place(4);s.Place(4);s.Place(5);s.Commit();Assert.AreEqual(1,s.Document.level.grid[4].Objects.Length);s.Undo();Assert.IsEmpty(s.Document.level.grid[4].Objects);Assert.IsEmpty(s.Document.level.grid[5].Objects);Assert.IsFalse(s.CanUndo);s.Redo();Assert.AreEqual(1,s.Document.level.grid[5].Objects.Length);
        }
        [Test]public void CancelStrokeDoesNotCreateHistory()
        {var s=new LevelEditSession(LevelDocument.New());s.Begin();s.Place(0);s.Cancel();Assert.IsEmpty(s.Document.level.grid[0].Objects);Assert.IsFalse(s.CanUndo);}
        [Test]public void PlacingPlayerMovesOriginalAndUndoRestoresIt()
        {var s=new LevelEditSession(Playable()){Brush=ObjectKind.Player,Tone=Tone.White};s.Begin();s.Place(20);s.Commit();Assert.IsFalse(s.Document.level.grid[8].Objects.Any(o=>o.ObjectType==ObjectKind.Player));s.Undo();Assert.AreEqual(ObjectKind.Player,s.Document.level.grid[8].Objects[0].ObjectType);}
        [Test]public void CoveredPlayerIsValidAndLayerRemovalDoesNotRemoveWallOrBox()
        {var d=Playable();d.level.grid[8].Objects=Array.Empty<CellObjectData>();Put(d,8,ObjectKind.Wall,Tone.Black);Put(d,8,ObjectKind.Player,Tone.White);Put(d,8,ObjectKind.Box,Tone.Black);Assert.IsEmpty(LevelValidation.ForPlay(d));var s=new LevelEditSession(d);s.RemoveLayer(8,1);Assert.AreEqual(2,s.Document.level.grid[8].Objects.Length);Assert.AreEqual(ObjectKind.Wall,s.Document.level.grid[8].Objects[0].ObjectType);Assert.IsTrue(LevelValidation.ForPlay(s.Document).Any(p=>p.Kind==ObjectKind.Player));s.Undo();Assert.IsEmpty(LevelValidation.ForPlay(s.Document));}
        [Test]public void GrayWallCannotDestroyExistingPieces()
        {var d=Playable();var s=new LevelEditSession(d){Brush=ObjectKind.Wall,Tone=Tone.Gray};s.Begin();Assert.IsFalse(s.Place(8));s.Commit();Assert.AreEqual(ObjectKind.Player,s.Document.level.grid[8].Objects[0].ObjectType);Assert.IsFalse(s.CanUndo);}
        [Test]public void ResizePreviewAndUndoPreserveWholeDocument()
        {var d=Playable();Put(d,0,ObjectKind.Box,Tone.Black);var s=new LevelEditSession(d);string before=JsonUtility.ToJson(d.level);s.PreviewResize(3,3,4,out int cropped);Assert.Greater(cropped,0);Assert.AreEqual(before,JsonUtility.ToJson(s.Document.level));s.Resize(3,3,4);s.Undo();Assert.AreEqual(before,JsonUtility.ToJson(s.Document.level));}
        [Test]public void CopyMoveRotatePreservesLayerOrderAndPlayerUniqueness()
        {var d=Playable();Put(d,8,ObjectKind.Box,Tone.Black);var s=new LevelEditSession(d);s.Select(8,9);var stamp=s.CopySelection().Transform(true,false,false);Assert.IsTrue(s.Paste(stamp,4,3,true));Assert.AreEqual(1,s.Document.level.grid.Sum(c=>c.Objects.Count(o=>o.ObjectType==ObjectKind.Player&&o.Color==Tone.White)));Assert.AreEqual(ObjectKind.Player,s.Document.level.grid[32].Objects[0].ObjectType);s.Undo();Assert.AreEqual(2,s.Document.level.grid[8].Objects.Length);}
        [Test]public void OutOfBoundsPasteIsAtomic()
        {var s=new LevelEditSession(Playable());s.Select(7,9);string before=JsonUtility.ToJson(s.Document);Assert.IsFalse(s.Paste(s.CopySelection(),6,6));Assert.AreEqual(before,JsonUtility.ToJson(s.Document));Assert.IsFalse(s.CanUndo);}
        [Test]public void FillMatchesConnectedTerrain()
        {var d=LevelDocument.New(3,3);for(int y=0;y<3;y++)Put(d,y*3+1,ObjectKind.Wall,Tone.Gray);var s=new LevelEditSession(d);CollectionAssert.AreEquivalent(new[]{0,3,6},s.FillArea(0));}
        [Test]public void AdditiveSelectionPreservesUnselectedHolesWhenMoving()
        {var d=LevelDocument.New(7,7);Put(d,8,ObjectKind.Box,Tone.White);Put(d,9,ObjectKind.Box,Tone.Black);Put(d,10,ObjectKind.Box,Tone.White);var s=new LevelEditSession(d);s.Select(8,8);var original=s.SelectedIndices.ToArray();s.Select(10,10,original);Assert.AreEqual(2,s.SelectedIndices.Count());Assert.IsTrue(s.Paste(s.CopySelection(),1,3,true));Assert.AreEqual(1,s.Document.level.grid[9].Objects.Length);Assert.AreEqual(0,s.Document.level.grid[8].Objects.Length);Assert.AreEqual(0,s.Document.level.grid[23].Objects.Length);}
        [Test]public void UndoDuringStrokeCancelsOnlyThatStroke()
        {var s=new LevelEditSession(LevelDocument.New());s.Begin();s.Place(0);s.Commit();s.Begin();s.Place(1);s.Undo();Assert.AreEqual(1,s.Document.level.grid[0].Objects.Length);Assert.AreEqual(0,s.Document.level.grid[1].Objects.Length);Assert.IsTrue(s.CanUndo);}
        [Test]public void NewEditsInvalidateRedoAndSolvedBadgeUsesLayoutOnly()
        {var d=Playable();d.solvedHash=d.LayoutHash();d.rulesVersion=LevelDocument.CurrentRules;var s=new LevelEditSession(d);s.SetName("新名字");Assert.IsTrue(s.Document.IsPlayedThrough);s.Begin();s.Place(1);s.Commit();Assert.IsFalse(s.Document.IsPlayedThrough);s.Undo();Assert.IsTrue(s.Document.IsPlayedThrough);s.SetTheme(0);Assert.IsFalse(s.CanRedo);}
        [Test]public void PlayStateCannotMutateDraftOrEditHistory()
        {var s=new LevelEditSession(Playable());string initial=JsonUtility.ToJson(s.Document);var game=new PuzzleSession(PuzzleDefinition.FromData(s.Document.level));game.Apply(Command.Right);game.Apply(Command.Reset);game.Apply(Command.Undo);Assert.AreEqual(initial,JsonUtility.ToJson(s.Document));Assert.IsFalse(s.CanUndo);}
        [Test]public void UnfinishedDraftCanSaveReloadAndExportImportWithoutLosingLayers()
        {
            var lib=new UserLevelLibrary(folder);var d=LevelDocument.New();Put(d,10,ObjectKind.Box,Tone.White);Put(d,10,ObjectKind.Box,Tone.Black);lib.Save(d);Assert.IsNotEmpty(LevelValidation.ForPlay(d));var reloaded=lib.Load(d.id);Assert.AreEqual(d.LayoutHash(),reloaded.LayoutHash());string path=lib.Export(d);var imported=lib.Import(lib.ReadImport(path));Assert.AreNotEqual(d.id,imported.id);Assert.AreEqual(d.LayoutHash(),imported.LayoutHash());Assert.AreNotEqual(d.name,imported.name);Assert.AreEqual(2,lib.List().Count);Assert.AreNotEqual(path,lib.Export(d));
        }
        [Test]public void AutomaticRecoveryDoesNotOverwriteManualSave()
        {var lib=new UserLevelLibrary(folder);var d=Playable();lib.Save(d);string hash=d.LayoutHash();Put(d,1,ObjectKind.Box,Tone.White);lib.AutoSave(d);Assert.AreEqual(hash,lib.Load(d.id).LayoutHash());Assert.IsTrue(lib.List()[0].HasRecovery);Assert.AreEqual(d.LayoutHash(),lib.Load(d.id,true).LayoutHash());lib.Save(d);Assert.IsFalse(lib.List()[0].HasRecovery);}
        [Test]public void BrokenPrimaryRecoversBackupWithoutDeletingBrokenBytes()
        {var lib=new UserLevelLibrary(folder);var d=Playable();lib.Save(d);string old=d.LayoutHash();Put(d,1,ObjectKind.Box,Tone.White);lib.Save(d);string path=Path.Combine(folder,"Levels",d.id+".json");File.WriteAllText(path,"broken");Assert.IsTrue(lib.List()[0].FromBackup);Assert.AreEqual(old,lib.Load(d.id).LayoutHash());Assert.AreEqual("broken",File.ReadAllText(path));}
        [Test]public void LibraryRenameAndDuplicateRetainNewerRecoveryContents()
        {var lib=new UserLevelLibrary(folder);var d=Playable();lib.Save(d);Put(d,1,ObjectKind.Box,Tone.Black);lib.AutoSave(d);var reopened=new UserLevelLibrary(folder);var row=reopened.List()[0];var copy=reopened.Duplicate(reopened.LatestDocument(row));Assert.AreEqual(d.LayoutHash(),copy.LayoutHash());var renamed=reopened.LatestDocument(row);renamed.name="保留自动草稿";reopened.Save(renamed);Assert.AreEqual(d.LayoutHash(),reopened.Load(d.id).LayoutHash());Assert.AreEqual("保留自动草稿",reopened.Load(d.id).name);Assert.IsFalse(reopened.List().Single(e=>e.Document.id==d.id).HasRecovery);}
        [Test]public void RecycleAndRestorePreserveLatestDraft()
        {var lib=new UserLevelLibrary(folder);var d=Playable();lib.Save(d);Put(d,2,ObjectKind.Box,Tone.Black);lib.AutoSave(d);lib.Recycle(d.id);Assert.IsEmpty(lib.List());Assert.AreEqual(1,lib.List(true).Count);lib.Restore(d.id);Assert.AreEqual(d.LayoutHash(),lib.Load(d.id).LayoutHash());Assert.IsEmpty(lib.List(true));}
        [Test]public void SaveAfterCorruptionKeepsLastGoodBackup()
        {var lib=new UserLevelLibrary(folder);var d=Playable();lib.Save(d);lib.Save(d);string path=Path.Combine(folder,"Levels",d.id+".json"),backup=File.ReadAllText(path+".bak");File.WriteAllText(path,"broken");var recovered=lib.Load(d.id);lib.Save(recovered);Assert.AreEqual(backup,File.ReadAllText(path+".bak"));Assert.AreEqual(1,Directory.GetFiles(Path.GetDirectoryName(path),"*.invalid-*").Length);Assert.AreEqual(d.LayoutHash(),lib.Load(d.id).LayoutHash());}
        [Test]public void ImportDoesNotTrustPathsOrCompletionAndRejectsDamagedArrays()
        {var lib=new UserLevelLibrary(folder);var d=Playable();d.id="../../outside";d.solvedHash=d.LayoutHash();d.rulesVersion=LevelDocument.CurrentRules;var imported=lib.Import(d);Assert.IsTrue(Guid.TryParseExact(imported.id,"N",out _));Assert.IsFalse(imported.IsPlayedThrough);d.level.grid=new CellData[1];Assert.Throws<ArgumentException>(()=>lib.Import(d));Assert.AreEqual(1,lib.List().Count);}
        private sealed class Keys:IStudyKeyboard
        {public readonly HashSet<KeyCode> KeysHeld=new HashSet<KeyCode>();public KeyCode Down;public bool Held(KeyCode k)=>KeysHeld.Contains(k);public bool Pressed(KeyCode k)=>k==Down;}
        [TestCase(KeyCode.Z,"undo")][TestCase(KeyCode.Y,"redo")][TestCase(KeyCode.F5,"play")][TestCase(KeyCode.Tab,"tone")][TestCase(KeyCode.Alpha6,"brush-5")]
        public void HotkeysAreExplicitAndSuppressedWhileTyping(KeyCode key,string expected)
        {var k=new Keys{Down=key};Assert.AreEqual(expected,EditorShortcuts.Poll(k,false,false));Assert.IsNull(EditorShortcuts.Poll(k,true,false));Assert.IsNull(EditorShortcuts.Poll(k,false,true));}
        [Test]public void ModifiedBindingsDoNotFallThroughToLetters()
        {var k=new Keys{Down=KeyCode.Z};k.KeysHeld.Add(KeyCode.LeftControl);Assert.AreEqual("undo",EditorShortcuts.Poll(k,false,false));k.KeysHeld.Add(KeyCode.LeftShift);Assert.AreEqual("redo",EditorShortcuts.Poll(k,false,false));k.Down=KeyCode.S;Assert.AreEqual("save",EditorShortcuts.Poll(k,false,false));k.Down=KeyCode.V;Assert.AreEqual("paste",EditorShortcuts.Poll(k,false,false));}
    }
}
