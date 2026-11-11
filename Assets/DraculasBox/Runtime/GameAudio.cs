using UnityEngine;

namespace DraculasBox.Runtime
{
    public enum MusicContext { Main,Editor }

    public sealed partial class GameAudio:MonoBehaviour
    {
        public const float CrossfadeSeconds=.9f;
        private GameAssets assets;
        private AudioSource main,editor,effects,burn;
        private float master,targetMaster,blend,from,to,elapsed,duration;
        private bool initialized;
        public MusicContext Context {get;private set;}

        private AudioSource Source(string name)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            source.dopplerLevel=0;source.volume=0;return source;
        }
        public void Initialize(GameAssets a)
        {
            if(main==null){main=Source("Main music");editor=Source("Editor music");effects=Source("Effects");}
            if(burn==null)burn=Source("Burn");
            if(initialized&&assets==a&&main.clip==a.mainMusic&&editor.clip==a.editorMusic)return;
            assets=a;main.Stop();editor.Stop();main.clip=a.mainMusic;editor.clip=a.editorMusic;
            BuildCircuitSounds();CancelCircuitReplies();
            main.loop=editor.loop=true;master=targetMaster=blend=from=to=elapsed=duration=0;
            Context=MusicContext.Main;initialized=true;ApplyVolumes();
            // Both loops share a DSP start and continue silently when not selected.
            // Returning to either context reveals its current playhead, not its intro.
            double at=AudioSettings.dspTime+.06;
            if(main.clip!=null)main.PlayScheduled(at);
            if(editor.clip!=null)editor.PlayScheduled(at);
        }
        public void Music(MusicContext context)
        {
            if(!initialized)return;
            // A missing optional editor track keeps the existing main music audible.
            if(context==MusicContext.Editor&&editor.clip==null)context=MusicContext.Main;
            if(context==MusicContext.Main&&main.clip==null&&editor.clip!=null)context=MusicContext.Editor;
            if(Context==context)return;
            Context=context;from=blend;to=context==MusicContext.Editor?1:0;elapsed=0;
            duration=Mathf.Max(.18f,CrossfadeSeconds*Mathf.Abs(to-from));
        }
        public void Volume(float music,float soundEffects)
        {
            targetMaster=Mathf.Clamp01(music);
            if(effects!=null)effects.volume=Mathf.Clamp01(soundEffects);
            if(burn!=null)burn.volume=Mathf.Clamp01(soundEffects);
        }
        private void Update()
        {
            if(!initialized)return;
            // Volume zero takes effect immediately; unmuting has a short ramp.
            master=targetMaster<=0?0:Mathf.MoveTowards(master,targetMaster,Time.unscaledDeltaTime*Mathf.Max(targetMaster,.01f)/.35f);
            if(duration>0)
            {
                elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);
                blend=Mathf.Lerp(from,to,t*t*(3-2*t));if(t>=1)duration=0;
            }
            ApplyVolumes();
            UpdateCircuitReplies();
        }
        private void ApplyVolumes()
        {
            if(main==null)return;
            // Equal-power gains keep the overlap from making a dip in loudness.
            main.volume=master*(blend>=1?0:Mathf.Cos(blend*Mathf.PI*.5f));
            editor.volume=master*(blend<=0?0:Mathf.Sin(blend*Mathf.PI*.5f));
        }
        private void OnEnable()
        {if(initialized){main.UnPause();editor.UnPause();}}
        private void OnDisable()
        {CancelCircuitReplies();if(initialized){main.Pause();editor.Pause();effects.Stop();burn.Stop();}}
        public void Cue(string key)
        {
            if(key=="undo"||key=="reset"||key=="IrisClose")burn?.Stop();
            var clip=assets?.Clip(key);if(clip==null)return;
            if(key=="burn"&&burn!=null){burn.Stop();burn.clip=clip;burn.pitch=1;burn.Play();return;}
            if(effects!=null){effects.pitch=1;effects.PlayOneShot(clip);}
        }
    }
}
