using UnityEngine;
using UnityEngine.UI;
namespace DraculasBox.Runtime
{
    public sealed class FadingGraphic:MonoBehaviour
    {
        private Graphic graphic;private Color color;private float start;
        private void Awake(){graphic=GetComponent<Graphic>();color=graphic.color;start=Time.unscaledTime;}
        private void Update(){float t=(Time.unscaledTime-start)/.38f;graphic.color=new Color(color.r,color.g,color.b,color.a*(1-Mathf.Clamp01(t)));if(t>=1)Destroy(gameObject);}
    }
}
