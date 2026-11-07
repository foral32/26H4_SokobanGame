using System;
using System.IO;
using NUnit.Framework;
using BoxOrVoid.Study;

namespace BoxOrVoid.Tests
{
    public sealed class ProgressStoreTests
    {
        private string folder;
        private LevelEntry[] levels;
        [SetUp] public void Setup()
        {
            folder=Path.Combine(Path.GetTempPath(),"BoxOrVoidTests",Guid.NewGuid().ToString("N"));
            levels=new[]{new LevelEntry{id="a",chapter=0,defaultUnlocked=true,unlocks=new[]{"b"}},new LevelEntry{id="b",chapter=0,unlocks=new string[0]},new LevelEntry{id="c",chapter=1,defaultUnlocked=true,unlocks=new string[0]}};
        }
        [TearDown] public void Cleanup() { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
        [Test] public void CompletionUnlocksFollowingLevelAndThenChapterAfterReload()
        {
            var store=new ProgressStore(folder,levels);store.Load();
            Assert.IsTrue(store.IsUnlocked("a"));Assert.IsFalse(store.IsUnlocked("b"));Assert.IsFalse(store.IsUnlocked("c"));
            store.Complete("a");Assert.IsTrue(store.IsUnlocked("b"));Assert.IsFalse(store.IsUnlocked("c"));
            store.Complete("b");store.Data.lastLevel="c";store.Save();
            var reloaded=new ProgressStore(folder,levels);reloaded.Load();
            Assert.IsTrue(reloaded.IsUnlocked("c"));Assert.AreEqual(2,reloaded.CompletedCount);Assert.AreEqual("c",reloaded.Data.lastLevel);
        }
        [Test] public void UnknownAndDuplicateSavedIdsDoNotInflateProgress()
        {
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"progress-v1.json"),"{\"version\":1,\"completed\":[\"a\",\"a\",\"unknown\"]}");
            var store=new ProgressStore(folder,levels);store.Load();
            Assert.AreEqual(1,store.CompletedCount);Assert.IsFalse(store.ChapterUnlocked(1));
        }
        [Test] public void CorruptSaveIsPreservedAndFreshProgressRemainsUsable()
        {
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"progress-v1.json"),"broken {");
            var store=new ProgressStore(folder,levels);store.Load();
            Assert.AreEqual(0,store.CompletedCount);Assert.IsNotEmpty(store.Warning);
            Assert.AreEqual(1,Directory.GetFiles(folder,"*.invalid-*").Length);store.Complete("a");
            var reloaded=new ProgressStore(folder,levels);reloaded.Load();Assert.AreEqual(1,reloaded.CompletedCount);
        }
    }
}
