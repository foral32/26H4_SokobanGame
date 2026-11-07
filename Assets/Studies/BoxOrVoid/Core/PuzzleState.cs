using System;
using System.Text;

namespace BoxOrVoid.Core
{
    public sealed class PuzzleState
    {
        internal readonly Piece[] Pieces;
        public PuzzleDefinition Definition { get; }
        public Tone ActiveTone { get; }
        public int PieceCount => Pieces.Length;
        public Piece GetPiece(int index) => Pieces[index];
        internal PuzzleState(PuzzleDefinition definition, Piece[] pieces, Tone active)
        { Definition = definition; Pieces = pieces; ActiveTone = active; }
        internal PuzzleState Copy(Tone active) => new PuzzleState(Definition, (Piece[])Pieces.Clone(), active);
        public int PlayerIndex(Tone tone)
        {
            for (int i = 0; i < Pieces.Length; i++)
                if (Pieces[i].Kind == ObjectKind.Player && Pieces[i].Tone == tone) return i;
            return -1;
        }
        public int TopAt(Cell cell)
        {
            int result = -1, depth = -1;
            for (int i = 0; i < Pieces.Length; i++)
                if (Pieces[i].Cell == cell && Pieces[i].Depth > depth) { result = i; depth = Pieces[i].Depth; }
            return result;
        }
        public bool GoalFulfilled(int goalIndex)
        {
            var goal = Definition.Goals[goalIndex];
            int count = 0, candidate = -1;
            for (int i = 0; i < Pieces.Length; i++)
                if (Pieces[i].Cell == goal.Cell) { count++; candidate = i; }
            return count == 1 && Pieces[candidate].Kind == goal.RequiredKind && Pieces[candidate].Tone == goal.Tone;
        }
        public int UnderTopAt(Cell cell)
        {
            int top=TopAt(cell),result=-1,depth=-1;
            for(int i=0;i<Pieces.Length;i++)
                if(i!=top && Pieces[i].Cell==cell && Pieces[i].Depth>depth) {result=i;depth=Pieces[i].Depth;}
            return result;
        }
        public bool IsSolved
        {
            get
            {
                for (int i = 0; i < Definition.Goals.Count; i++) if (!GoalFulfilled(i)) return false;
                return Definition.Goals.Count > 0;
            }
        }
        // Facing is cosmetic; depth is normalized because only stack order affects rules.
        public string PositionKey()
        {
            var text = new StringBuilder().Append((int)ActiveTone).Append('|');
            for (int i = 0; i < Pieces.Length; i++)
            {
                var p = Pieces[i]; int rank = 0;
                for (int j = 0; j < Pieces.Length; j++) if (Pieces[j].Cell == p.Cell && Pieces[j].Depth < p.Depth) rank++;
                text.Append(p.Cell.X).Append(',').Append(p.Cell.Y).Append(',').Append(rank).Append(';');
            }
            return text.ToString();
        }
    }
}
