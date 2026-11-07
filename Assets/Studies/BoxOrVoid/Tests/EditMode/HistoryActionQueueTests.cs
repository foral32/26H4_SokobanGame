using BoxOrVoid.Core;
using BoxOrVoid.Study;
using NUnit.Framework;

namespace BoxOrVoid.Tests
{
    public sealed class HistoryActionQueueTests
    {
        [Test] public void UndoWaitsForMotionThenTwoConsecutiveSettledFrames()
        {
            var queue=new HistoryActionQueue();queue.Request(Command.Undo);
            Assert.IsNull(queue.Tick(true));Assert.IsNull(queue.Tick(false));Assert.IsNull(queue.Tick(true));
            Assert.IsNull(queue.Tick(false));Assert.AreEqual(Command.Undo,queue.Tick(false));
            Assert.IsFalse(queue.HasPending);Assert.IsNull(queue.Tick(false));
        }
        [Test] public void MostRecentRequestWinsAndChangingPageClearsIt()
        {
            var queue=new HistoryActionQueue();queue.Request(Command.Undo);queue.Request(Command.Reset);
            Assert.IsNull(queue.Tick(false));Assert.AreEqual(Command.Reset,queue.Tick(false));
            queue.Request(Command.Reset);queue.Clear();Assert.IsNull(queue.Tick(false));Assert.IsFalse(queue.HasPending);
        }
    }
}
