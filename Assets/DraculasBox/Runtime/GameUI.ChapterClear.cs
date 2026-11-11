using System.Collections;
using System.Linq;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private Coroutine chapterCelebration;
        private ChapterMapView playableMap;
        public bool IsChapterCelebrating {get;private set;}

        private void ChapterExit(Chapter chapter)
        {
            if(!app.Progress.ChapterDone(chapter))return;
            var chapters=assets.Catalog.chapters;int next=System.Array.FindIndex(chapters,c=>c.id==chapter.id)+1;
            if(next<=0||next>=chapters.Length||!app.Progress.ChapterUnlocked(chapters[next],assets.Catalog))return;
            MenuAction("next-chapter","下一章",new Vector2(467,-298),180,()=>app.ShowLevels(chapters[next].id));
        }
        public void CelebrateChapter(Chapter chapter)
        {
            StopChapterCelebration();IsChapterCelebrating=true;
            chapterCelebration=StartCoroutine(ChapterClearSequence(chapter));
        }
        private void StopChapterCelebration()
        {
            if(chapterCelebration!=null)StopCoroutine(chapterCelebration);
            chapterCelebration=null;IsChapterCelebrating=false;
        }
        private IEnumerator ChapterClearSequence(Chapter chapter)
        {
            var completed=chapter.levels.Where(l=>app.Progress.Done(l.id)).ToArray();
            var next=FindButton("next-chapter");if(next!=null)next.gameObject.SetActive(false);
            foreach(var link in completed)
            {
                var button=FindButton("level-"+link.id);if(button==null)continue;
                var check=button.transform.Find("Completed");if(check!=null)check.gameObject.SetActive(false);
                button.transform.Find("Tile").GetComponent<MenuTileGraphic>().color=GameThemes.Current.NodeOpen;
                var number=button.transform.Find("Number")?.GetComponent<TMPro.TMP_Text>();if(number!=null)number.color=GameThemes.Current.NodeInk;
            }
            while(app.transition.IsBusy||IsEntering)yield return null;
            yield return new WaitForSecondsRealtime(.10f);
            float interval=Mathf.Min(.18f,2f/Mathf.Max(1,completed.Length));
            foreach(var link in completed)
            {
                var button=FindButton("level-"+link.id);if(button==null)continue;
                playableMap?.Reveal(link.id);
                button.transform.Find("Tile").GetComponent<MenuTileGraphic>().color=GameThemes.Current.NodeDone;
                var number=button.transform.Find("Number")?.GetComponent<TMPro.TMP_Text>();if(number!=null)number.color=GameThemes.Current.NodeDoneInk;
                var check=button.transform.Find("Completed");if(check!=null)check.gameObject.SetActive(true);
                button.GetComponent<JuicyButton>()?.Conquer();
                var target=(RectTransform)button.transform;
                var center=(Vector2)content.InverseTransformPoint(target.TransformPoint(target.rect.center));
                var burst=Rect("Chapter clear particles",center,new Vector2(240,230)).gameObject.AddComponent<SaveCelebrationGraphic>();
                burst.Initialize(target.rect.width*.28f);app.Cue("levelSelect");
                yield return new WaitForSecondsRealtime(interval);
            }
            yield return new WaitForSecondsRealtime(.42f);
            if(next!=null){next.gameObject.SetActive(true);next.GetComponent<JuicyButton>()?.Conquer();app.Cue("uiSubmit");}
            chapterCelebration=null;IsChapterCelebrating=false;
        }
    }
}
