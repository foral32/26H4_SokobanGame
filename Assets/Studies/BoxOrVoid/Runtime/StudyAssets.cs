using System;
using System.Collections.Generic;
using BoxOrVoid.Core;
using TMPro;
using UnityEngine;

namespace BoxOrVoid.Study
{
    [Serializable] public sealed class SpriteEntry { public string key; public string sprite; }
    [Serializable] public sealed class AudioEntry { public string key; public string clip; }
    [Serializable] public sealed class StudyReplay {public string id,commands,source;}
    [Serializable] public sealed class ThemeData
    {
        public int chapter;
        public SpriteEntry[] sprites;
        public SpriteEntry[] backgrounds;
    }
    [Serializable] public sealed class LevelEntry
    {
        public string id;
        public int chapter;
        public string displayId;
        public bool defaultUnlocked;
        public string[] hintGroups;
        public float x;
        public float y;
        public string[] unlocks;
        public string lockedSprite;
        public string unlockedSprite;
        public string completedSprite;
        public string ChapterTitle => chapter == 0 ? "欢迎" : "箱即是空";
    }
    [Serializable] public sealed class CatalogData
    {
        public ThemeData[] themes;
        public AudioEntry[] audio;
        public LevelEntry[] levels;
        public NavigationNode[] navigation;
    }
    [Serializable] public sealed class NavigationNode
    {
        public string id,level,sprite;
        public int chapter;
        public float x,y;
        public bool isReturn;
        public string[] neighbors;
    }

    [CreateAssetMenu(menuName = "Box or Void Study/Assets")]
    public sealed class StudyAssets : ScriptableObject
    {
        [SerializeField] private TextAsset catalog;
        [SerializeField] private TextAsset[] levels;
        [SerializeField] private Sprite[] sprites;
        [SerializeField] private AudioClip[] audioClips;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TMP_FontAsset editorBodyFont;
        [SerializeField] private Material tileMaterial;
        [SerializeField] private TextAsset[] replays;
        [SerializeField] private UnityEngine.Rendering.RenderPipelineAsset renderPipeline;
        private Dictionary<string, Sprite> spriteMap;
        private Dictionary<string, AudioClip> audioMap;
        private Dictionary<string, TextAsset> levelMap;
        private Dictionary<string, string>[] themeMap;
        public CatalogData Data { get; private set; }
        public TMP_FontAsset Font => font;
        public TMP_FontAsset EditorBodyFont=>editorBodyFont!=null?editorBodyFont:font;
        public void ConfigureEditorFont(TMP_FontAsset value){editorBodyFont=value;}
        public Material TileMaterial=>tileMaterial;
        public UnityEngine.Rendering.RenderPipelineAsset RenderPipeline=>renderPipeline;
        public void ConfigureRendering(UnityEngine.Rendering.RenderPipelineAsset pipeline) {renderPipeline=pipeline;}

        public void Configure(TextAsset catalogAsset, TextAsset[] levelAssets, Sprite[] spriteAssets, AudioClip[] clips, TMP_FontAsset fontAsset,Material material,TextAsset[] replayAssets)
        { catalog = catalogAsset; levels = levelAssets; sprites = spriteAssets; audioClips = clips; font = fontAsset;tileMaterial=material;replays=replayAssets;spriteMap = null; }

        public void Initialize()
        {
            if (spriteMap != null) return;
            Data = JsonUtility.FromJson<CatalogData>(catalog.text);
            spriteMap = new Dictionary<string, Sprite>();
            audioMap = new Dictionary<string, AudioClip>();
            levelMap = new Dictionary<string, TextAsset>();
            foreach (var sprite in sprites) spriteMap[sprite.name] = sprite;
            foreach (var clip in audioClips) audioMap[clip.name] = clip;
            foreach (var level in levels) levelMap[level.name] = level;
            themeMap = new Dictionary<string, string>[Data.themes.Length];
            foreach (var theme in Data.themes)
            {
                var map = themeMap[theme.chapter] = new Dictionary<string, string>();
                foreach (var item in theme.sprites) map[item.key] = item.sprite;
                foreach (var item in theme.backgrounds) map[item.key] = item.sprite;
            }
        }
        public Sprite Sprite(string name)
        {
            Initialize();
            if(name==null) return null;
            if(spriteMap.TryGetValue(name,out var sprite)) return sprite;
            return spriteMap.TryGetValue(name+"_0",out sprite)?sprite:null;
        }
        public Sprite ThemeSprite(int chapter, string key) { Initialize(); return themeMap[chapter].TryGetValue(key, out var n) ? Sprite(n) : null; }
        public AudioClip Sound(string key)
        {
            Initialize();
            foreach (var entry in Data.audio) if (entry.key == key && audioMap.TryGetValue(entry.clip, out var clip)) return clip;
            return null;
        }
        public LevelEntry Level(string id)
        {
            Initialize();
            foreach (var level in Data.levels) if (level.id == id) return level;
            throw new ArgumentException("Unknown level: " + id);
        }
        public PuzzleDefinition Definition(string id)
        { Initialize(); return PuzzleDefinition.FromData(JsonUtility.FromJson<LevelData>(levelMap[id].text)); }
        public LevelData LevelData(string id)
        {Initialize();return JsonUtility.FromJson<LevelData>(levelMap[id].text);}
        public StudyReplay Replay(string id)
        {
            foreach(var replay in replays) if(replay.name==id) return JsonUtility.FromJson<StudyReplay>(replay.text);
            throw new ArgumentException("Missing replay: "+id);
        }
    }
}
