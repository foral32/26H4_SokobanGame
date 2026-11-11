using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private readonly List<HoldConfirmation> progressHolds=new List<HoldConfirmation>();
        private Coroutine progressFeedback;
        public bool IsResettingProgress {get;private set;}
        private void ClearProgressPresentation()
        {
            foreach(var hold in progressHolds)if(hold!=null)hold.Cancel();progressHolds.Clear();
            if(progressFeedback!=null)StopCoroutine(progressFeedback);progressFeedback=null;IsResettingProgress=false;
        }
        public bool ProcessHoldInput(IStudyKeyboard keys)
        {
            if(progressHolds.Count==0)return false;
            if(keys!=null&&keys.Pressed(KeyCode.Escape)){foreach(var hold in progressHolds)hold.Cancel();return false;}
            bool selected=false;
            foreach(var hold in progressHolds)
            {
                if(hold==null||!hold.isActiveAndEnabled)continue;
                bool current=SelectedId==hold.name;selected|=current;
                hold.Step(keys,current,keys!=null);if(IsResettingProgress)break;
            }
            return selected&&keys!=null&&(keys.Held(KeyCode.Return)||keys.Held(KeyCode.KeypadEnter)||keys.Held(KeyCode.Space));
        }
        private void ProgressResetEntry()=>ProgressHoldEntry("reset-progress","清除关卡进度",-126,app.ClearProgress);
        private void ProgressUnlockEntry()=>ProgressHoldEntry("unlock-progress","解锁所有关卡",-182,app.UnlockProgress);
        private void ProgressHoldEntry(string id,string title,float y,Func<string> commit)
        {
            var row=MenuAction(id,title,new Vector2(30,y),280,()=>{});
            row.motion.ScaleOnPress=false;
            var textMotion=row.label.gameObject.AddComponent<HoldTextMotion>();
            // Clicks only select. Both actions are committed exclusively by a completed hold.
            row.button.onClick.RemoveAllListeners();row.button.onClick.AddListener(()=>{if(!app.Transitioning)FocusButton(id);});
            var hold=row.button.gameObject.AddComponent<HoldConfirmation>();progressHolds.Add(hold);
            hold.Configure(()=>
            {
                bool success=commit()==null;IsResettingProgress=true;
                foreach(var entry in progressHolds){entry.Cancel();entry.enabled=false;}
                if(success){textMotion.Complete();CelebrateProgress(row,id=="reset-progress");app.Cue("reset");}
                else{textMotion.ResetMotion();row.label.text="写入失败";}
                progressFeedback=StartCoroutine(FinishProgressFeedback(row,title,success));
            },textMotion.SetProgress);
        }
        private void CelebrateProgress(Row row,bool clearing)
        {
            var visual=row.label.rectTransform;var bounds=row.label.textBounds;
            var low=(Vector2)content.InverseTransformPoint(visual.TransformPoint(bounds.min));
            var high=(Vector2)content.InverseTransformPoint(visual.TransformPoint(bounds.max));
            var size=high-low;
            var burst=Rect("Progress completion particles",(low+high)*.5f,new Vector2(size.x+140,140)).gameObject.AddComponent<SaveCelebrationGraphic>();
            burst.InitializeProgress(clearing,new Vector2(size.x*.5f+4,size.y*.5f+8));
        }
        private IEnumerator FinishProgressFeedback(Row row,string title,bool success)
        {
            yield return new WaitForSecondsRealtime(success?.28f:.85f);
            row.label.text=title;progressFeedback=null;IsResettingProgress=false;
            foreach(var hold in progressHolds){hold.Retry();hold.enabled=true;}
        }
    }
}
