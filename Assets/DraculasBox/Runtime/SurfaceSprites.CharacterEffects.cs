using UnityEngine;

namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static readonly Sprite[] bats=new Sprite[3];private static Sprite capeFold;
        public static Sprite Bat(int frame)
        {
            frame=Mathf.Clamp(frame,0,2);if(bats[frame]!=null)return bats[frame];int pose=frame;
            return bats[frame]=Make("Bat wing "+frame,(x,y)=>
            {
                float dx=Mathf.Abs(x-31.5f),dy=y-31.5f;
                bool body=dx*dx/19+dy*dy/100<1;
                bool ears=dy>6&&dy<17&&dx>1.5f&&dx<(18-dy)*.48f+2;
                float reach=pose==0?28:pose==1?23:15;
                float upper=pose==0?10+dx*.35f:pose==1?8-dx*.10f:5-dx*.60f;
                float lower=pose==0?-9+dx*.12f:pose==1?-12+dx*.05f:-17+dx*.15f;
                // Three scallops give the wings a readable bat silhouette at small scale.
                lower+=Mathf.Pow(Mathf.Sin(dx/reach*Mathf.PI*3),2)*5;
                bool wing=dx>2&&dx<reach&&dy<upper&&dy>lower;
                if(!body&&!ears&&!wing)return Color.clear;
                float shade=body||dy>upper-2?1:.86f;return new Color(shade,shade,shade,1);
            });
        }
        public static Sprite CapeFold=>capeFold??(capeFold=Make("Cape front fold",(x,y)=>
        {
            float u=x/63f,v=y/63f;if(u<.08f+v*.18f||u>.98f-v*.12f||v<.05f||v>.96f)return Color.clear;
            float tone=u<.14f+v*.18f?.65f:u>.82f-v*.12f?1.18f:.94f;
            return new Color(tone,tone,tone,1);
        }));
    }
}
