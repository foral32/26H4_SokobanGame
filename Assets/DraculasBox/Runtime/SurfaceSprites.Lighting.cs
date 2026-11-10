using UnityEngine;

namespace DraculasBox.Runtime
{
    public static partial class SurfaceSprites
    {
        private static Sprite coffinLight;
        public static Sprite CoffinLight=>coffinLight!=null?coffinLight:(coffinLight=Make("Coffin interior light",(x,y)=>
        {
            float side=Mathf.Abs(x-31.5f),edge=y<44?15.5f+(y-7)*.10f:19.2f-(y-44)*.67f;
            return y>=7&&y<=57&&side<edge?new Color(1,1,1,.55f+.35f*y/63f):Color.clear;
        }));
    }
}
