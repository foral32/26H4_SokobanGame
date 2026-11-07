using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoxOrVoid.Core;
using BoxOrVoid.Study;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BoxOrVoid.Tests
{
    public sealed class ContentReplayTests
    {
        [Serializable] private sealed class Replay {public string id,commands;}
        private const string Root="Assets/Studies/BoxOrVoid";
        public static IEnumerable<string> Replays=>Directory.GetFiles(Root+"/Content/Replays","*.json").Select(Path.GetFileNameWithoutExtension);
        [TestCaseSource(nameof(Replays))]
        public void AuthoredLevelReplaySolvesAndUndoRestoresEveryStep(string id)
        {
            var data=JsonUtility.FromJson<LevelData>(File.ReadAllText(Root+"/Content/Levels/"+id+".json"));
            var replay=JsonUtility.FromJson<Replay>(File.ReadAllText(Root+"/Content/Replays/"+id+".json"));
            Assert.AreEqual(id,replay.id);
            var session=new PuzzleSession(PuzzleDefinition.FromData(data));var before=new List<string>();
            foreach(char c in replay.commands)
            {
                before.Add(session.State.PositionKey());var action=session.Apply((Command)"URDLS".IndexOf(c));
                Assert.IsTrue(action.Changed,"Rejected replay command "+c);
            }
            Assert.IsTrue(session.State.IsSolved);
            for(int i=before.Count-1;i>=0;i--) {session.Apply(Command.Undo);Assert.AreEqual(before[i],session.State.PositionKey(),"Undo step "+i);}
        }
        [Test] public void AllCatalogLinksResolveAndExactlyOneStudySceneExists()
        {
            var catalog=JsonUtility.FromJson<CatalogData>(File.ReadAllText(Root+"/Content/Catalog.json"));
            Assert.AreEqual(21,catalog.levels.Length);
            foreach(var level in catalog.levels)
            {
                Assert.IsTrue(File.Exists(Root+"/Content/Levels/"+level.id+".json"));
                foreach(string id in level.unlocks) Assert.IsTrue(catalog.levels.Any(l=>l.id==id));
            }
            foreach(var node in catalog.navigation)
                foreach(string id in node.neighbors) if(!string.IsNullOrEmpty(id)) Assert.IsTrue(catalog.navigation.Any(n=>n.id==id));
            Assert.AreEqual(1,AssetDatabase.FindAssets("t:Scene",new[]{Root}).Length);
        }
    }
}
