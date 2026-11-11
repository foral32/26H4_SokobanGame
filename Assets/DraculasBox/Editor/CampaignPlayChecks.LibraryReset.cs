using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DraculasBox.Editor
{
    public static partial class CampaignPlayChecks
    {
        public static void RunLibraryReset()=>Begin(LibraryReset(false),"library-reset",true);
        public static void RunProgressSettings()=>Begin(LibraryReset(true),"progress-settings",true);
        private static IEnumerator LibraryReset(bool progressOnly)
        {
            CreateFixture();Bind(fixture);var catalog=fixture.Catalog;
            var first=catalog.chapters[0];var second=catalog.chapters[1];
            foreach(var level in first.levels.Where(l=>l.main))app.Progress.Complete(level.id);
            app.Progress.RememberUnlocks(catalog);app.Progress.Data.music=.37f;app.Progress.Data.effects=.44f;app.Progress.Save();
            var custom=LevelData.Blank();custom.title="保留的创作";app.Library.Save(custom);
            string customPath=Path.Combine(app.Library.Folder,custom.id+".json"),customBytes=File.ReadAllText(customPath);
            string beforeProgress=JsonUtility.ToJson(app.Progress.Data);
            if(!progressOnly)
            {
            yield return Click("creator");yield return Click("template");
            Need(app.Page==GamePage.Confirming&&app.UI.Root.Find("Campaign copy dialog")!=null,"copy opens a chooser without opening an arbitrary board");
            Need(!app.UI.FindButton("copy-confirm").interactable,"copy requires an explicit choice");
            yield return Click("copy-cancel");Need(app.Page==GamePage.Library&&app.Library.Read(out _).Count==1,"cancel copy creates no draft file");
            yield return Click("template");yield return Click("copy-chapter-"+second.id);yield return new WaitForSecondsRealtime(.18f);Capture("copy_picker");
            yield return Click("copy-next");yield return Click("copy-level-0");
            var source=fixture.Level(second.levels[6].id);Need(!second.levels[6].main,"fixture covers optional level copying");
            yield return Click("copy-confirm");
            Need(app.Page==GamePage.Editing&&app.Creator.Document.Data.title==source.title+" · 副本","copy uses the selected optional level");
            Need(app.Creator.Document.Data.id!=source.id&&app.Creator.Document.Data.rows.SequenceEqual(source.rows),"copied board has independent identity and matching layout");
            Need(app.Creator.Document.Data.solution==JsonUtility.FromJson<ReplayCatalog>(fixture.replays.text).levels.First(p=>p.id==source.id).route,"copy keeps the chosen board's replay");
            yield return Click("leave-editor");yield return Click("discard-leave");yield return Ready();
            Need(JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"choosing and copying do not alter campaign progress");

            app.ShowTitle();yield return Ready();yield return Click("campaign");yield return Click("select-chapter-"+second.id);
            var node=app.UI.FindButton("select-level-"+second.levels[1].id);app.UI.HoverForCheck(node.name);
            app.SetTestKeyboard(new HeldKey{key=KeyCode.Space});yield return new WaitForSecondsRealtime(.20f);
            var peek=app.UI.Root.Find("Level peek");
            Need(peek!=null&&peek.Find("Peek outline")==null,"preview uses a soft card without the hard outline");
            Need(peek.Find("Peek title").GetComponent<TMP_Text>().alignment==TextAlignmentOptions.Center,"preview title is centered beneath the board");
            Need(peek.GetComponent<CanvasGroup>().blocksRaycasts==false,"preview remains transparent to pointer input");Capture("soft_level_peek");
            app.SetTestKeyboard(driver);yield return null;yield return null;
            Need(app.UI.Root.Find("Level peek")==null,"soft preview still closes on Space release");
            }
            app.ShowTitle();yield return Ready();yield return Click("settings");
            var clear=app.UI.FindButton("reset-progress");var unlock=app.UI.FindButton("unlock-progress");
            Need(clear!=null&&unlock!=null,"main-menu settings expose both progress actions");
            Need(((RectTransform)clear.transform).anchoredPosition.y>((RectTransform)unlock.transform).anchoredPosition.y,"unlock is below clear in the settings layout");
            var clearLabel=clear.GetComponentInChildren<TMP_Text>();var unlockLabel=unlock.GetComponentInChildren<TMP_Text>();
            app.UI.HoverForCheck("reset-progress");yield return null;app.UI.HoverForCheck("unlock-progress");yield return null;
            Need(clearLabel.text=="清除关卡进度"&&unlockLabel.text=="解锁所有关卡","hover leaves both action labels unchanged");
            yield return Click("unlock-progress");
            Need(app.Page==GamePage.Settings&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"clicking unlock only selects it and performs no action");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            var clearHold=clear.GetComponent<HoldConfirmation>();var unlockHold=unlock.GetComponent<HoldConfirmation>();
            Need(clear.transform.Find("Hold progress")==null&&unlock.transform.Find("Hold progress")==null,"hold actions use text feedback without progress circles");
            ExecuteEvents.Execute(unlock.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.65f);
            Need(unlockHold.Amount>0&&unlockHold.Amount<1&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"partial unlock hold advances without changing progress");Capture("text_unlock_holding");
            ExecuteEvents.Execute(unlock.gameObject,pointer,ExecuteEvents.pointerUpHandler);yield return null;
            Need(unlockHold.Amount==0&&unlockLabel.text=="解锁所有关卡","releasing unlock cancels confirmation and keeps its label");
            ExecuteEvents.Execute(unlock.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.2f);
            ExecuteEvents.Execute(unlock.gameObject,pointer,ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(1.5f);
            Need(unlockHold.Amount==0&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"leaving unlock cancels the pending action");
            app.UI.FocusButton("unlock-progress");app.SetTestKeyboard(new HeldKey{key=KeyCode.Return});yield return new WaitForSecondsRealtime(.2f);app.SetTestKeyboard(driver);yield return null;
            Need(unlockHold.Amount==0&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"short keyboard hold does not unlock");
            string progressPath=Path.Combine(Directory.GetParent(app.Library.Folder).FullName,"progress.json");
            using(var locked=new FileStream(progressPath,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                app.SetTestKeyboard(new HeldKey{key=KeyCode.Return});yield return new WaitForSecondsRealtime(HoldConfirmation.Duration+.1f);app.SetTestKeyboard(driver);yield return null;
                Need(!app.Progress.Data.allLevelsUnlocked&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress&&unlockLabel.text=="写入失败","failed unlock reports the write error and preserves progress");
                yield return Ready();
            }
            Need(unlockLabel.text=="解锁所有关卡","unlock restores its normal label after an error");
            app.SetTestKeyboard(new HeldKey{key=KeyCode.Return});yield return new WaitForSecondsRealtime(HoldConfirmation.Duration+.08f);
            Need(app.UI.IsResettingProgress&&app.Progress.Data.allLevelsUnlocked&&unlockLabel.text=="解锁所有关卡","successful unlock commits with no label replacement");Capture("text_unlock_complete");
            app.SetTestKeyboard(driver);yield return Ready();
            Need(catalog.chapters.All(c=>app.Progress.ChapterUnlocked(c,catalog)&&c.levels.All(app.Progress.Unlocked)),"full unlock hold opens every chapter and both main and optional levels");
            Need(app.Progress.Data.completed.SequenceEqual(first.levels.Where(l=>l.main).Select(l=>l.id)),"unlock does not mark unplayed levels complete");
            var unlockedDisk=new ProgressStore(Directory.GetParent(app.Library.Folder).FullName);
            Need(unlockedDisk.Data.allLevelsUnlocked&&catalog.chapters.SelectMany(c=>c.levels).All(unlockedDisk.Unlocked),"all-level unlock survives reload");
            Need(unlockHold.Amount==0,"unlock hold resets after completing");
            beforeProgress=JsonUtility.ToJson(app.Progress.Data);
            yield return Click("reset-progress");
            Need(app.Page==GamePage.Settings&&app.UI.Root.Find("Reset progress dialog")==null,"clear remains on the existing settings page without a modal");
            ExecuteEvents.Execute(clear.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.65f);
            Need(clearHold.Amount>0&&clearHold.Amount<1&&clearLabel.text=="清除关卡进度"&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"clear hold advances with unchanged text");Capture("text_clear_holding");
            ExecuteEvents.Execute(clear.gameObject,pointer,ExecuteEvents.pointerUpHandler);yield return null;
            Need(clearHold.Amount==0&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"short clear hold cancels without changing progress");
            ExecuteEvents.Execute(clear.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.2f);
            ExecuteEvents.Execute(clear.gameObject,pointer,ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(1.5f);
            Need(clearHold.Amount==0&&JsonUtility.ToJson(app.Progress.Data)==beforeProgress,"leaving clear cancels the pending action");
            app.UI.FocusButton("reset-progress");
            using(var locked=new FileStream(progressPath,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                app.SetTestKeyboard(new HeldKey{key=KeyCode.Return});yield return new WaitForSecondsRealtime(HoldConfirmation.Duration+.1f);app.SetTestKeyboard(driver);yield return null;
                Need(JsonUtility.ToJson(app.Progress.Data)==beforeProgress&&clearLabel.text=="写入失败","failed clear preserves live progress and exposes the write error");yield return Ready();
            }
            ExecuteEvents.Execute(clear.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            float deadline=Time.realtimeSinceStartup+3;
            while(!app.UI.IsResettingProgress){NeedTime(deadline,"long press did not commit");yield return null;}
            Need(app.UI.IsResettingProgress&&app.Progress.Data.completed.Count==0&&clearLabel.text=="清除关卡进度"&&app.Page==GamePage.Settings,"completed clear keeps its action name");Capture("text_clear_complete");
            ExecuteEvents.Execute(clear.gameObject,pointer,ExecuteEvents.pointerUpHandler);yield return Ready();
            Need(clearHold.Amount==0&&clearLabel.text=="清除关卡进度"&&unlockLabel.text=="解锁所有关卡","both labels remain fixed after completion and confirmation resets");Capture("text_settings");
            Need(app.Progress.Data.completed.Count==0&&!app.Progress.Data.allLevelsUnlocked,"clear removes completions and the all-level unlock");
            Need(app.Progress.Data.lastLevel==first.levels.First(l=>l.main).id&&app.ChapterId==first.id,"clear resets the resume position to the first campaign level");
            var last=catalog.chapters.Last().levels.Last();
            Need(!app.Progress.ChapterUnlocked(second,catalog)&&!app.Progress.Unlocked(last),"later chapters and their levels relock");
            Need(Mathf.Approximately(app.Progress.Data.music,.37f)&&Mathf.Approximately(app.Progress.Data.effects,.44f)&&app.Progress.Data.theme==GameThemes.Current.Id,"progress actions preserve sound and theme preferences");
            Need(File.ReadAllText(customPath)==customBytes,"progress actions preserve custom level files byte for byte");
            var disk=new ProgressStore(Directory.GetParent(app.Library.Folder).FullName);
            Need(disk.Data.completed.Count==0&&!disk.Data.allLevelsUnlocked&&!disk.ChapterUnlocked(second,catalog),"cleared progress survives reload");
            app.SetTestKeyboard(new HeldKey{key=KeyCode.Return});yield return new WaitForSecondsRealtime(.2f);
            yield return Press(KeyCode.Escape);app.SetTestKeyboard(driver);yield return Ready();yield return new WaitForSecondsRealtime(1.5f);
            Need(app.Page==GamePage.Title&&!app.UI.IsResettingProgress,"leaving settings cancels the hold and its presentation");
            app.LoadLevel(first.levels.First(l=>l.main).id);yield return Ready();app.Pause();yield return Ready();yield return Click("settings");
            Need(app.UI.FindButton("reset-progress")==null&&app.UI.FindButton("unlock-progress")==null,"paused-game settings expose neither progress action");
            app.ShowTitle();yield return Ready();Need(new CampaignProject(fixture).Revision==Get("start"),"all checks leave official level data untouched");
        }
        public static void RunProgressFeedback()=>Begin(ProgressFeedback(),"progress-feedback",true);
        private static IEnumerator ProgressFeedback()
        {
            CreateFixture();Bind(fixture);app.Progress.Complete(fixture.Catalog.chapters[0].levels[0].id);
            yield return Click("settings");yield return Click("unlock-progress");
            string path=Path.Combine(Directory.GetParent(app.Library.Folder).FullName,"progress.json");
            using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                app.SetTestKeyboard(new HeldKey{key=KeyCode.Return});yield return new WaitForSecondsRealtime(HoldConfirmation.Duration+.12f);app.SetTestKeyboard(driver);yield return null;
                Need(app.UI.Root.GetComponentsInChildren<SaveCelebrationGraphic>().Length==0&&!app.Progress.Data.allLevelsUnlocked,"failed write plays no completion effect");yield return Ready();
            }
            foreach(string id in new[]{"unlock-progress","reset-progress"})
            {
                app.UI.FocusButton(id);yield return new WaitForSecondsRealtime(.15f);
                var button=app.UI.FindButton(id);var hit=(RectTransform)button.transform;
                var label=button.GetComponentInChildren<TMP_Text>();var visual=label.rectTransform;
                var origin=visual.anchoredPosition;var hitPosition=hit.anchoredPosition;var hitSize=hit.sizeDelta;
                string saved=JsonUtility.ToJson(app.Progress.Data);
                Need(button.transform.Find("Hold progress")==null,"held "+id+" has no progress circle");
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(.2f);float earlyScale=visual.localScale.x;
                float earlyShake=0,deadline=Time.realtimeSinceStartup+.10f;var firstAnchor=TextAnchor(label);
                while(Time.realtimeSinceStartup<deadline){earlyShake=Mathf.Max(earlyShake,Vector2.Distance(firstAnchor,TextAnchor(label)));yield return null;}
                Capture(id=="unlock-progress"?"unlock_hold_early":"clear_hold_early");
                yield return new WaitForSecondsRealtime(.65f);
                float lateShake=0;deadline=Time.realtimeSinceStartup+.10f;firstAnchor=TextAnchor(label);
                while(Time.realtimeSinceStartup<deadline){lateShake=Mathf.Max(lateShake,Vector2.Distance(firstAnchor,TextAnchor(label)));yield return null;}
                Need(visual.localScale.x>earlyScale+.07f,"held "+id+" gradually enlarges its text");
                Need(lateShake>earlyShake+.15f,"held "+id+" progressively strengthens its small shake");
                Need(hit.anchoredPosition==hitPosition&&hit.sizeDelta==hitSize,"held "+id+" keeps its hit area stable");
                Need(label.text==(id=="unlock-progress"?"解锁所有关卡":"清除关卡进度")&&JsonUtility.ToJson(app.Progress.Data)==saved,"held "+id+" preserves its name and does not commit early");
                Capture(id=="unlock-progress"?"unlock_hold_late":"clear_hold_late");
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);yield return new WaitForSecondsRealtime(.32f);
                Need(Mathf.Abs(visual.localScale.x-1)<.001f&&Vector2.Distance(visual.anchoredPosition,origin)<.1f,"released "+id+" restores its text pose promptly");
                Need(app.UI.Root.GetComponentsInChildren<SaveCelebrationGraphic>().Length==0&&JsonUtility.ToJson(app.Progress.Data)==saved,"cancelled "+id+" creates no particles or save change");
                app.SetTestKeyboard(new HeldKey{key=KeyCode.Return});deadline=Time.realtimeSinceStartup+3;
                while(!app.UI.IsResettingProgress){NeedTime(deadline,"hold did not complete");yield return null;}
                app.SetTestKeyboard(driver);yield return new WaitForSecondsRealtime(.035f);
                Need(visual.localScale.x>1.08f,"completed "+id+" gives a visible text pop");
                var particles=app.UI.Root.GetComponentInChildren<SaveCelebrationGraphic>();
                Need(particles!=null&&!particles.raycastTarget,"completed "+id+" shows particles without blocking input");
                var bounds=label.textBounds;var center=app.UI.Root.InverseTransformPoint(visual.TransformPoint(bounds.center));
                var particleRect=(RectTransform)particles.transform;
                Need(Mathf.Abs(particleRect.anchoredPosition.x-center.x)<20&&particleRect.sizeDelta.x>label.preferredWidth,"completed "+id+" centers particles around the whole text");
                yield return new WaitForSecondsRealtime(.10f);Capture(id=="unlock-progress"?"unlock_feedback":"clear_feedback");
                yield return new WaitForSecondsRealtime(.65f);yield return Ready();
                Need(app.UI.Root.GetComponentsInChildren<SaveCelebrationGraphic>().Length==0&&Mathf.Abs(visual.localScale.x-1)<.001f,"completed "+id+" promptly removes particles and restores the text");
            }
            Need(app.Page==GamePage.Settings&&!app.Progress.Data.allLevelsUnlocked&&app.Progress.Data.completed.Count==0,"feedback finishes in settings with the committed progress state");
            app.ShowTitle();yield return Ready();
        }
        private static Vector2 TextAnchor(TMP_Text label)
        {
            var bounds=label.textBounds;
            return app.UI.Root.InverseTransformPoint(label.rectTransform.TransformPoint(new Vector3(bounds.min.x,bounds.center.y,0)));
        }
    }
}
