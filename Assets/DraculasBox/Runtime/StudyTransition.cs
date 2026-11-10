using System;
using UnityEngine;

namespace DraculasBox.Runtime
{
    /// <summary>Page changes happen behind a fully closed iris. A second request cannot replace an in-flight change.</summary>
    public sealed class StudyTransition:MonoBehaviour
    {
        [SerializeField] private StudyIrisGraphic iris;
        [SerializeField,Min(.05f)] private float closeSeconds=.3f,openSeconds=.3f;
        private enum Phase {Idle,Closing,Covered,Opening}
        private Phase phase;
        private float elapsed;
        private int coveredFrame;
        private Action change,finished;
        public bool IsBusy=>phase!=Phase.Idle;
        public float Aperture=>iris==null?1.1f:iris.Aperture;
        public void Configure(StudyIrisGraphic graphic) {iris=graphic;iris.enabled=false;iris.raycastTarget=false;}
        public void SetTiming(float close,float open){closeSeconds=close;openSeconds=open;}
        public bool Change(Action action,Action onFinished)
        {
            if(IsBusy || action==null) return false;
            change=action;finished=onFinished;phase=Phase.Closing;elapsed=0;
            iris.enabled=true;iris.raycastTarget=true;iris.Aperture=1.1f;return true;
        }
        public void Reveal(Action onFinished)
        {
            if(IsBusy) return;
            finished=onFinished;phase=Phase.Opening;elapsed=0;iris.enabled=true;iris.raycastTarget=true;iris.Aperture=0;
        }
        private void Update()
        {
            if(phase==Phase.Idle) return;
            elapsed+=Time.unscaledDeltaTime;
            if(phase==Phase.Closing)
            {
                iris.Aperture=1.1f*(1-Ease(elapsed/closeSeconds));
                if(elapsed>=closeSeconds) {iris.Aperture=0;phase=Phase.Covered;coveredFrame=Time.frameCount;}
            }
            else if(phase==Phase.Covered && Time.frameCount>coveredFrame)
            {
                var action=change;change=null;
                try {action?.Invoke();}
                catch(Exception error) {Debug.LogException(error);Finish();return;}
                phase=Phase.Opening;elapsed=0;
            }
            else if(phase==Phase.Opening)
            {
                iris.Aperture=1.1f*Ease(elapsed/openSeconds);
                if(elapsed>=openSeconds) Finish();
            }
        }
        private static float Ease(float value)
        {float t=Mathf.Clamp01(value);return t<.5f?4*t*t*t:1-Mathf.Pow(-2*t+2,3)/2;}
        private void Finish()
        {
            phase=Phase.Idle;change=null;iris.Aperture=1.1f;iris.enabled=false;iris.raycastTarget=false;
            var callback=finished;finished=null;callback?.Invoke();
        }
        private void OnDisable()
        {phase=Phase.Idle;change=null;finished=null;if(iris!=null) {iris.enabled=false;iris.raycastTarget=false;}}
    }
}
