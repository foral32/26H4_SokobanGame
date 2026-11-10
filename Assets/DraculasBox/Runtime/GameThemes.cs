using System;
using System.Collections.Generic;
using UnityEngine;

namespace DraculasBox.Runtime
{
    /// <summary>Presentation colors only. Theme selection never enters the puzzle state.</summary>
    public sealed class GameTheme
    {
        public string Id {get;private set;} public string Name {get;private set;} public int Index {get;private set;}
        public bool LightMenu {get;internal set;}
        public Color Background {get;internal set;} public Color AtmosphereCenter {get;internal set;} public Color AtmosphereEdge {get;internal set;}
        public Color Foreground {get;internal set;} public Color Secondary {get;internal set;} public Color Ink {get;internal set;}
        public Color TitleFace {get;internal set;} public Color TitleDepth {get;internal set;} public Color Focus {get;internal set;} public Color Accent {get;internal set;}
        public Color Button {get;internal set;} public Color ButtonInk {get;internal set;} public Color Panel {get;internal set;} public Color Border {get;internal set;}
        public Color NodeOpen {get;internal set;} public Color NodeDone {get;internal set;} public Color NodeLocked {get;internal set;}
        public Color NodeInk {get;internal set;} public Color NodeDoneInk {get;internal set;} public Color Lock {get;internal set;} public Color Path {get;internal set;}
        public Color WorkshopBackground {get;internal set;} public Color WorkshopSurface {get;internal set;} public Color WorkshopLine {get;internal set;}
        public Color WorkshopInk {get;internal set;} public Color WorkshopMuted {get;internal set;} public Color WorkshopAccent {get;internal set;}
        public Color WorkshopSelected {get;internal set;} public Color WorkshopOnAccent {get;internal set;}
        public Color FloorSafe {get;internal set;} public Color FloorLit {get;internal set;}
        public Color WallTop {get;internal set;} public Color WallFront {get;internal set;} public Color WallFoot {get;internal set;}
        public Color WallLight {get;internal set;} public Color WallDark {get;internal set;} public Color WallRim {get;internal set;}
        public Color ObjectTop {get;internal set;} public Color ObjectShade {get;internal set;} public Color ObjectLight {get;internal set;}
        public Color Cape {get;internal set;} public Color CapeShade {get;internal set;}
        public Color CoffinFace {get;internal set;} public Color CoffinRim {get;internal set;} public Color CoffinLight {get;internal set;} public Color CoffinInside {get;internal set;}
        internal GameTheme(string id,string name,int index){Id=id;Name=name;Index=index;}
        internal GameTheme Variant(string id,string name,int index){var copy=(GameTheme)MemberwiseClone();copy.Id=id;copy.Name=name;copy.Index=index;return copy;}
    }

