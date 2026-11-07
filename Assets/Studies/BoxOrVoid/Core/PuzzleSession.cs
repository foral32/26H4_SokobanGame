using System;
using System.Collections.Generic;

namespace BoxOrVoid.Core
{
    public enum ActionKind { Moved, Pushed, Switched, Blocked, Covered, Undone, Reset, NoChange }
    public readonly struct ActionResult
    {
        public readonly ActionKind Kind;
        public readonly PuzzleState Before;
        public readonly PuzzleState After;
        public readonly int PushedCount;
        public bool Changed => Kind == ActionKind.Moved || Kind == ActionKind.Pushed || Kind == ActionKind.Switched || Kind == ActionKind.Undone || Kind == ActionKind.Reset;
        public ActionResult(ActionKind kind, PuzzleState before, PuzzleState after, int pushed = 0)
        { Kind = kind; Before = before; After = after; PushedCount = pushed; }
    }

    public static class PuzzleRules
    {
        private static readonly Cell[] Offsets = { new Cell(0, 1), new Cell(1, 0), new Cell(0, -1), new Cell(-1, 0) };

        public static ActionResult Apply(PuzzleState state, Command command)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == Command.Switch)
                return new ActionResult(ActionKind.Switched, state, state.Copy(state.ActiveTone == Tone.White ? Tone.Black : Tone.White));
            if (command < Command.Up || command > Command.Left) throw new ArgumentException("Undo/reset belong to PuzzleSession.");
            int playerIndex = state.PlayerIndex(state.ActiveTone);
            var player = state.Pieces[playerIndex];
            if (state.TopAt(player.Cell) != playerIndex)
                return new ActionResult(ActionKind.Covered, state, state);

            var direction = Offsets[(int)command];
            var order = new List<int>();
            var next = state.Copy(state.ActiveTone);
            // Bumping changes the facing in the reference, but does not add an undo step.
            next.Pieces[playerIndex] = player.Turned(command);
            if (!PlanMove(state, playerIndex, direction, order))
                return new ActionResult(ActionKind.Blocked, state, next);

            // Dependencies are planned without mutation, then committed furthest-first.
            foreach (int index in order)
            {
                var piece = next.Pieces[index];
                var cell = piece.Cell + direction;
                int depth = 0;
                for (int j = 0; j < next.Pieces.Length; j++)
                    if (j != index && next.Pieces[j].Cell == cell) depth = Math.Max(depth, next.Pieces[j].Depth);
                next.Pieces[index] = piece.Moved(cell, depth + 1, piece.Kind == ObjectKind.Player ? command : piece.Facing);
            }
            int pushed = order.Count - 1;
            return new ActionResult(pushed > 0 ? ActionKind.Pushed : ActionKind.Moved, state, next, pushed);
        }

        private static bool PlanMove(PuzzleState state, int index, Cell direction, List<int> order)
        {
            var piece = state.Pieces[index];
            var target = piece.Cell + direction;
            int top = state.TopAt(target);
            Tone color = top >= 0 ? state.Pieces[top].Tone : state.Definition.WallAt(target);
            if (color == Tone.Gray) return false;
            if (color == piece.Tone)
            {
                if (top < 0 || state.Pieces[top].Kind != ObjectKind.Box) return false;
                if (!PlanMove(state, top, direction, order)) return false;
            }
            order.Add(index);
            return true;
        }
    }

    public sealed class PuzzleSession
    {
        private readonly Stack<PuzzleState> history = new Stack<PuzzleState>();
        private PuzzleState resetCheckpoint;
        private PuzzleState[] checkpointHistory;
        public PuzzleState State { get; private set; }
        public int UndoCount => history.Count;
        public PuzzleSession(PuzzleDefinition definition) { State = definition.CreateInitialState(); }
        public ActionResult Apply(Command command)
        {
            var before = State;
            if (command == Command.Undo)
            {
                if (history.Count > 0) State = history.Pop();
                else if (resetCheckpoint != null)
                {
                    State = resetCheckpoint;
                    for (int i = checkpointHistory.Length - 1; i >= 0; i--) history.Push(checkpointHistory[i]);
                    resetCheckpoint = null;
                    checkpointHistory = null;
                }
                else return new ActionResult(ActionKind.NoChange, State, State);
                return new ActionResult(ActionKind.Undone, before, State);
            }
            if (command == Command.Reset)
            {
                resetCheckpoint = State;
                checkpointHistory = history.ToArray();
                history.Clear();
                State = State.Definition.CreateInitialState();
                return new ActionResult(ActionKind.Reset, before, State);
            }
            var result = PuzzleRules.Apply(State, command);
            if (result.Changed) history.Push(State);
            State = result.After;
            return result;
        }
    }
}
