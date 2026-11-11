using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private bool ownsDeathBackdrop;
        private Color deathBackdrop,appliedBackdrop;
        private const float DeathPause=2f/60f;
        private Material burnMaterial;
        private SpriteRenderer burnGlow;
        private Vector3 deathLight=Vector3.right;

        private void BuildDeathEffects()
        {
            if(editing){embers=System.Array.Empty<SpriteRenderer>();return;}
            if(burnMaterial==null)
            {
                var shader=Resources.Load<Shader>("Shaders/VampireBurn");
                if(shader==null||!shader.isSupported)throw new System.InvalidOperationException("Vampire burn shader is unavailable.");
                burnMaterial=new Material(shader){name="Vampire burn",hideFlags=HideFlags.DontSave};
            }
            embers=new SpriteRenderer[20];
            for(int i=0;i<embers.Length;i++){embers[i]=MakeSprite("Burn ash "+i,SurfaceSprites.BurnAsh,20,Vector3.zero,.04f);embers[i].enabled=false;}
            burnGlow=MakeSprite("Burn contact glow",SurfaceSprites.Glow,10,Vector3.zero,1.2f);burnGlow.enabled=false;
        }
        private void ConfigureDeathLight(LightField field)
        {
            if(!state.Dead)return;
            var d=state.Level;int at=state.Player.y*d.Width+state.Player.x;
            if(field.Sun[at]){var sun=LightField.SunTravel[(int)state.SunFrom];deathLight=new Vector3(sun.x,-sun.y,0);return;}
            int nearest=int.MaxValue;deathLight=Vector3.up;
            // Use the final ray segment, including a mirror's outgoing arm.
            for(int direction=0;direction<4;direction++)
            {
                var delta=LightField.Directions[direction];var p=state.Player;
                for(int n=1;n<d.Width+d.Height;n++)
                {
                    p+=delta;if(!d.Inside(p)||d.Terrain(p)=='#'||state.ClosedDoor(p))break;
                    int piece=state.At(p);if(piece<0)continue;
                    bool source=d.Kind(piece)==PieceKind.Lamp;
                    if(d.Kind(piece)==PieceKind.MirrorSlash||d.Kind(piece)==PieceKind.MirrorBackslash)source=(field.ReflectionEdges[p.y*d.Width+p.x]&(1<<((direction+2)%4)))!=0;
                    if(source&&n<nearest){nearest=n;deathLight=new Vector3(-delta.x,delta.y,0);}break;
                }
            }
        }
        private void RestoreDeathInk()
        {
            if(player==null)return;
            if(player.sharedMaterial!=assets.material){player.sharedMaterial=assets.material;SetSprite(player,player.sprite,true);}
            if(cape.sharedMaterial!=assets.material){cape.sharedMaterial=assets.material;SetSprite(cape,cape.sprite,true);}
            if(fangs.sharedMaterial!=assets.material){fangs.sharedMaterial=assets.material;SetSprite(fangs,fangs.sprite,true);}
        }
        private void ApplyBurnInk(SpriteRenderer renderer)
        {
            renderer.sharedMaterial=burnMaterial;var bounds=renderer.sprite.bounds;
            spriteProperties.Clear();spriteProperties.SetTexture(MainTexture,renderer.sprite.texture);
            spriteProperties.SetVector("_BurnRect",new Vector4(bounds.center.x,bounds.center.y,bounds.size.x,bounds.size.y));renderer.SetPropertyBlock(spriteProperties);
        }

        private void DeathFrame(out Vector3 offset,out float scale)
        {
            offset=Vector3.zero;scale=1;
            float q=!editing&&state!=null&&state.Dead&&deathAt>=0?Time.unscaledTime-deathAt-DeathImpactSeconds:-1;
            if(q<0){ResetDeathBackdrop();return;}
            float z=q<.055f?1-Mathf.Pow(1-Mathf.Clamp01(q/.055f),3):1-Mathf.SmoothStep(0,1,(q-.055f)/.30f);
            scale=1+.018f*z;
            float u=q-DeathPause;
            if(u>=0&&u<.15f){float decay=Mathf.Pow(1-u/.15f,2);offset=new Vector3(Mathf.Cos(u*83),Mathf.Sin(u*68)*.54f,0)*(1.8f*unitsPerUI*decay);}
            offset-=Position(state.Player)*.35f*(scale-1);
            if(q<.15f)
            {
                if(!ownsDeathBackdrop){deathBackdrop=cameraView.backgroundColor;ownsDeathBackdrop=true;}
                float pulse=(7f/255f)*Mathf.Sin(Mathf.PI*Mathf.Clamp01(q/.10f))*Mathf.Max(0,1-q/.15f);
                appliedBackdrop=deathBackdrop+new Color(pulse,pulse,pulse,0);cameraView.backgroundColor=appliedBackdrop;
            }
            else ResetDeathBackdrop();
        }
        private void ResetDeathBackdrop()
        {
            if(!ownsDeathBackdrop)return;
            if(cameraView!=null&&cameraView.backgroundColor==appliedBackdrop)cameraView.backgroundColor=deathBackdrop;
            ownsDeathBackdrop=false;
        }
        private void OnDisable(){ResetDeathBackdrop();}
        private void DeathPose()
        {
            float u=Time.unscaledTime-deathAt-DeathImpactSeconds;
            if(u<0){RestoreDeathInk();return;}
            float flash=Mathf.Sin(Mathf.Clamp01(u/.12f)*Mathf.PI)*Mathf.Exp(-u*4);
            float heat=Mathf.SmoothStep(0,1,(u-.035f)/.18f);
            float dissolve=Mathf.Lerp(-.05f,1.02f,Mathf.SmoothStep(0,1,(u-.105f)/.335f));
            burnMaterial.SetFloat("_Heat",heat);burnMaterial.SetFloat("_Dissolve",dissolve);burnMaterial.SetFloat("_Flash",flash);
            burnMaterial.SetVector("_Direction",new Vector4(deathLight.x,deathLight.y,0,0));
            ApplyBurnInk(player);ApplyBurnInk(cape);ApplyBurnInk(fangs);
            var recoil=-deathLight*(Mathf.Sin(Mathf.Clamp01(u/.18f)*Mathf.PI)*.045f);
            player.transform.localPosition+=recoil;fangs.transform.localPosition+=recoil;cape.transform.localPosition+=recoil;
            float flinch=Mathf.Sin(Mathf.Clamp01(u/.16f)*Mathf.PI)*.08f;
            player.transform.localScale=Vector3.Scale(player.transform.localScale,new Vector3(1+flinch,1-flinch,1));fangs.transform.localScale=player.transform.localScale;
            var capeScale=cape.transform.localScale;capeScale.y*=1-heat*.18f;cape.transform.localScale=capeScale;cape.transform.localPosition+=Vector3.down*(heat*.035f);
            burnGlow.enabled=u<.20f;burnGlow.transform.localPosition=Position(state.Player);
            burnGlow.color=new Color(1,.85f,.53f,flash*.42f);burnGlow.transform.localScale=Vector3.one*(1.1f+u*1.2f);
            for(int i=0;i<embers.Length;i++)
            {
                var bit=embers[i];float start=.075f+(i%7)*.021f,age=u-start;
                bit.enabled=age>=0&&age<.43f;
                if(!bit.enabled)continue;
                float t=age/.43f,a=i*2.399963f,n=(i*37%17)/16f;
                var origin=new Vector3(((i*7)%9-4)*.062f,((i*11)%7-3)*.067f,0);
                var drift=deathLight*(.07f+t*.15f)+new Vector3(Mathf.Cos(a)*(.12f*t),.20f*t-.34f*t*t,0);
                bit.transform.localPosition=Position(state.Player)+origin+drift;
                Color face=i%5==0?GameThemes.Current.Cape:GameThemes.Current.ObjectLight;
                Color charred=Color.Lerp(GameThemes.Current.Background,new Color(.26f,.24f,.25f,1),.6f);
                Color color=Color.Lerp(face,charred,Mathf.Clamp01(t*1.4f));
                if(i<4)color=Color.Lerp(new Color(1,.79f,.39f),color,Mathf.Clamp01(t*3));
                color.a=Mathf.SmoothStep(0,1,age/.025f)*(1-Mathf.SmoothStep(0,1,(t-.52f)/.48f))*.85f;bit.color=color;
                bit.transform.localScale=Vector3.one*((.028f+n*.037f)*(1-t*.48f));bit.transform.localRotation=Quaternion.Euler(0,0,(i%4)*90+t*(i%2==0?48:-48));
            }
            if(dissolve>=1)player.enabled=cape.enabled=fangs.enabled=false;
        }
    }
}
