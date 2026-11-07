using System;
using System.Collections.Generic;
using BoxOrVoid.Core;

namespace BoxOrVoid.Tests
{
    public static class CoreRuleSpecs
    {
        public static readonly Dictionary<string, Action> Cases = new Dictionary<string, Action>
        {
            { "OppositeColorSupportsPlayer", OppositeColorSupportsPlayer },
            { "SameColorAndGrayBlockWithoutUndoEntry", SameColorAndGrayBlockWithoutUndoEntry },
            { "CoveredPlayerCannotMoveButCanSwitch", CoveredPlayerCannotMoveButCanSwitch },
            { "SameColorBoxChainMovesAtomically", SameColorBoxChainMovesAtomically },
            { "BlockedChainDoesNotPartiallyMove", BlockedChainDoesNotPartiallyMove },
            { "OppositeBoxIsSupportAndRemainsUnderneath", OppositeBoxIsSupportAndRemainsUnderneath },
            { "GoalRequiresExactlyOneMatchingPiece", GoalRequiresExactlyOneMatchingPiece },
            { "ThreeLayerStackExposesOnlyImmediateUnderPiece", ThreeLayerStackExposesOnlyImmediateUnderPiece },
            { "SwitchUndoRestoresControlAndBoard", SwitchUndoRestoresControlAndBoard },
            { "ResetCanBeUndoneAlongWithEarlierMoves", ResetCanBeUndoneAlongWithEarlierMoves },
            { "RepeatedResetReplacesRecoveryCheckpoint", RepeatedResetReplacesRecoveryCheckpoint },
            { "EmptyCellAllowsEitherColor", EmptyCellAllowsEitherColor },
            { "InvalidDataIsRejected", InvalidDataIsRejected }
        };
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        private static CellObjectData Obj(ObjectKind kind, Tone tone) => new CellObjectData { ObjectType = kind, Color = tone };
        private static LevelData Fixture()
        {
            var d = new LevelData { width = 8, height = 3, grid = new CellData[24] };
            for (int i = 0; i < 24; i++) d.grid[i] = new CellData { Objects = new[] { Obj(ObjectKind.Wall, i / 8 == 1 ? Tone.Black : Tone.Gray) } };
            Set(d,1,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.Player,Tone.White));
            Set(d,6,Obj(ObjectKind.Wall,Tone.White),Obj(ObjectKind.Player,Tone.Black));
            Set(d,5,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.PlayerGoal,Tone.White));
            return d;
        }
        private static void Set(LevelData d, int x, params CellObjectData[] objects) { d.grid[8+x] = new CellData { Objects=objects }; }
        private static PuzzleSession Session(LevelData d) => new PuzzleSession(PuzzleDefinition.FromData(d));
        private static Cell Player(PuzzleState s, Tone tone) => s.GetPiece(s.PlayerIndex(tone)).Cell;
        private static void OppositeColorSupportsPlayer()
        {
            var s=Session(Fixture()); var r=s.Apply(Command.Right);
            Check(r.Kind==ActionKind.Moved && Player(s.State,Tone.White)==new Cell(2,1),"White must walk on black.");
            Check(Player(r.Before,Tone.White)==new Cell(1,1),"A transition must preserve the earlier snapshot.");
        }
        private static void SameColorAndGrayBlockWithoutUndoEntry()
        {
            var d=Fixture(); Set(d,2,Obj(ObjectKind.Wall,Tone.White)); var s=Session(d); var key=s.State.PositionKey();
            Check(s.Apply(Command.Right).Kind==ActionKind.Blocked,"White wall must block white.");
            Check(s.Apply(Command.Up).Kind==ActionKind.Blocked,"Gray wall must block both colors.");
            Check(s.UndoCount==0 && s.State.PositionKey()==key,"Blocked move must not move pieces or add history.");
        }
        private static void CoveredPlayerCannotMoveButCanSwitch()
        {
            var d=Fixture(); Set(d,2,Obj(ObjectKind.Wall,Tone.White),Obj(ObjectKind.Player,Tone.Black)); Set(d,3,Obj(ObjectKind.Wall,Tone.White)); Set(d,6,Obj(ObjectKind.Wall,Tone.White));
            var s=Session(d); s.Apply(Command.Right); s.Apply(Command.Switch);
            Check(s.Apply(Command.Left).Kind==ActionKind.Covered,"A covered black player cannot leave the stack.");
            s.Apply(Command.Switch); s.Apply(Command.Left); s.Apply(Command.Switch);
            Check(s.Apply(Command.Right).Changed,"Uncovering restores the player's movement.");
        }
        private static void SameColorBoxChainMovesAtomically()
        {
            var d=Fixture(); Set(d,2,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.Box,Tone.White)); Set(d,3,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.Box,Tone.White));
            var s=Session(d); var key=s.State.PositionKey(); var r=s.Apply(Command.Right);
            Check(r.Kind==ActionKind.Pushed && r.PushedCount==2,"Both white boxes must move in one turn.");
            Check(Player(s.State,Tone.White)==new Cell(2,1) && s.State.TopAt(new Cell(4,1))>=0,"Chain end must advance.");
            s.Apply(Command.Undo); Check(s.State.PositionKey()==key,"One undo must restore the entire push.");
        }
        private static void BlockedChainDoesNotPartiallyMove()
        {
            var d=Fixture(); Set(d,2,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.Box,Tone.White)); Set(d,3,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.Box,Tone.White)); Set(d,4,Obj(ObjectKind.Wall,Tone.Gray));
            var s=Session(d); var key=s.State.PositionKey();
            Check(s.Apply(Command.Right).Kind==ActionKind.Blocked && s.State.PositionKey()==key,"A blocked chain must be atomic.");
        }
        private static void OppositeBoxIsSupportAndRemainsUnderneath()
        {
            var d=Fixture(); Set(d,2,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.Box,Tone.White)); Set(d,3,Obj(ObjectKind.Wall,Tone.White),Obj(ObjectKind.Box,Tone.Black));
            var s=Session(d); s.Apply(Command.Right); int top=s.State.TopAt(new Cell(3,1));
            Check(top>=0 && s.State.GetPiece(top).Tone==Tone.White,"White box must stack over the black box.");
            int count=0; for(int i=0;i<s.State.PieceCount;i++) if(s.State.GetPiece(i).Cell==new Cell(3,1)) count++;
            Check(count==2,"Opposite box must remain in place under the moved box.");
        }
        private static void GoalRequiresExactlyOneMatchingPiece()
        {
            var d=Fixture(); Set(d,2,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.PlayerGoal,Tone.White)); Set(d,5,Obj(ObjectKind.Wall,Tone.Black));
            var s=Session(d); s.Apply(Command.Right); Check(s.State.IsSolved,"Matching player alone must fulfill its goal.");
            Set(d,2,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.BoxGoal,Tone.White));
            s=Session(d);s.Apply(Command.Right);Check(!s.State.IsSolved,"Player cannot fulfill a box goal.");
            Set(d,2,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.PlayerGoal,Tone.White),Obj(ObjectKind.Box,Tone.Black));
            s=Session(d);s.Apply(Command.Right);Check(!s.State.IsSolved,"Matching top player cannot fulfill a goal with another piece below.");
        }
        private static void SwitchUndoRestoresControlAndBoard()
        {
            var s=Session(Fixture());var key=s.State.PositionKey();
            Check(s.Apply(Command.Switch).Kind==ActionKind.Switched && s.State.ActiveTone==Tone.Black,"Switch must select black.");
            s.Apply(Command.Undo);Check(s.State.PositionKey()==key,"Undo must include control color.");
        }
        private static void ThreeLayerStackExposesOnlyImmediateUnderPiece()
        {
            var d=Fixture();Set(d,2,Obj(ObjectKind.Wall,Tone.Black),Obj(ObjectKind.BoxGoal,Tone.White),Obj(ObjectKind.Box,Tone.White),Obj(ObjectKind.Box,Tone.Black),Obj(ObjectKind.Box,Tone.White));
            var state=Session(d).State;var cell=new Cell(2,1);
            Check(state.GetPiece(state.TopAt(cell)).Tone==Tone.White,"The third layer is the top white box.");
            Check(state.GetPiece(state.UnderTopAt(cell)).Tone==Tone.Black,"Only the second layer should supply the overlap badge.");
            Check(!state.GoalFulfilled(0),"Three pieces cannot fulfill the covered goal.");
        }
        private static void ResetCanBeUndoneAlongWithEarlierMoves()
        {
            var s=Session(Fixture());var initial=s.State.PositionKey();s.Apply(Command.Right);s.Apply(Command.Switch);var prior=s.State.PositionKey();
            Check(prior!=initial && s.UndoCount==2,"Fixture must have moved and switched before testing reset.");
            s.Apply(Command.Reset);Check(s.State.PositionKey()==initial,"Reset restores initial state.");
            s.Apply(Command.Undo);Check(s.State.PositionKey()==prior,"Undo after reset restores the pre-reset checkpoint.");
            s.Apply(Command.Undo);Check(s.State.ActiveTone==Tone.White,"Earlier move history must survive undoing reset.");
        }
        private static void EmptyCellAllowsEitherColor()
        {
            var d=Fixture();Set(d,2);var s=Session(d);
            Check(s.Apply(Command.Right).Changed,"An empty cell is traversable in the reference runtime.");
        }
        private static void RepeatedResetReplacesRecoveryCheckpoint()
        {
            var s=Session(Fixture());var initial=s.State.PositionKey();
            s.Apply(Command.Right);s.Apply(Command.Reset);s.Apply(Command.Reset);s.Apply(Command.Undo);
            Check(s.State.PositionKey()==initial && s.UndoCount==0,"A second reset captures the current initial board, replacing the older checkpoint.");
            Check(s.Apply(Command.Undo).Kind==ActionKind.NoChange,"The overwritten checkpoint must not resurrect earlier moves.");
        }
        private static void InvalidDataIsRejected()
        {
            var d=Fixture();d.height=8;
            try { PuzzleDefinition.FromData(d); } catch(ArgumentException) { return; }
            throw new Exception("Mismatched cell counts must be rejected.");
        }
    }
}
