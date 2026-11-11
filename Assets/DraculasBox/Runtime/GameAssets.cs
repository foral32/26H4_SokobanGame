using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using TMPro;
using UnityEngine;

namespace DraculasBox.Runtime
{
    [Serializable] public sealed class ChapterLink {public string id;public bool main=true;public string[] requires;public bool branchAbove;}
    [Serializable] public sealed class Chapter {public string id,title,description,requiresChapter;public ChapterLink[] levels;}
    [Serializable] public sealed class ChapterCatalog {public Chapter[] chapters;}
    [Serializable] public sealed class ReplayPlan {public string id,route,shortest,alternate,wrong;public int pushes;public string[] essential;}
    [Serializable] public sealed class ReplayCatalog {public ReplayPlan[] levels;}
    public sealed partial class GameAssets:ScriptableObject
    {
        public TextAsset catalog,replays; public TextAsset[] levels;public Sprite[] sprites;public AudioClip[] audio;
        public AudioClip mainMusic,editorMusic;
        public TMP_FontAsset font,bodyFont;public Material material;public UnityEngine.Rendering.RenderPipelineAsset pipeline;
        private Dictionary<string,Sprite> spriteMap;
        private readonly Dictionary<string,Sprite> themedSprites=new Dictionary<string,Sprite>();
        public void ResetSpriteCache(){spriteMap=null;}
        public ChapterCatalog Catalog=>JsonUtility.FromJson<ChapterCatalog>(catalog.text);
        public LevelData Level(string id)=>JsonUtility.FromJson<LevelData>(levels.First(l=>l!=null&&l.name==id).text);
        public Sprite Sprite(string name)
        {
            if(name=="vampire")return SurfaceSprites.VampireIcon;
            if(name=="mirror-slash"||name=="mirror-backslash")return SurfaceSprites.Mirror(name=="mirror-backslash");
            if(name=="Box_active_white")return SurfaceSprites.RaisedBox;
            if(spriteMap==null)
            {
                spriteMap=new Dictionary<string,Sprite>();
                foreach(var sprite in sprites.Where(s=>s!=null))
                {
                    spriteMap[sprite.name]=sprite;
                    // Unity 6's single-sprite importer may append the generated local sprite index.
                    if(sprite.name.EndsWith("_0",StringComparison.Ordinal))spriteMap[sprite.name.Substring(0,sprite.name.Length-2)]=sprite;
                }
            }
            return spriteMap.TryGetValue(name,out var s)?ThemedSprite(name,s):null;
        }
        public AudioClip Clip(string name)=>audio.FirstOrDefault(a=>a!=null&&a.name==name);
    }
    [Serializable] public sealed class ProgressData
    {public int schema=1,catalogRevision;public bool allLevelsUnlocked;public List<string> completed=new List<string>(),unlockedChapters=new List<string>();public string lastLevel="c1_01",theme=GameThemes.DefaultThemeId;public float music=.25f,effects=.55f;}
    public sealed class ProgressStore
    {
        private readonly string path;public ProgressData Data=new ProgressData();public string Warning {get;private set;}
        public ProgressStore(string folder){Directory.CreateDirectory(folder);path=Path.Combine(folder,"progress.json");
            try{if(File.Exists(path)){var d=JsonUtility.FromJson<ProgressData>(File.ReadAllText(path));if(d==null||d.schema!=1||d.completed==null)throw new InvalidDataException();Data=d;Data.music=Mathf.Clamp01(Data.music);Data.effects=Mathf.Clamp01(Data.effects);}}
            catch{Warning="进度文件无法读取，已使用新进度；原文件保留。";}Data.theme=GameThemes.Resolve(Data.theme).Id;}
        public bool Done(string id)=>Data.completed.Contains(id);
        public bool Unlocked(ChapterLink link)=>Data.allLevelsUnlocked||Done(link.id)||link.requires==null||link.requires.All(Done);
        public bool ChapterDone(Chapter c)=>c.levels.Where(l=>l.main).All(l=>Done(l.id));
        public bool ChapterUnlocked(Chapter chapter,ChapterCatalog catalog)=>Data.allLevelsUnlocked||string.IsNullOrEmpty(chapter.requiresChapter)||Data.unlockedChapters!=null&&Data.unlockedChapters.Contains(chapter.id)||catalog.chapters.Any(c=>c.id==chapter.requiresChapter&&ChapterDone(c));
        public void RememberUnlocks(ChapterCatalog catalog){if(Data.unlockedChapters==null)Data.unlockedChapters=new List<string>();foreach(var chapter in catalog.chapters)if(ChapterUnlocked(chapter,catalog)&&!Data.unlockedChapters.Contains(chapter.id))Data.unlockedChapters.Add(chapter.id);Save();}
        public void Reconcile(ChapterCatalog catalog)
        {
            if(Data.unlockedChapters==null)Data.unlockedChapters=new List<string>();
            if(Data.catalogRevision<2){if(Done("c1_07")&&!Data.unlockedChapters.Contains("chapter_2"))Data.unlockedChapters.Add("chapter_2");if(Done("c2_07")&&!Data.unlockedChapters.Contains("chapter_3"))Data.unlockedChapters.Add("chapter_3");Data.catalogRevision=2;RememberUnlocks(catalog);}
            if(catalog.chapters.Any(c=>c.levels.Any(l=>l.id==Data.lastLevel)))return;
            var chapter=catalog.chapters.Last(c=>ChapterUnlocked(c,catalog));Data.lastLevel=(chapter.levels.FirstOrDefault(l=>l.main&&!Done(l.id))??chapter.levels.First(l=>l.main)).id;Save();
        }
        public void Complete(string id){if(!Done(id))Data.completed.Add(id);Data.lastLevel=id;Save();}
        public void UnlockCampaign()
        {
            var unlocked=JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(Data));unlocked.allLevelsUnlocked=true;
            Atomic(path,JsonUtility.ToJson(unlocked,true));Data=unlocked;Warning=null;
        }
        public void ClearCampaign(ChapterCatalog catalog)
        {
            var first=catalog.chapters.First(c=>c.levels.Any(l=>l.main));
            var cleared=new ProgressData{catalogRevision=Math.Max(2,Data.catalogRevision),lastLevel=first.levels.First(l=>l.main).id,
                theme=Data.theme,music=Data.music,effects=Data.effects};
            cleared.unlockedChapters=catalog.chapters.Where(c=>string.IsNullOrEmpty(c.requiresChapter)).Select(c=>c.id).ToList();
            // Commit the new file before replacing live state. A write failure preserves both.
            Atomic(path,JsonUtility.ToJson(cleared,true));Data=cleared;Warning=null;
        }
        public void Save(){try{Atomic(path,JsonUtility.ToJson(Data,true));Warning=null;}catch(Exception e){Warning="进度未写入，请检查文件夹权限："+e.Message;}}
        public static void Atomic(string path,string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,text);if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
