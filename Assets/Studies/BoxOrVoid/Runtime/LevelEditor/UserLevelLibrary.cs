using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BoxOrVoid.Core;
using UnityEngine;

namespace BoxOrVoid.Study.LevelEditing
{
    public sealed class LibraryEntry
    {
        public LevelDocument Document;
        public bool HasRecovery,FromBackup;
        public string Warning;
    }
    /// <summary>Private user files. Imported identity is never trusted as a destination path.</summary>
    public sealed class UserLevelLibrary
    {
        public const long MaxFileBytes=4*1024*1024;
        public string Root {get;}
        public string ExportFolder=>Path.Combine(Root,"Shared");
        public string LastWarning {get;private set;}="";
        private string SavedFolder=>Path.Combine(Root,"Levels");
        private string RecoveryFolder=>Path.Combine(Root,"Recovery");
        private string RecycleFolder=>Path.Combine(Root,"Recycle");
        public UserLevelLibrary(string root){Root=Path.GetFullPath(root);}
        private static string SafeId(string id)
        {if(!Guid.TryParseExact(id,"N",out _))throw new ArgumentException("关卡编号无效。");return id;}
        private string SavedPath(string id)=>Path.Combine(SavedFolder,SafeId(id)+".json");
        private string RecoveryPath(string id)=>Path.Combine(RecoveryFolder,SafeId(id)+".json");
        public List<LibraryEntry> List(bool recycled=false)
        {
            var result=new Dictionary<string,LibraryEntry>();LastWarning="";
            string folder=recycled?RecycleFolder:SavedFolder;
            if(Directory.Exists(folder))foreach(string path in Directory.GetFiles(folder,"*.json"))
            {
                try
                {
                    bool fallback=false;LevelDocument d;
                    try{d=Read(path);}catch(Exception ex)when(IsFileFailure(ex)){d=Read(path+".bak");fallback=true;}
                    SafeId(d.id);result[d.id]=new LibraryEntry{Document=d,FromBackup=fallback,Warning=fallback?"已从上次备份找回":""};
                }
                catch(Exception ex)when(IsFileFailure(ex)){LastWarning="有一份关卡暂时打不开，原文件仍保留。";}
            }
            if(!recycled&&Directory.Exists(RecoveryFolder))foreach(string path in Directory.GetFiles(RecoveryFolder,"*.json"))
            {
                try
                {
                    var draft=Read(path);SafeId(draft.id);
                    if(!result.TryGetValue(draft.id,out var row)) result[draft.id]=new LibraryEntry{Document=draft,HasRecovery=true};
                    else if(draft.updatedUtc>row.Document.updatedUtc && Payload(draft)!=Payload(row.Document))row.HasRecovery=true;
                }
                catch(Exception ex)when(IsFileFailure(ex)){LastWarning="有一份自动草稿暂时打不开，原文件仍保留。";}
            }
            return result.Values.OrderByDescending(x=>x.Document.updatedUtc).ThenBy(x=>x.Document.name).ToList();
        }
        private static string Payload(LevelDocument d){var copy=d.Clone();copy.updatedUtc=0;return JsonUtility.ToJson(copy);}
        public LevelDocument Load(string id,bool recovery=false)
        {
            string path=recovery?RecoveryPath(id):SavedPath(id);
            try{return Read(path);}catch(Exception ex)when(IsFileFailure(ex)&&!recovery){return Read(path+".bak");}
        }
        public LevelDocument LatestDocument(LibraryEntry entry)
        {if(entry==null)throw new ArgumentNullException(nameof(entry));return entry.HasRecovery?Load(entry.Document.id,true):entry.Document.Clone();}
        public void Save(LevelDocument document)
        {
            LevelValidation.CheckEnvelope(document);SafeId(document.id);
            var copy=document.Clone();copy.updatedUtc=DateTime.UtcNow.Ticks;
            string path=SavedPath(copy.id);bool preserveBackup=false;
            if(File.Exists(path))try{Read(path);}catch(Exception ex)when(IsFileFailure(ex))
            {File.Copy(path,path+".invalid-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));preserveBackup=true;}
            AtomicWrite(path,JsonUtility.ToJson(copy,true),preserveBackup);document.updatedUtc=copy.updatedUtc;
            // The recovery is no longer needed only after the durable replacement succeeds.
            RemoveRecovery(document.id);
        }
        public void AutoSave(LevelDocument document)
        {LevelValidation.CheckEnvelope(document);var copy=document.Clone();copy.updatedUtc=DateTime.UtcNow.Ticks;AtomicWrite(RecoveryPath(copy.id),JsonUtility.ToJson(copy,true));}
        public void RemoveRecovery(string id)
        {
            string path=RecoveryPath(id);
            if(File.Exists(path))File.Delete(path);
            if(File.Exists(path+".bak"))File.Delete(path+".bak");
        }
        public LevelDocument Duplicate(LevelDocument source)
        {var result=source.Clone();result.id=Guid.NewGuid().ToString("N");result.name=ShortName(source.name+" · 副本");result.updatedUtc=0;Save(result);return result;}
        public void Recycle(string id)
        {
            var rows=List();var item=rows.FirstOrDefault(x=>x.Document.id==id);if(item==null)return;
            var d=item.HasRecovery?Load(id,true):item.Document;
            AtomicWrite(Path.Combine(RecycleFolder,SafeId(id)+".json"),JsonUtility.ToJson(d,true));
            string path=SavedPath(id);if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");RemoveRecovery(id);
        }
        public LevelDocument Restore(string id)
        {
            string path=Path.Combine(RecycleFolder,SafeId(id)+".json");var d=Read(path);
            if(File.Exists(SavedPath(id)))d.id=Guid.NewGuid().ToString("N");
            Save(d);File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");return d;
        }
        public LevelDocument ReadImport(string path)=>Parse(FileText(path),Path.GetFileNameWithoutExtension(path));
        public LevelDocument Import(LevelDocument preview)
        {
            var d=preview.Clone();LevelValidation.CheckEnvelope(d);d.id=Guid.NewGuid().ToString("N");d.updatedUtc=0;
            // A third-party completion claim is not a locally observed play-through.
            d.solvedHash="";d.solution="";d.rulesVersion="";
            var names=new HashSet<string>(List().Select(e=>e.Document.name));string original=d.name;int n=2;
            while(names.Contains(d.name))d.name=ShortName(original+" ("+(n++)+")");Save(d);return d;
        }
        public string Export(LevelDocument document,string folder=null,bool raw=false)
        {
            LevelValidation.CheckEnvelope(document);folder=Path.GetFullPath(folder??ExportFolder);Directory.CreateDirectory(folder);
            string name=string.Join("_",document.name.Split(Path.GetInvalidFileNameChars())).Trim('.',' ');if(name.Length==0)name="关卡";
            string path=Path.Combine(folder,name+(raw?"-布局":"")+".json");int n=2;
            while(File.Exists(path))path=Path.Combine(folder,name+"-"+(n++)+".json");
            AtomicWrite(path,raw?JsonUtility.ToJson(document.level,true):JsonUtility.ToJson(document,true));return path;
        }
        public static LevelDocument Parse(string json,string fallback="导入的关卡")
        {
            if(string.IsNullOrWhiteSpace(json)||Encoding.UTF8.GetByteCount(json)>MaxFileBytes)throw new ArgumentException("关卡文件为空或太大。");
            LevelDocument d;
            try
            {
                // Detect by parsed fields rather than a substring inside a title.
                d=JsonUtility.FromJson<LevelDocument>(json);
                if(d==null)throw new ArgumentException("没有读到关卡内容。");
                if(d.level==null)
                {
                    var layout=JsonUtility.FromJson<LevelData>(json);
                    d=new LevelDocument{id=Guid.NewGuid().ToString("N"),name=ShortName(fallback),level=layout};
                }
            }
            catch(ArgumentException){throw new ArgumentException("这份文件不是可读取的关卡。");}
            LevelValidation.CheckEnvelope(d);return d;
        }
        private static LevelDocument Read(string path)=>Parse(FileText(path));
        private static string FileText(string path)
        {var info=new FileInfo(path);if(info.Length>MaxFileBytes)throw new ArgumentException("文件太大了，请选择关卡文件。");return File.ReadAllText(path,Encoding.UTF8);}
        public static void AtomicWrite(string path,string text,bool preserveBackup=false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+".tmp-"+Guid.NewGuid().ToString("N");
            try
            {
                byte[] bytes=Encoding.UTF8.GetBytes(text);
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path))File.Replace(temporary,path,preserveBackup?null:path+".bak");else File.Move(temporary,path);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static bool IsFileFailure(Exception ex)=>ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException||ex is System.Security.SecurityException;
        private static string ShortName(string value)=>string.IsNullOrWhiteSpace(value)?"导入的关卡":value.Substring(0,Math.Min(60,value.Length));
    }
}
