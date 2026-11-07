using BoxOrVoid.Core;
using UnityEngine;

namespace BoxOrVoid.Study
{
    /// <summary>Presentation only: cross-fade, vignette and the second chapter's drifting outlines.</summary>
    public sealed class StudyBackdrop:MonoBehaviour
    {
        private StudyAssets assets;
        private Camera view;
        private Material material;
        private SpriteRenderer front,back,vignette;
        private readonly SpriteRenderer[] particles=new SpriteRenderer[15];
        private readonly Vector2[] velocity=new Vector2[15];
        private readonly float[] phases=new float[15],scales=new float[15];
        private Tone tone;
        private int chapter;
        private float fade=1,priorSize,priorAspect;

        public void Initialize(StudyAssets source,Camera camera,Material shared)
        {
            assets=source;view=camera;material=shared;
            back=Create("Background previous",-101);front=Create("Background",-100);
            vignette=Create("Soft vignette",-90);vignette.sprite=assets.Sprite("vignette");vignette.color=new Color(1,1,1,.6f);
            var random=new System.Random(1623);
            for(int i=0;i<particles.Length;i++)
            {
                particles[i]=Create("Drifting outline "+i,-95);particles[i].sprite=assets.Sprite("Particle_Box_Uncolored");
                phases[i]=(float)random.NextDouble()*Mathf.PI*2;scales[i]=.5f+(float)random.NextDouble()*1.5f;
                velocity[i]=new Vector2(Mathf.Cos(phases[i]),Mathf.Sin(phases[i]))*.5f;
            }
        }
        public void Show(int chapterIndex,Tone active)
        {
            chapter=chapterIndex;tone=active;fade=1;
            vignette.enabled=true;
            front.sprite=assets.ThemeSprite(chapter,tone==Tone.White?"whiteBackground":"blackBackground");back.sprite=front.sprite;
            for(int i=0;i<particles.Length;i++) particles[i].enabled=chapter==1;
            Fit(true);
        }
        public void ShowMenu(int chapterIndex,string backgroundName)
        {
            Show(chapterIndex,Tone.Black);
            front.sprite=back.sprite=assets.Sprite(backgroundName);vignette.enabled=false;Fit(true);
        }
        public void SetTone(Tone active)
        {
            if(active==tone) return;
            back.sprite=front.sprite;tone=active;
            front.sprite=assets.ThemeSprite(chapter,tone==Tone.White?"whiteBackground":"blackBackground");fade=0;Fit(false);
        }
        private void Update()
        {
            fade=Mathf.Min(1,fade+Time.unscaledDeltaTime/.1f);front.color=new Color(1,1,1,fade);
            if(view.orthographicSize!=priorSize || view.aspect!=priorAspect) Fit(false);
            if(chapter!=1) return;
            float halfH=view.orthographicSize+1,halfW=view.orthographicSize*view.aspect+1;
            for(int i=0;i<particles.Length;i++)
            {
                var p=particles[i];float phase=Time.unscaledTime*.25f+phases[i];
                var position=p.transform.localPosition+(Vector3)velocity[i]*Time.unscaledDeltaTime;
                if(position.x>halfW) position.x=-halfW;if(position.x< -halfW) position.x=halfW;
                if(position.y>halfH) position.y=-halfH;if(position.y< -halfH) position.y=halfH;
                p.transform.localPosition=position;
                float scale=scales[i]*(.85f+.15f*Mathf.Sin(phase*.4f))*view.orthographicSize/5;
                p.transform.localScale=Vector3.one*scale;
                Color color=tone==Tone.White?new Color(.5f,.7526f,1):new Color(.2863f,.2196f,.2353f);
                color.a=.4f+.6f*(.5f+.5f*Mathf.Sin(phase));p.color=color;
            }
        }
        private void Fit(bool resetParticles)
        {
            priorSize=view.orthographicSize;priorAspect=view.aspect;
            foreach(var renderer in new[]{front,back,vignette})
                if(renderer.sprite!=null) renderer.transform.localScale=new Vector3(priorSize*2*priorAspect/renderer.sprite.bounds.size.x,priorSize*2/renderer.sprite.bounds.size.y,1);
            if(!resetParticles) return;
            for(int i=0;i<particles.Length;i++)
                particles[i].transform.localPosition=new Vector3(((i%5+.5f)/5*2-1)*priorSize*priorAspect,((i/5+.5f)/3*2-1)*priorSize,0);
        }
        private SpriteRenderer Create(string label,int order)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            var sprite=go.AddComponent<SpriteRenderer>();sprite.sharedMaterial=material;sprite.sortingOrder=order;return sprite;
        }
    }
}
