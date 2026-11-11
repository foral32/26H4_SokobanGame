using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private SpriteRenderer[] resetBats,frontCape;
        private double resetAt=-1,characterTick;
        private Vector3 resetFrom;
        private bool pauseTarget;private float pauseBlend;
        private const float BatResetSeconds=.34f;
        private static readonly int[] BatFrames={0,1,2,1};
        private void BuildCharacterEffects()
        {
            characterTick=Time.realtimeSinceStartupAsDouble;if(editing)return;
            resetBats=new SpriteRenderer[3];frontCape=new SpriteRenderer[2];
            for(int frame=0;frame<3;frame++)SurfaceSprites.Bat(frame);
            for(int i=0;i<resetBats.Length;i++){resetBats[i]=MakeSprite("Reset bat "+i,SurfaceSprites.Bat(0),18,Vector3.zero,.34f);resetBats[i].enabled=false;}
            for(int i=0;i<frontCape.Length;i++){frontCape[i]=MakeSprite("Pause cape fold "+i,SurfaceSprites.CapeFold,14,Vector3.zero,1);frontCape[i].enabled=false;}
        }
        private void ClearCharacterEffects()
        {
            if(resetBats!=null)foreach(var r in resetBats)if(r!=null)r.enabled=false;
            if(frontCape!=null)foreach(var r in frontCape)if(r!=null)r.enabled=false;
            resetBats=frontCape=null;resetAt=-1;pauseTarget=false;pauseBlend=0;
        }
        private void StartCharacterAction(ActionResult result)
        {
            resetAt=-1;
            if(!editing&&result.Kind==ActionKind.Reset&&!state.Dead&&!state.Won)
            {resetAt=Time.realtimeSinceStartupAsDouble;resetFrom=Position(previous.Player);pauseBlend=0;}
        }
        public void SetPaused(bool value)
        {
            pauseTarget=value;characterTick=Time.realtimeSinceStartupAsDouble;
            if(value)resetAt=-1;
        }
        private void RefreshCharacterEffects()
        {
            if(editing||resetBats==null)return;
            double now=Time.realtimeSinceStartupAsDouble;float dt=Mathf.Clamp((float)(now-characterTick),0,.1f);characterTick=now;
            foreach(var r in resetBats)r.enabled=false;foreach(var r in frontCape)r.enabled=false;
            if(state.Dead||state.Won){resetAt=-1;pauseBlend=0;return;}
            if(resetAt>=0)
            {
                float age=(float)(now-resetAt),q=age/BatResetSeconds;
                if(q>=1)resetAt=-1;
                else
                {
                    float shrink=1-Mathf.SmoothStep(0,1,age/.09f),grow=Mathf.SmoothStep(0,1,(age-.20f)/.14f);
                    float size=age<.12f?shrink:grow;
                    Vector3 anchor=age<.12f?resetFrom:Position(state.Player);
                    player.transform.localPosition=anchor+Vector3.up*.015f;cape.transform.localPosition=anchor+Vector3.down*.025f;
                    fangs.transform.localPosition=player.transform.localPosition+Vector3.down*.035f;
                    player.transform.localScale=Vector3.one*(.72f*size);cape.transform.localScale=Vector3.one*(.94f*size);fangs.transform.localScale=player.transform.localScale;
                    player.enabled=cape.enabled=size>.015f;fangs.enabled=player.enabled&&state.Facing!=Command.Up;
                    for(int i=0;i<resetBats.Length;i++)
                    {
                        float u=Mathf.Clamp01((age-.025f-i*.012f)/.27f),arc=Mathf.Sin(u*Mathf.PI);
                        var bat=resetBats[i];bat.enabled=u>0&&u<1;
                        if(!bat.enabled)continue;
                        Vector3 point=Vector3.Lerp(resetFrom,Position(state.Player),u*u*(3-2*u));
                        point+=new Vector3((i-1)*.44f*arc,arc*(.28f+i*.14f)+Mathf.Sin(u*Mathf.PI*3+i)*.09f*arc,0);
                        bat.transform.localPosition=point;bat.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(u*Mathf.PI*2+i)*14);
                        bat.transform.localScale=Vector3.one*(.30f+arc*.07f);
                        SetSprite(bat,SurfaceSprites.Bat(BatFrames[((int)(age*17)+i)%4]));
                        var ink=Color.Lerp(GameThemes.Current.Cape,GameThemes.Current.ObjectLight,.24f);ink.a=Mathf.Clamp01(arc*2);bat.color=ink;
                    }
                }
            }
            pauseBlend=Mathf.MoveTowards(pauseBlend,pauseTarget?1:0,dt/.16f);
            if(pauseBlend<=0||!player.enabled)return;
            float wrap=pauseBlend*pauseBlend*(3-2*pauseBlend);
            cape.transform.localScale=new Vector3(.94f*(1-.10f*wrap),.94f,1);
            for(int i=0;i<frontCape.Length;i++)
            {
                var fold=frontCape[i];float side=i==0?-1:1;fold.enabled=true;
                fold.transform.localPosition=player.transform.localPosition+new Vector3(side*Mathf.Lerp(.29f,.10f,wrap),-.33f,0);
                fold.transform.localScale=new Vector3(side*.43f,.23f,1);
                Color ink=i==0?GameThemes.Current.Cape:Color.Lerp(GameThemes.Current.Cape,GameThemes.Current.CapeShade,.18f);ink.a=wrap;fold.color=ink;
            }
            if(wrap>.5f)fangs.enabled=false;
        }
    }
}
