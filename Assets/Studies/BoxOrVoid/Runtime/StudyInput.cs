using UnityEngine;

namespace BoxOrVoid.Study
{
    public enum StudyAction { Up, Right, Down, Left, Switch, Undo, Reset, Confirm, Back }

    public interface IStudyKeyboard
    {
        bool Held(KeyCode key);
        bool Pressed(KeyCode key);
    }

    public sealed class UnityStudyKeyboard : IStudyKeyboard
    {
        public bool Held(KeyCode key) => Input.GetKey(key);
        public bool Pressed(KeyCode key) => Input.GetKeyDown(key);
    }

    /// <summary>One binding and repeat path for hardware and isolated test keys.</summary>
    public sealed class StudyInput
    {
        private StudyAction? held;
        private float nextRepeat;
        private static readonly KeyCode[] Arrows={KeyCode.UpArrow,KeyCode.RightArrow,KeyCode.DownArrow,KeyCode.LeftArrow};
        private static readonly KeyCode[] Letters={KeyCode.W,KeyCode.D,KeyCode.S,KeyCode.A};
        public void Clear() { held=null;nextRepeat=0; }

        public StudyAction? Poll(IStudyKeyboard keys,float now,bool puzzle,bool moving)
        {
            if(keys.Pressed(KeyCode.Escape) || (!puzzle && keys.Pressed(KeyCode.Z))) { Clear();return StudyAction.Back; }
            if(!puzzle && (keys.Pressed(KeyCode.Return)||keys.Pressed(KeyCode.KeypadEnter)||keys.Pressed(KeyCode.Space))) { Clear();return StudyAction.Confirm; }
            if(puzzle)
            {
                if(keys.Pressed(KeyCode.Space)) { Clear();return moving?null:(StudyAction?)StudyAction.Switch; }
                if(keys.Pressed(KeyCode.R)) { Clear();return StudyAction.Reset; }
                if(keys.Pressed(KeyCode.Z)) { held=StudyAction.Undo;nextRepeat=now+.5f;return StudyAction.Undo; }
                if(held==StudyAction.Undo && keys.Held(KeyCode.Z))
                { if(now<nextRepeat) return null;nextRepeat=now+.2f;return StudyAction.Undo; }
            }
            int direction=-1;
            for(int i=0;i<4;i++) if(keys.Pressed(Arrows[i])||keys.Pressed(Letters[i])) direction=i;
            if(direction<0 && held.HasValue && (int)held.Value<4)
            { int i=(int)held.Value;if(keys.Held(Arrows[i])||keys.Held(Letters[i])) direction=i; }
            if(direction<0) for(int i=0;i<4;i++) if(keys.Held(Arrows[i])||keys.Held(Letters[i])) {direction=i;break;}
            if(direction<0) { Clear();return null; }
            var action=(StudyAction)direction;
            if(moving && puzzle) return null;
            if(held==action && now<nextRepeat) return null;
            bool first=held!=action;
            held=action;nextRepeat=now+(!puzzle && first?.25f:.12f);return action;
        }
    }
}
