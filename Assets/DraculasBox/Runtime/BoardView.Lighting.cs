using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        private SpriteRenderer coffinExposure;
        private float coffinFrom,coffinTo;
        private Vector3 ContactOffset()
        {
            if(!state.SunEnabled)return new Vector3(.035f,-.055f,0);
            var v=LightField.SunTravel[(int)state.SunFrom];return new Vector3(v.x,-v.y,0)*.07f+Vector3.down*.012f;
        }
        private static Quaternion SunRotation(SunSide side)
        {
            var v=LightField.SunTravel[(int)side];return Quaternion.Euler(0,0,Mathf.Atan2(-v.y,v.x)*Mathf.Rad2Deg);
        }
        private void BlendCoffinLight(float t)
        {
            coffinExposure.color=new Color(1,.87f,.60f,Mathf.Lerp(coffinFrom,coffinTo,t));
        }
    }
}
