#if UNITY_EDITOR
using System;
using System.IO;
using DraculasBox.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    // Records the actual camera + Canvas. Samples contain real commands and animations.
    public sealed class ShowcaseRecorder:MonoBehaviour
    {
        public DraculaApp app;public string Clip {get;private set;}
        private RenderTexture target;private Texture2D image;private float next;private int frame;
        private string directory;private RectTransform pointer;private Vector2 point;private bool showPointer;
        public void Begin(string id)
        {
            End();Clip=id;directory="Library/DraculasBoxShowcase/Frames/"+id;Directory.CreateDirectory(directory);frame=0;next=0;
            target=new RenderTexture(1280,720,24);target.Create();image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        }
        public void Point(Vector2 screen)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle(app.UI.Root,screen,app.viewCamera,out point);showPointer=true;}
        public void HidePointer(){showPointer=false;}
        private void LateUpdate()
        {
            if(Clip==null||Time.unscaledTime<next)return;next=Time.unscaledTime+.05f;
            var camera=app.viewCamera;var prior=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            try
            {
                camera.targetTexture=target;camera.aspect=1280f/720;app.Board.FitForCapture();Canvas.ForceUpdateCanvases();
                if(showPointer)
                {
                    if(pointer==null){pointer=app.UI.Rect("Recorded pointer",Vector2.zero,Vector2.one*20);var g=pointer.gameObject.AddComponent<SelectionCorners>();g.color=GameUI.Gold;g.raycastTarget=false;}
                    pointer.gameObject.SetActive(true);pointer.anchoredPosition=point;pointer.SetAsLastSibling();
                }
                else if(pointer!=null)pointer.gameObject.SetActive(false);
                camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes(directory+"/"+(frame++).ToString("D5")+".jpg",image.EncodeToJPG(92));
            }
            finally{camera.targetTexture=prior;camera.aspect=aspect;RenderTexture.active=active;app.Board.FitForCapture();}
        }
        public void End()
        {
            if(Clip!=null)File.WriteAllText(directory+"/frames.txt",frame.ToString());
            Clip=null;if(pointer!=null)Destroy(pointer.gameObject);pointer=null;showPointer=false;
            if(target!=null){target.Release();Destroy(target);target=null;}if(image!=null){Destroy(image);image=null;}
        }
        private void OnDestroy(){End();}
    }
}

#endif
