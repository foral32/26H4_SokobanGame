using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BoxOrVoid.Study
{
    [Serializable] public sealed class StudyProgress
    {
        public int version=1;
        public List<string> completed=new List<string>();
        public int lastChapter;
        public string lastLevel="";
        public float musicVolume=.3f;
        public float effectsVolume=.5f;
    }

    public sealed class ProgressStore
    {
        private readonly string path;
        private readonly LevelEntry[] levels;
        private readonly HashSet<string> completed=new HashSet<string>();
        private bool savingBlocked;
        public StudyProgress Data {get;private set;}=new StudyProgress();
        public string Warning {get;private set;}="";
        public string SavePath=>path;
        public int CompletedCount=>completed.Count;
        public ProgressStore(string folder,LevelEntry[] catalog) { path=Path.Combine(folder,"progress-v1.json");levels=catalog; }

        public void Load()
        {
            Warning="";savingBlocked=false;Data=new StudyProgress();completed.Clear();
            if(!File.Exists(path)) return;
            try
            {
                Data=JsonUtility.FromJson<StudyProgress>(File.ReadAllText(path));
                if(Data==null || Data.version!=1) throw new InvalidDataException("Unsupported or missing save version");
                foreach(string id in Data.completed??new List<string>()) if(levels.Any(l=>l.id==id)) completed.Add(id);
                Data.lastChapter=Mathf.Clamp(Data.lastChapter,0,1);
                if(!levels.Any(l=>l.id==Data.lastLevel)) Data.lastLevel="";
                Data.musicVolume=ClampVolume(Data.musicVolume,.3f);Data.effectsVolume=ClampVolume(Data.effectsVolume,.5f);
            }
            catch(Exception ex) when(ex is ArgumentException || ex is InvalidDataException)
            {
                // Preserve the unreadable bytes; recovery never writes into the reference game's save.
                Data=new StudyProgress();completed.Clear();
                try {File.Move(path,path+".invalid-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));Warning="存档无法读取，已保留原文件并开始新的进度。";}
                catch(Exception archiveError) when(archiveError is IOException || archiveError is UnauthorizedAccessException)
                {savingBlocked=true;Warning="存档无法读取或备份，本次进度暂不保存。";}
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
            { Data=new StudyProgress();completed.Clear();savingBlocked=true;Warning="暂时无法读取存档，本次进度暂不保存。"; }
        }
        public bool IsComplete(string id)=>completed.Contains(id);
        public int ChapterCount(int chapter)=>levels.Count(l=>l.chapter==chapter && IsComplete(l.id));
        public bool ChapterUnlocked(int chapter)=>chapter==0 || levels.Where(l=>l.chapter==0).All(l=>IsComplete(l.id));
        public bool IsUnlocked(string id)
        {
            var level=levels.FirstOrDefault(l=>l.id==id);
            return level!=null && ChapterUnlocked(level.chapter) && (level.defaultUnlocked || IsComplete(id) || levels.Any(l=>IsComplete(l.id) && (l.unlocks??Array.Empty<string>()).Contains(id)));
        }
        public void Complete(string id)
        {
            if(!levels.Any(l=>l.id==id)) throw new ArgumentException("Unknown progress level");
            completed.Add(id);Save();
        }
        public bool Save()
        {
            if(savingBlocked) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                Data.completed=levels.Where(l=>completed.Contains(l.id)).Select(l=>l.id).ToList();
                string temporary=path+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(Data,true));
                if(File.Exists(path)) File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
                return true;
            }
            catch(IOException) { Warning="暂时无法保存进度。";return false; }
            catch(UnauthorizedAccessException) { Warning="存档目录不可写。";return false; }
        }
        private static float ClampVolume(float value,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp01(value);
    }
}
