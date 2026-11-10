using System;
using TMPro;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class DraculaApp
    {
        public void ShowMusicCredits()
        {
            if(Page!=GamePage.Settings||Transitioning)return;
            Page=GamePage.Confirming;input.Clear();
            void BackToSettings(){Page=GamePage.Settings;ui.Settings();ui.FocusButton("music-credits");input.Clear();}
            confirmationCancel=BackToSettings;ui.MusicCredits(BackToSettings);
        }
    }
    public sealed partial class GameUI
    {
        public void MusicCredits(Action back)
        {
            Dim("音乐署名");
            Text("Main music","Hypnotic Jewels",new Vector2(0,140),new Vector2(1000,42),28);
            Text("Main artist","Eric Matyas · soundimage.org",new Vector2(0,99),new Vector2(1000,34),22,Quiet);
            MenuAction("music-source-main","作品来源",new Vector2(0,54),230,()=>Application.OpenURL("https://opengameart.org/content/hypnotic-jewels-looping"));
            Text("Editor music","Puzzle Tune 1 — B",new Vector2(0,-13),new Vector2(1000,42),28);
            Text("Editor artist","rezoner · OpenGameArt.org",new Vector2(0,-54),new Vector2(1000,34),22,Quiet);
            MenuAction("music-source-editor","作品来源",new Vector2(0,-99),230,()=>Application.OpenURL("https://opengameart.org/content/puzzle-tune-1"));
            MenuAction("music-license","CC BY 3.0",new Vector2(0,-201),230,()=>Application.OpenURL("https://creativecommons.org/licenses/by/3.0/"));
            MenuAction("music-credits-back","返回",new Vector2(0,-279),230,back,GameIcon.Back);Focus(rows.Count-1);
        }
    }
}
