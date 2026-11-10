using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private bool ownsDeathBackdrop;
        private Color deathBackdrop,appliedBackdrop;
        private const float DeathPause=2f/60f;

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
            float u=Time.unscaledTime-deathAt-DeathImpactSeconds-DeathPause;
            float fade=Mathf.SmoothStep(0,1,(u-.035f)/.19f),alpha=1-fade;
            player.color=cape.color=fangs.color=new Color(1,1,1,alpha);
            player.transform.localScale*=1-fade*.055f;fangs.transform.localScale=player.transform.localScale;
            var capeScale=cape.transform.localScale;capeScale.y*=1-fade*.20f;cape.transform.localScale=capeScale;
            cape.transform.localPosition+=Vector3.down*(3*unitsPerUI*fade);
            for(int i=0;i<embers.Length;i++)
            {
                var bit=embers[i];bit.enabled=u>.13f&&u<.45f;
                if(!bit.enabled)continue;
                float t=Mathf.Clamp01((u-.13f)/.32f),x=i==0?-9:6,y=i==0?-3:2,size=i==0?1.5f:1.9f;
                bit.transform.localPosition=Position(state.Player)+new Vector3(x+(i-1)*t*3,-y-t*(5+i*2),0)*unitsPerUI;
                Color color=i==0?new Color32(211,208,201,255):new Color32(169,167,173,255);color.a=Mathf.Sin(t*Mathf.PI)*.38f;bit.color=color;
                bit.transform.localScale=Vector3.one*(size*(1-t*.30f)*unitsPerUI);
            }
            if(fade>=1)player.enabled=cape.enabled=fangs.enabled=false;
        }
    }
}
