using BoxOrVoid.Core;

namespace BoxOrVoid.Study
{
    /// <summary>Undo/reset wait for two settled frames; later requests replace the pending request.</summary>
    public sealed class HistoryActionQueue
    {
        private Command? pending;
        private int settledFrames;
        public bool HasPending=>pending.HasValue;
        public void Request(Command command) {pending=command;settledFrames=0;}
        public void Clear() {pending=null;settledFrames=0;}
        public Command? Tick(bool moving)
        {
            if(!pending.HasValue) return null;
            if(moving) {settledFrames=0;return null;}
            if(++settledFrames<2) return null;
            var result=pending;Clear();return result;
        }
    }
}
