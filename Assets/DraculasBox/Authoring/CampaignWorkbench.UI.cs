using System;
using System.Collections.Generic;
using System.Linq;
using DraculasBox.Core;
using DraculasBox.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Editor
{
    internal sealed partial class CampaignWorkbench
    {
        private static Color Muted => GameThemes.IsClassic ? new Color32(94,89,101,255) : GameThemes.Current.WorkshopMuted;
        private Button Button(string id,string text,float x,float y,float w,Action action,bool primary=false)
            => UI.EditorButton(id,text,new Vector2(x,y),new Vector2(w,40),action,primary,null,true);
        private void Label(string id,string value,float x,float y,float w,float size=20,bool quiet=false)
            => UI.Text(id,value,new Vector2(x,y),new Vector2(w,40),size,quiet?Muted:GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);
        private void Rule(string id,float x,float y,float w,float h=1)
            => UI.Image(id,null,new Vector2(x,y),new Vector2(w,h),GameUI.WorkshopLine);
        private void Draw()
        {
            HidePeek();hoveredId=null;
            if(mapView?.Alive==true)mapOffset=mapView.Offset;
            mapView=null;
            if(panel!=Panel.Chapters&&panel!=Panel.Outline)
            {
                overlay=panel==Panel.Remove?Overlay.Remove:panel==Panel.Transfer?Overlay.Transfer:panel==Panel.Anchor?Overlay.Anchor:Overlay.Reload;
                panel=Panel.Outline;
            }
            if(panel==Panel.Chapters)DrawChapters();else DrawOutline();
            DrawOverlay();
        }
    }
}
