using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private SaveCelebrationGraphic saveCelebration;
        private double lastSaveCelebration=-10;
        public bool CelebrateSave(string buttonId="save")
        {
            var button=FindButton(buttonId);
            if(button==null&&buttonId=="save")button=FindButton("save-leave");
            if(button==null||!button.gameObject.activeInHierarchy)return false;
            bool active=saveCelebration!=null&&saveCelebration.isActiveAndEnabled;
            if(active&&Time.realtimeSinceStartupAsDouble-lastSaveCelebration<.45)return false;
            if(active){saveCelebration.gameObject.SetActive(false);Destroy(saveCelebration.gameObject);}
            var target=(RectTransform)button.transform;
            var center=(Vector2)content.InverseTransformPoint(target.TransformPoint(target.rect.center));
            var root=Rect("Save celebration",center,new Vector2(420,260));
            saveCelebration=root.gameObject.AddComponent<SaveCelebrationGraphic>();
            saveCelebration.Initialize(Mathf.Min(target.rect.width*.34f,58));
            lastSaveCelebration=Time.realtimeSinceStartupAsDouble;button.GetComponent<JuicyButton>()?.Pulse();
            app.Cue("levelSelect");return true;
        }
    }
}
