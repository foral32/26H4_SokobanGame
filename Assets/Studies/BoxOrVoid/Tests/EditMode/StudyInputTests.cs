using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BoxOrVoid.Study;

namespace BoxOrVoid.Tests
{
    public sealed class StudyInputTests
    {
        private sealed class Keys:IStudyKeyboard
        {
            public readonly HashSet<KeyCode> held=new HashSet<KeyCode>(),pressed=new HashSet<KeyCode>();
            public bool Held(KeyCode key)=>held.Contains(key);
            public bool Pressed(KeyCode key)=>pressed.Contains(key);
            public void Set(KeyCode key) {held.Clear();pressed.Clear();held.Add(key);pressed.Add(key);}
        }
        [TestCase(KeyCode.W,StudyAction.Up)] [TestCase(KeyCode.D,StudyAction.Right)]
        [TestCase(KeyCode.S,StudyAction.Down)] [TestCase(KeyCode.A,StudyAction.Left)]
        [TestCase(KeyCode.UpArrow,StudyAction.Up)] [TestCase(KeyCode.RightArrow,StudyAction.Right)]
        [TestCase(KeyCode.DownArrow,StudyAction.Down)] [TestCase(KeyCode.LeftArrow,StudyAction.Left)]
        public void BothDirectionBindingsRepeatAfterPointTwelve(KeyCode key,StudyAction expected)
        {
            var keys=new Keys();keys.Set(key);var input=new StudyInput();
            Assert.AreEqual(expected,input.Poll(keys,0,true,false));keys.pressed.Clear();
            Assert.IsNull(input.Poll(keys,.119f,true,false));
            Assert.AreEqual(expected,input.Poll(keys,.121f,true,false));
        }
        [Test] public void UndoUsesSeparateHoldDelayAndCanInterruptAnimation()
        {
            var keys=new Keys();keys.Set(KeyCode.Z);var input=new StudyInput();
            Assert.AreEqual(StudyAction.Undo,input.Poll(keys,0,true,true));keys.pressed.Clear();
            Assert.IsNull(input.Poll(keys,.49f,true,true));
            Assert.AreEqual(StudyAction.Undo,input.Poll(keys,.5f,true,true));
            Assert.IsNull(input.Poll(keys,.69f,true,true));
            Assert.AreEqual(StudyAction.Undo,input.Poll(keys,.71f,true,true));
        }
        [Test] public void SwitchIsBlockedWhileMovingButSpaceConfirmsMenus()
        {
            var keys=new Keys();keys.Set(KeyCode.Space);var input=new StudyInput();
            Assert.IsNull(input.Poll(keys,0,true,true));
            Assert.AreEqual(StudyAction.Switch,input.Poll(keys,1,true,false));
            Assert.AreEqual(StudyAction.Confirm,input.Poll(keys,2,false,false));
        }
        [Test] public void NewDirectionWinsOverPreviousHeldDirection()
        {
            var keys=new Keys();keys.Set(KeyCode.UpArrow);var input=new StudyInput();input.Poll(keys,0,true,false);
            keys.pressed.Clear();keys.held.Add(KeyCode.D);keys.pressed.Add(KeyCode.D);
            Assert.AreEqual(StudyAction.Right,input.Poll(keys,.05f,true,false));
        }
        [Test] public void MenuHasQuarterSecondInitialDelayThenPointTwelveRepeat()
        {
            var keys=new Keys();keys.Set(KeyCode.RightArrow);var input=new StudyInput();
            Assert.AreEqual(StudyAction.Right,input.Poll(keys,0,false,false));keys.pressed.Clear();
            Assert.IsNull(input.Poll(keys,.249f,false,false));
            Assert.AreEqual(StudyAction.Right,input.Poll(keys,.251f,false,false));
            Assert.IsNull(input.Poll(keys,.369f,false,false));
            Assert.AreEqual(StudyAction.Right,input.Poll(keys,.372f,false,false));
        }
    }
}
