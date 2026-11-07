using UnityEngine;

namespace BoxOrVoid.Study.LevelEditing
{
    public static class EditorShortcuts
    {
        public static string Poll(IStudyKeyboard k,bool typing,bool modal)
        {
            if(k==null)return null;
            if(k.Pressed(KeyCode.Escape))return "back";
            if(typing||modal)return null;
            bool control=k.Held(KeyCode.LeftControl)||k.Held(KeyCode.RightControl)||k.Held(KeyCode.LeftCommand)||k.Held(KeyCode.RightCommand);
            bool shift=k.Held(KeyCode.LeftShift)||k.Held(KeyCode.RightShift);
            if(control)
            {
                if(k.Pressed(KeyCode.Z))return shift?"redo":"undo";
                if(k.Pressed(KeyCode.Y))return "redo";
                if(k.Pressed(KeyCode.S))return "save";
                if(k.Pressed(KeyCode.C))return "copy";
                if(k.Pressed(KeyCode.X))return "cut";
                if(k.Pressed(KeyCode.V))return "paste";
                if(k.Pressed(KeyCode.A))return "select-all";
                return null;
            }
            if(k.Pressed(KeyCode.Z))return "undo";
            if(k.Pressed(KeyCode.Y))return "redo";
            if(k.Pressed(KeyCode.F5))return "play";
            if(k.Pressed(KeyCode.B))return "tool-place";
            if(k.Pressed(KeyCode.V))return "tool-look";
            if(k.Pressed(KeyCode.E))return "tool-erase";
            if(k.Pressed(KeyCode.I))return "tool-pick";
            if(k.Pressed(KeyCode.L))return "tool-line";
            if(k.Pressed(KeyCode.U))return "tool-rectangle";
            if(k.Pressed(KeyCode.F))return "tool-fill";
            if(k.Pressed(KeyCode.M))return "tool-select";
            if(k.Pressed(KeyCode.Tab))return "tone";
            if(k.Pressed(KeyCode.Q))return "view";
            if(k.Pressed(KeyCode.G))return "grid";
            if(k.Pressed(KeyCode.Home))return "fit";
            if(k.Pressed(KeyCode.Delete)||k.Pressed(KeyCode.Backspace))return "delete";
            if(k.Pressed(KeyCode.F1))return "help";
            for(int i=0;i<6;i++)if(k.Pressed(KeyCode.Alpha1+i))return "brush-"+i;
            return null;
        }
        public static string Hint(string id)
        {
            switch(id)
            {
                case "undo":return "Z / Ctrl+Z";case "redo":return "Y / Ctrl+Y";case "save":return "Ctrl+S";case "play":case "return-edit":return "F5";
                case "tool-place":return "B";case "tool-look":return "V";case "tool-erase":return "E";case "tool-pick":return "I";case "tool-line":return "L";case "tool-rectangle":return "U";case "tool-fill":return "F";case "tool-select":return "M";
                case "tone":return "Tab";case "view":return "Q";case "grid":return "G";case "fit":return "Home";case "help":return "F1";
                case "copy":return "Ctrl+C";case "cut":return "Ctrl+X";case "paste":return "Ctrl+V";case "delete":return "Delete";case "back":return "Esc";
                default:return id.StartsWith("brush-")?(int.Parse(id.Substring(6))+1).ToString():"";
            }
        }
    }
}
