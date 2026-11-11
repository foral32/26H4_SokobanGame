using DraculasBox.Core;
using TMPro;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private RectTransform levelPeek;
        public void HideLevelPeek()
        {
            if(levelPeek==null)return;
            levelPeek.gameObject.SetActive(false);Destroy(levelPeek.gameObject);levelPeek=null;
        }
        public void ShowLevelPeek(LevelData data,RectTransform anchor,string displayName=null)
        {
            HideLevelPeek();
            var size=new Vector2(384,310);
            var point=(Vector2)content.InverseTransformPoint(anchor.position);
            float side=point.x+anchor.rect.width*.5f+size.x+28<content.rect.xMax?1:-1;
            var center=point+new Vector2(side*(anchor.rect.width*.5f+size.x*.5f+22),38);
            center.x=Mathf.Clamp(center.x,content.rect.xMin+size.x*.5f+18,content.rect.xMax-size.x*.5f-18);
            center.y=Mathf.Clamp(center.y,content.rect.yMin+size.y*.5f+22,content.rect.yMax-size.y*.5f-22);
            levelPeek=Rect("Level peek",center,size);
            var group=levelPeek.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            SoftCard("Peek shadow",new Vector2(0,-8),size+Vector2.one*30,new Color(0,0,0,.25f),levelPeek,true);
            SoftCard("Peek surface",Vector2.zero,size,Color.Lerp(GameThemes.Current.Panel,White,.075f),levelPeek);
            SoftCard("Peek board",new Vector2(0,25),new Vector2(356,234),Color.Lerp(BoardView.SafeColor,GameThemes.Current.Panel,.25f),levelPeek);
            Thumbnail("Peek layout",data,new Vector2(0,25),new Vector2(340,218),levelPeek);
            var accent=Gold;accent.a=.65f;SoftCard("Peek accent",new Vector2(0,-107),new Vector2(22,3),accent,levelPeek);
            Label(Rect("Peek title",new Vector2(0,-131),new Vector2(338,35),levelPeek),displayName??data.title,24,White);
            levelPeek.gameObject.AddComponent<WorkshopPopover>();
        }
        private UnityEngine.UI.Image SoftCard(string name,Vector2 at,Vector2 size,Color ink,Transform parent,bool shadow=false)
        {
            var image=Image(name,null,at,size,ink,parent);image.sprite=shadow?SurfaceSprites.SoftCardShadow:SurfaceSprites.SoftCard;
            image.type=UnityEngine.UI.Image.Type.Sliced;image.raycastTarget=false;return image;
        }
    }
}
