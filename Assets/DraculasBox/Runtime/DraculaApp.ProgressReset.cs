using System;
using System.Linq;

namespace DraculasBox.Runtime
{
    public sealed partial class DraculaApp
    {
        public bool MainMenuSettings=>settingsReturn==GamePage.Title;
        internal string UnlockProgress()
        {
            if(Page!=GamePage.Settings||!MainMenuSettings||Transitioning)return "解锁不可用";
            try{Progress.UnlockCampaign();input.Clear();return null;}
            catch(Exception){return "未能写入存档，请重试。";}
        }
        internal string ClearProgress()
        {
            if(Page!=GamePage.Settings||!MainMenuSettings||Transitioning)return "清除不可用";
            try
            {
                Progress.ClearCampaign(assets.Catalog);ChapterId=assets.Catalog.chapters.First(c=>c.levels.Any(l=>l.id==Progress.Data.lastLevel)).id;
                chapterJustCompleted=false;input.Clear();return null;
            }
            catch(Exception){return "未能写入存档，请重试。";}
        }
    }
}
