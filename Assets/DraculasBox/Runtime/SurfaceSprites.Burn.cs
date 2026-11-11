using UnityEngine;
namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static Sprite burnAsh;
        public static Sprite BurnAsh=>burnAsh??(burnAsh=Make("Burn fragment",(x,y)=>
        {
            if(x<6||x>57||y<7||y>56||x<13&&y>47||x>49&&y<15)return Color.clear;
            return x<11||y>51?new Color(1,1,1,.65f):Color.white;
        }));
    }
}
