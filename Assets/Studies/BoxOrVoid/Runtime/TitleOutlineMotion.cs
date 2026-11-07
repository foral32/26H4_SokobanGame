using UnityEngine;

namespace BoxOrVoid.Study
{
    public sealed class TitleOutlineMotion:MonoBehaviour
    {
        private RectTransform[] outlines;
        private Vector2[] positions;
        private void Awake()
        {
            outlines=new RectTransform[transform.childCount];positions=new Vector2[outlines.Length];
            for(int i=0;i<outlines.Length;i++) {outlines[i]=(RectTransform)transform.GetChild(i);positions[i]=outlines[i].anchoredPosition;}
        }
        private void Update()
        {
            for(int i=0;i<outlines.Length;i++)
            {
                float phase=Time.unscaledTime*.06f+i*1.618f;
                outlines[i].anchoredPosition=positions[i]+new Vector2(Mathf.Sin(phase),Mathf.Cos(phase*.7f))*22;
            }
        }
    }
}