    public static class GameThemes
    {
        public const string DefaultThemeId="wine";
        private static Color C(uint rgb)=>new Color32((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb,255);
        private static readonly GameTheme[] palettes=Create();
        public static IReadOnlyList<GameTheme> All {get;}=Array.AsReadOnly(palettes);
        public static GameTheme Current {get;private set;}=Resolve(DefaultThemeId);
        public static bool IsClassic=>Current.Id=="classic";
        public static GameTheme Resolve(string id)
        {foreach(var theme in palettes)if(string.Equals(theme.Id,id,StringComparison.Ordinal))return theme;foreach(var theme in palettes)if(string.Equals(theme.Id,DefaultThemeId,StringComparison.Ordinal))return theme;return palettes[0];}
        public static bool Apply(string id){var next=Resolve(id);if(ReferenceEquals(next,Current))return false;Current=next;return true;}

        private static GameTheme[] Create()
        {
            var original=new GameTheme("classic","暮夜",0)
            {
                Background=C(0x202530),AtmosphereCenter=C(0x363B49),AtmosphereEdge=C(0x1B1F29),Foreground=C(0xF2EFEB),Secondary=C(0xA7B1C2),Ink=C(0x31424D),
                TitleFace=C(0xF2EFEB),TitleDepth=C(0x7B4C62),Focus=C(0xEDCA8F),Accent=C(0xB18EA9),Button=C(0xA9AFBF),ButtonInk=C(0x202530),Panel=C(0x2F3645),Border=C(0x576175),
                NodeOpen=C(0xE2E5E7),NodeDone=C(0x896784),NodeLocked=C(0x566377),NodeInk=C(0x31424D),NodeDoneInk=C(0xF2EFEB),Lock=C(0xAFBAC8),Path=C(0x586377),
                WorkshopBackground=C(0xE7E5E8),WorkshopSurface=C(0xF4F2F4),WorkshopLine=C(0xB7B2BC),WorkshopInk=C(0x2D2C34),WorkshopMuted=C(0x637784),WorkshopAccent=C(0x896386),WorkshopSelected=C(0xE4D7E6),WorkshopOnAccent=C(0xEFF2F4),
                FloorSafe=C(0x364656),FloorLit=C(0xA4ABA9),WallTop=C(0x778596),WallFront=C(0x485468),WallFoot=C(0x374255),WallLight=C(0xAFBDCA),WallDark=C(0x4E5C71),WallRim=C(0x657488),
                ObjectTop=C(0xE8E7EC),ObjectShade=C(0xB3B0BE),ObjectLight=C(0xFAF8F5),Cape=C(0x764A6A),CapeShade=C(0x4F2F49),
                CoffinFace=C(0x543646),CoffinRim=C(0xA77C8F),CoffinLight=C(0xC3A5B0),CoffinInside=C(0x312737)
            };
            // Approved cream editor + dark wine menu sheet, not the later all-light revision.
            var wine=original.Variant("wine","酒红",1);
            wine.Background=C(0x403139);wine.AtmosphereCenter=C(0x54434B);wine.AtmosphereEdge=C(0x382A33);wine.Foreground=C(0xF7EBD4);wine.Secondary=C(0xD5BFB2);wine.Ink=C(0x612C3F);
            wine.TitleFace=C(0xFBEFD7);wine.TitleDepth=C(0x783448);wine.Focus=C(0xEAC789);wine.Accent=C(0xC78D9A);wine.Button=C(0xF1DEC3);wine.ButtonInk=C(0x612C3F);wine.Panel=C(0x55434B);wine.Border=C(0x866C77);
            wine.NodeOpen=C(0xD6BAAD);wine.NodeDone=C(0xA27483);wine.NodeLocked=C(0x65515B);wine.NodeInk=C(0x612C3F);wine.NodeDoneInk=C(0xFBEFD7);wine.Lock=C(0xBC9EAA);wine.Path=C(0x846A76);
            wine.WorkshopBackground=C(0xEEE1CF);wine.WorkshopSurface=C(0xFAEEDB);wine.WorkshopLine=C(0xC3A08E);wine.WorkshopInk=C(0x612C3F);wine.WorkshopMuted=C(0x8B6B70);wine.WorkshopAccent=C(0x8B354C);wine.WorkshopSelected=C(0xDEC0BA);wine.WorkshopOnAccent=C(0xFFF4DC);
            wine.FloorSafe=C(0x4A3C42);wine.FloorLit=C(0xC9BDA7);wine.WallTop=C(0xB8A292);wine.WallFront=C(0x887467);wine.WallFoot=C(0x62534E);wine.WallLight=C(0xE5D2BC);wine.WallDark=C(0x958271);wine.WallRim=C(0xAF9E89);
            wine.ObjectTop=C(0xF2E5D0);wine.ObjectShade=C(0xC4AC9D);wine.ObjectLight=C(0xFFF7E7);wine.Cape=C(0x8B354C);wine.CapeShade=C(0x58263B);wine.CoffinFace=C(0x713047);wine.CoffinRim=C(0xB76D83);wine.CoffinLight=C(0xDAABB0);wine.CoffinInside=C(0x412333);
            var cobalt=original.Variant("cobalt","钴蓝",2);
            cobalt.Background=C(0x123968);cobalt.AtmosphereCenter=C(0x1A447B);cobalt.AtmosphereEdge=C(0x0E2E57);cobalt.Foreground=C(0xF4EEE3);cobalt.Secondary=C(0xBBCDE2);cobalt.Ink=C(0x173B72);
            cobalt.TitleFace=C(0xFFF2DB);cobalt.TitleDepth=C(0xC87572);cobalt.Focus=C(0xF2B889);cobalt.Accent=C(0xEE8B73);cobalt.Button=C(0xB1C8E3);cobalt.ButtonInk=C(0x143660);cobalt.Panel=C(0x234A80);cobalt.Border=C(0x6E92BC);
            cobalt.NodeOpen=C(0x87A8D4);cobalt.NodeDone=C(0x5B7FAE);cobalt.NodeLocked=C(0x2D507B);cobalt.NodeInk=C(0x143660);cobalt.NodeDoneInk=C(0xFFF2DB);cobalt.Lock=C(0x8BA7C8);cobalt.Path=C(0x5477A2);
            cobalt.WorkshopBackground=C(0x173B72);cobalt.WorkshopSurface=C(0x234A80);cobalt.WorkshopLine=C(0x6E92BC);cobalt.WorkshopInk=C(0xF0EDDF);cobalt.WorkshopMuted=C(0xB4CAE1);cobalt.WorkshopAccent=C(0xE8907B);cobalt.WorkshopSelected=C(0x8E5767);cobalt.WorkshopOnAccent=C(0x122F50);
            cobalt.FloorSafe=C(0x1B3553);cobalt.FloorLit=C(0xB2BEB7);cobalt.WallTop=C(0x7698C1);cobalt.WallFront=C(0x3D5E88);cobalt.WallFoot=C(0x25456D);cobalt.WallLight=C(0xB0C9E7);cobalt.WallDark=C(0x4C6E98);cobalt.WallRim=C(0x6F8FB8);
            cobalt.ObjectTop=C(0xF2EADF);cobalt.ObjectShade=C(0xB4C1D3);cobalt.ObjectLight=C(0xFFF8E9);cobalt.Cape=C(0xC87572);cobalt.CapeShade=C(0x81485C);cobalt.CoffinFace=C(0x6B3E47);cobalt.CoffinRim=C(0xEC8D79);cobalt.CoffinLight=C(0xF4B9A0);cobalt.CoffinInside=C(0x352B3B);
            // First-round neutral black/silver/crimson. Do not use the rejected purple-tinted refinement.
            var eclipse=original.Variant("eclipse","月蚀",3);
            eclipse.Background=C(0x111214);eclipse.AtmosphereCenter=C(0x202124);eclipse.AtmosphereEdge=C(0x101113);eclipse.Foreground=C(0xECECE9);eclipse.Secondary=C(0xBDBFC5);eclipse.Ink=C(0x27292D);
            eclipse.TitleFace=C(0xF5F0E6);eclipse.TitleDepth=C(0xA63748);eclipse.Focus=C(0xECCA8E);eclipse.Accent=C(0xB14451);eclipse.Button=C(0xC6C8CD);eclipse.ButtonInk=C(0x24262A);eclipse.Panel=C(0x242629);eclipse.Border=C(0x5B5F65);
            eclipse.NodeOpen=C(0xBFC1C5);eclipse.NodeDone=C(0x873F4B);eclipse.NodeLocked=C(0x3C3E43);eclipse.NodeInk=C(0x27292D);eclipse.NodeDoneInk=C(0xF5F0E6);eclipse.Lock=C(0x94989F);eclipse.Path=C(0x57585C);
            eclipse.WorkshopBackground=C(0x17191C);eclipse.WorkshopSurface=C(0x242629);eclipse.WorkshopLine=C(0x5B5F65);eclipse.WorkshopInk=C(0xEBECE9);eclipse.WorkshopMuted=C(0xABAFB6);eclipse.WorkshopAccent=C(0xAE4252);eclipse.WorkshopSelected=C(0x62353F);eclipse.WorkshopOnAccent=C(0xFFF5E7);
            eclipse.FloorSafe=C(0x2A2D32);eclipse.FloorLit=C(0xBFBDAE);eclipse.WallTop=C(0x696D72);eclipse.WallFront=C(0x42454A);eclipse.WallFoot=C(0x303338);eclipse.WallLight=C(0xC8CDD2);eclipse.WallDark=C(0x4F5258);eclipse.WallRim=C(0x777D83);
            eclipse.ObjectTop=C(0xEFEEEA);eclipse.ObjectShade=C(0xB8B9BE);eclipse.ObjectLight=C(0xFBF8EC);eclipse.Cape=C(0xA63748);eclipse.CapeShade=C(0x602B3C);eclipse.CoffinFace=C(0x6B293B);eclipse.CoffinRim=C(0xBE687C);eclipse.CoffinLight=C(0xDEA1AD);eclipse.CoffinInside=C(0x2D222C);
            // First-round powder blue, not the rejected saturated-blue refinement.
            var porcelain=original.Variant("porcelain","瓷蓝",4);porcelain.LightMenu=true;
            porcelain.Background=C(0xF1F5F2);porcelain.AtmosphereCenter=C(0xE1EAF0);porcelain.AtmosphereEdge=C(0xF1F5F2);porcelain.Foreground=C(0x244A78);porcelain.Secondary=C(0x526D8D);porcelain.Ink=C(0x244A78);
            porcelain.TitleFace=C(0xFFFAE9);porcelain.TitleDepth=C(0x244A78);porcelain.Focus=C(0xD5A057);porcelain.Accent=C(0x3A679C);porcelain.Button=C(0xCAD9E9);porcelain.ButtonInk=C(0x244A78);porcelain.Panel=C(0xE1EAF0);porcelain.Border=C(0xA4B6CA);
            porcelain.NodeOpen=C(0xBCD0E8);porcelain.NodeDone=C(0x6E8FB7);porcelain.NodeLocked=C(0xC0CDDA);porcelain.NodeInk=C(0x244A78);porcelain.NodeDoneInk=C(0xFFFAE9);porcelain.Lock=C(0x748BA6);porcelain.Path=C(0x8DA7C4);
            porcelain.WorkshopBackground=C(0xE1EAF0);porcelain.WorkshopSurface=C(0xF1F5F7);porcelain.WorkshopLine=C(0xA4B6CA);porcelain.WorkshopInk=C(0x244A78);porcelain.WorkshopMuted=C(0x607C9C);porcelain.WorkshopAccent=C(0x3A679C);porcelain.WorkshopSelected=C(0xCAD9E9);porcelain.WorkshopOnAccent=C(0xFFF7E7);
            porcelain.FloorSafe=C(0x344C6C);porcelain.FloorLit=C(0xCFD8CD);porcelain.WallTop=C(0x96ADC9);porcelain.WallFront=C(0x6A829F);porcelain.WallFoot=C(0x546F96);porcelain.WallLight=C(0xD6E2EF);porcelain.WallDark=C(0x7189A7);porcelain.WallRim=C(0x93AACA);
            porcelain.ObjectTop=C(0xF3EEE6);porcelain.ObjectShade=C(0xB6C4D6);porcelain.ObjectLight=C(0xFEFAE9);porcelain.Cape=C(0xC67C7B);porcelain.CapeShade=C(0x88566E);porcelain.CoffinFace=C(0x975767);porcelain.CoffinRim=C(0xC48496);porcelain.CoffinLight=C(0xD7A0A0);porcelain.CoffinInside=C(0x4C3148);
            return new[]{original,wine,cobalt,eclipse,porcelain};
        }
    }
}
