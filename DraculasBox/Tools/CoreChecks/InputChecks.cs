using DraculasBox.Runtime;
using UnityEngine;

// StudyInput uses only KeyCode identities, not Unity's device APIs. Runtime checks
// separately exercise the same class against the real Unity frame loop.
namespace UnityEngine
{
    public enum KeyCode {None,UpArrow,RightArrow,DownArrow,LeftArrow,W,D,S,A,Escape,Z,Return,KeypadEnter,Space,R}
    public static class Input
    {
        public static bool GetKey(KeyCode key)=>throw new NotSupportedException("Use the isolated keyboard.");
        public static bool GetKeyDown(KeyCode key)=>throw new NotSupportedException("Use the isolated keyboard.");
    }
}
internal static class InputChecks
{
    private sealed class Keyboard:IStudyKeyboard
    {
        public KeyCode Down,HeldKey;
        public bool Pressed(KeyCode key)=>key==Down&&key!=KeyCode.None;
        public bool Held(KeyCode key)=>key==HeldKey&&key!=KeyCode.None;
    }
    public static (bool passed,int checks,object report) Run()
    {
        var input=new StudyInput();var key=new Keyboard{Down=KeyCode.RightArrow,HeldKey=KeyCode.RightArrow};
        var first=input.Poll(key,0,true,false);
        key.Down=key.HeldKey=KeyCode.UpArrow;var during=input.Poll(key,.04f,true,true);
        key.Down=key.HeldKey=KeyCode.None;var buffered=input.Poll(key,.11f,true,false);
        var noExtra=input.Poll(key,.12f,true,false);
        input.Clear();key.Down=key.HeldKey=KeyCode.RightArrow;input.Poll(key,1,true,false);key.Down=KeyCode.None;
        var early=input.Poll(key,1.13f,true,false);var repeat=input.Poll(key,1.23f,true,false);
        input.Clear();key.Down=key.HeldKey=KeyCode.UpArrow;input.Poll(key,2,true,true);key.Down=key.HeldKey=KeyCode.LeftArrow;input.Poll(key,2.03f,true,true);
        key.Down=key.HeldKey=KeyCode.None;var latest=input.Poll(key,2.11f,true,false);
        input.Clear();key.Down=key.HeldKey=KeyCode.UpArrow;input.Poll(key,3,true,true);key.Down=KeyCode.R;var reset=input.Poll(key,3.03f,true,true);
        key.Down=key.HeldKey=KeyCode.None;var afterReset=input.Poll(key,3.11f,true,false);
        bool passed=first==StudyAction.Right&&during==null&&buffered==StudyAction.Up&&noExtra==null&&early==null&&repeat==StudyAction.Right&&latest==StudyAction.Left&&reset==StudyAction.Reset&&afterReset==null;
        return(passed,9,new{passed,first=first?.ToString(),during=during?.ToString(),buffered=buffered?.ToString(),noExtra=noExtra?.ToString(),earlyRepeat=early?.ToString(),repeat=repeat?.ToString(),latest=latest?.ToString(),reset=reset?.ToString(),afterReset=afterReset?.ToString()});
    }
}
