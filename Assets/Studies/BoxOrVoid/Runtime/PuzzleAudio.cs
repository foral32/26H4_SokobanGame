using UnityEngine;

namespace BoxOrVoid.Study
{
    public sealed class PuzzleAudio:MonoBehaviour
    {
        private AudioSource effects,music;
        private StudyAssets assets;
        private float musicVolume;
        public void Initialize(StudyAssets source)
        {
            assets=source;effects=gameObject.AddComponent<AudioSource>();music=gameObject.AddComponent<AudioSource>();
            effects.playOnAwake=music.playOnAwake=false;effects.spatialBlend=music.spatialBlend=0;
            music.loop=true;music.volume=0;
        }
        public void SetVolumes(float background,float sounds) {musicVolume=background;effects.volume=sounds;}
        public void PlayMusic(int chapter)
        {
            var clip=assets.Sound(chapter==1?"boxWorld":"theme");
            if(music.clip==clip) return;
            music.Stop();music.clip=clip;music.volume=0;if(clip!=null) music.Play();
        }
        public void Cue(string key,float pitch=1)
        { var clip=assets.Sound(key);if(clip==null) return;effects.pitch=pitch;effects.PlayOneShot(clip); }
        private void Update() { music.volume=Mathf.MoveTowards(music.volume,musicVolume,Time.unscaledDeltaTime); }
    }
}
