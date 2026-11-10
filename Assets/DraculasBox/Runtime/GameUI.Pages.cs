using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        public void Title()
        {
            Clear();
            if(authoredTitle!=null)
            {
                authoredTitle.SetActive(true);EnsureTitleLayout();ApplyTitleTheme();pageGroup=Group((RectTransform)authoredTitle.transform);
                Action[] actions={app.ShowChapters,app.OpenLibrary,app.Settings,app.Exit};
                for(int i=0;i<authoredTitleButtons.Length;i++)
                {
                    var button=authoredTitleButtons[i];var focus=button.transform.Find("Focus").GetComponent<Graphic>();
                    Register(button.name,button,actions[i],focus,button.transform.Find("Label").GetComponent<TMP_Text>());
                }
                CampaignTitleEntry(authoredTitle.transform);
                Focus(0);return;
            }
            Backdrop();
            content.GetComponentInChildren<MenuAtmosphere>().titleLayout=true;
            BuildTitleLogo(content);
            TitleAction("play","开始游戏",app.ShowChapters);
            TitleAction("creator","关卡创作",app.OpenLibrary);
            TitleAction("settings","设置",app.Settings);
            TitleAction("exit","退出",app.Exit);
            LayoutTitleButtons(rows.Select(r=>r.button).ToArray());
            CampaignTitleEntry(content);
            Focus(0);
        }
        public void Chapters()=>ChapterPage(Mathf.Max(0,Array.FindIndex(assets.Catalog.chapters,c=>c.id==app.ChapterId))/4);
        private void ChapterPage(int page)
        {
            Clear();Backdrop();Text("Heading","章节",new Vector2(0,278),new Vector2(900,70),42);
            var chapters=assets.Catalog.chapters;
            int start=page*4,visible=Math.Min(4,chapters.Length-start);
            for(int i=start;i<start+visible;i++)
            {
                var chapter=chapters[i];bool open=app.Progress.ChapterUnlocked(chapter,assets.Catalog);
                float x=(i-start-(visible-1)*.5f)*Mathf.Min(380,960f/Mathf.Max(1,visible-1));
                var row=TileAction("chapter-"+chapter.id,new Vector2(x,32),196,open?GameThemes.Current.NodeDone:GameThemes.Current.NodeLocked,()=>{if(open)app.ShowLevels(chapter.id);},open);
                var parent=row.button.transform;
                if(open)Image("Coffin","coffin-lid",Vector2.zero,new Vector2(123,143),Color.white,parent);
                else Icon("Lock",GameIcon.Lock,Vector2.zero,68,GameThemes.Current.Lock,parent);
                if(app.Progress.ChapterDone(chapter))Icon("Completed",GameIcon.Check,new Vector2(67,67),30,Gold,parent);
                int count=chapter.levels.Count(l=>l.main&&app.Progress.Done(l.id)),total=chapter.levels.Count(l=>l.main);
                Text("Chapter title",chapter.title,new Vector2(x,-128),new Vector2(360,54),27);
                Text("Chapter progress",count+" / "+total,new Vector2(x,-182),new Vector2(260,35),18,Quiet);
            }
            MenuAction("back","返回",new Vector2(-478,-278),160,app.ShowTitle,GameIcon.Back);
            MapPager(page,chapters.Length,4,ChapterPage);
            Focus(Mathf.Clamp(Array.FindIndex(chapters,c=>c.id==app.ChapterId)-start,0,visible-1));
        }
        public void Levels(Chapter chapter)
        {
            var mainCount=chapter.levels.Count(l=>l.main);
            if(mainCount>7||chapter.levels.Any(l=>!l.main&&l.branchAbove)||chapter.levels.Where(l=>!l.main).GroupBy(l=>l.requires?.FirstOrDefault()).Any(g=>g.Count()>1))
            {
                PlayableChapterMap(chapter);
                return;
            }
            Clear();Backdrop();Text("Heading",chapter.title,new Vector2(0,276),new Vector2(900,65),40);
            var main=chapter.levels.Where(l=>l.main).ToArray();int count=main.Count(l=>app.Progress.Done(l.id));
            Text("Chapter progress",count+" / "+main.Length,new Vector2(480,280),new Vector2(240,45),24,Quiet);
            var detail=Text("Selection detail","",new Vector2(0,-238),new Vector2(900,56),30);float step=Mathf.Min(156,936f/Mathf.Max(1,main.Length-1));
            for(int i=0;i<main.Length-1;i++)Image("Main path",null,new Vector2((i-(main.Length-1)*.5f)*step+step*.5f,62),new Vector2(step-114,5),GameThemes.Current.Path);
            foreach(var link in chapter.levels)
            {
                bool branch=!link.main;int index=branch?Array.FindIndex(main,l=>link.requires!=null&&link.requires.Contains(l.id)):Array.IndexOf(main,link);index=Mathf.Max(0,index);
                float x=(index-(main.Length-1)*.5f)*step,y=branch?-116:62;float size=branch?96:112;
                if(branch)Image("Branch path",null,new Vector2(x,-29),new Vector2(3,68),new Color32(127,67,78,255));
                var level=assets.Level(link.id);bool open=app.Progress.Unlocked(link),done=app.Progress.Done(link.id);Color face=done?GameThemes.Current.NodeDone:open?GameThemes.Current.NodeOpen:GameThemes.Current.NodeLocked;
                var row=TileAction("level-"+link.id,new Vector2(x,y),size,face,()=>{if(open)app.LoadLevel(link.id);},open);
                if(branch){var border=Rect("Branch frame",Vector2.zero,Vector2.one*(size+8),row.button.transform).gameObject.AddComponent<BranchFrameGraphic>();border.color=BranchFrameGraphic.Red;border.raycastTarget=false;}
                string number=(index+1)+(branch?"A":"");
                if(open)Label(Rect("Number",Vector2.zero,new Vector2(size-10,76),row.button.transform),number,branch?37:49,done?GameThemes.Current.NodeDoneInk:GameThemes.Current.NodeInk);else Icon("Lock",GameIcon.Lock,Vector2.zero,branch?42:54,GameThemes.Current.Lock,row.button.transform);
                if(done)Icon("Completed",GameIcon.Check,new Vector2(size*.33f,size*.33f),22,Gold,row.button.transform);
                row.focus=()=>detail.text=number+" / "+level.title;
            }
            MenuAction("back","返回章节",new Vector2(-462,-298),190,app.ShowChapters,GameIcon.Back);
            int current=Array.FindIndex(chapter.levels,l=>l.id==app.Progress.Data.lastLevel);Focus(Mathf.Max(0,current));
        }
        public void Playing(string title,bool creator)
        {
            Clear();Text("Level name",title,new Vector2(-320,319),new Vector2(590,45),27,White,TextAlignmentOptions.MidlineLeft);
            RoundAction("undo","撤销 Z",new Vector2(533,137),48,GameIcon.Undo,()=>app.RequestHistory(Command.Undo));
            RoundAction("reset","重置 R",new Vector2(533,24),48,GameIcon.Reset,()=>app.RequestHistory(Command.Reset));
            RoundAction("pause",creator?"返回 F5":"暂停 Esc",new Vector2(533,-89),48,creator?GameIcon.Back:GameIcon.Pause,()=>{if(creator)app.ReturnToEditor();else app.Pause();});
        }
        public void DeathHint(){MenuAction("recover","Z 撤销",new Vector2(-60,-291),154,()=>app.RequestHistory(Command.Undo),GameIcon.Undo);Focus(rows.Count-1);}
        private void Dim(string heading)
        {
            Clear();Image("Dim",null,Vector2.zero,new Vector2(3200,2000),SettingsShade).raycastTarget=true;
            Text("Heading",heading,new Vector2(0,231),new Vector2(1000,70),44,White);
        }
        public void Pause()
        {
            Dim("暂停");
            MenuAction("resume","继续",new Vector2(0,111),240,app.Resume,GameIcon.Play);
            MenuAction("retry","重置关卡",new Vector2(0,49),240,app.ResetFromPause,GameIcon.Reset);
            MenuAction("settings","设置",new Vector2(0,-13),240,app.Settings,GameIcon.Settings);
            MenuAction("levels","返回选关",new Vector2(0,-75),240,()=>app.ShowLevels(app.ChapterId),GameIcon.Back);
            MenuAction("home","主菜单",new Vector2(0,-137),240,app.ShowTitle,GameIcon.Exit);
            ThemeChoices(-199,true);
            Focus(0);
        }
        public void Settings()
        {
            Dim("设置");
            var music=MenuAction("music","",new Vector2(0,126),350,()=>app.AdjustVolume(0,.1f));musicValue=music.label;
            var effects=MenuAction("effects","",new Vector2(0,31),350,()=>app.AdjustVolume(1,.1f));effectsValue=effects.label;
            volumeMarks=new Image[2][];
            for(int channel=0;channel<2;channel++){volumeMarks[channel]=new Image[10];for(int i=0;i<10;i++)volumeMarks[channel][i]=Image("Volume "+channel+" "+i,null,new Vector2(-120+i*29,89-channel*95),new Vector2(23,4),Quiet);}
            ThemeChoices();
            MenuAction("back","返回",new Vector2(0,-204),220,app.CloseSettings,GameIcon.Back);
            MenuAction("music-credits","音乐署名",new Vector2(0,-279),220,app.ShowMusicCredits);
            RefreshSettings();Focus(0);
        }
        public void RefreshSettings()
        {
            if(musicValue==null)return;
            float[] values={app.Progress.Data.music,app.Progress.Data.effects};musicValue.text="音乐     "+Mathf.RoundToInt(values[0]*100)+"%";effectsValue.text="音效     "+Mathf.RoundToInt(values[1]*100)+"%";
            for(int channel=0;channel<2;channel++)for(int i=0;i<10;i++)volumeMarks[channel][i].color=i<Mathf.RoundToInt(values[channel]*10)?Gold:GameThemes.Current.Border;
        }
        public void ConfirmLeave(Action save,Action discard,Action cancel)
        {
            Dim("保存修改？");
            MenuAction("save-leave","保存并离开",new Vector2(0,40),310,save,GameIcon.Check);
            MenuAction("discard-leave","不保存，离开",new Vector2(0,-30),310,discard,GameIcon.Exit);
            MenuAction("cancel-leave","继续编辑",new Vector2(0,-100),310,cancel,GameIcon.Back);Focus(0);
        }
    }
}
