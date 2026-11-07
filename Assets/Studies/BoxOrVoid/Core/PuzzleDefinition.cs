using System;
using System.Collections.Generic;

namespace BoxOrVoid.Core
{
    public enum Tone { None, White, Black, Gray }
    public enum ObjectKind { Empty, Wall, Box, BoxGoal, PlayerGoal, Player }
    public enum Command { Up, Right, Down, Left, Switch, Undo, Reset }

    [Serializable]
    public sealed class LevelData
    {
        public int width;
        public int height;
        public CellData[] grid;
    }

    [Serializable]
    public sealed class CellData { public CellObjectData[] Objects; }

    [Serializable]
    public sealed class CellObjectData
    {
        public ObjectKind ObjectType;
        public Tone Color;
    }

    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public static Cell operator +(Cell a, Cell b) => new Cell(a.X + b.X, a.Y + b.Y);
        public override string ToString() => X + "," + Y;
    }

    public readonly struct Piece
    {
        public readonly ObjectKind Kind;
        public readonly Tone Tone;
        public readonly Cell Cell;
        public readonly int Depth;
        public readonly Command Facing;
        public Piece(ObjectKind kind, Tone tone, Cell cell, int depth, Command facing = Command.Right)
        { Kind = kind; Tone = tone; Cell = cell; Depth = depth; Facing = facing; }
        public Piece Moved(Cell cell, int depth, Command facing) => new Piece(Kind, Tone, cell, depth, facing);
        public Piece Turned(Command facing) => new Piece(Kind, Tone, Cell, Depth, facing);
    }

    public readonly struct Goal
    {
        public readonly ObjectKind RequiredKind;
        public readonly Tone Tone;
        public readonly Cell Cell;
        public Goal(ObjectKind kind, Tone tone, Cell cell) { RequiredKind = kind; Tone = tone; Cell = cell; }
    }

    public sealed class PuzzleDefinition
    {
        private readonly Tone[] walls;
        private readonly Piece[] initialPieces;
        private readonly IReadOnlyList<Goal> goals;
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Goal> Goals => goals;

        private PuzzleDefinition(int width, int height, Tone[] walls, Piece[] pieces, Goal[] goals)
        { Width = width; Height = height; this.walls = walls; initialPieces = pieces; this.goals = Array.AsReadOnly(goals); }

        public Tone WallAt(Cell cell) => cell.X < 0 || cell.Y < 0 || cell.X >= Width || cell.Y >= Height
            ? Tone.None : walls[cell.Y * Width + cell.X];

        public PuzzleState CreateInitialState() => new PuzzleState(this, (Piece[])initialPieces.Clone(), Tone.White);

        public static PuzzleDefinition FromData(LevelData data)
        {
            if (data == null || data.width < 1 || data.height < 1 || data.width > 256 || data.height > 256 ||
                data.grid == null || data.grid.Length != data.width * data.height)
                throw new ArgumentException("Invalid grid dimensions or cell count.");
            var walls = new Tone[data.grid.Length];
            var pieces = new List<Piece>();
            var goals = new List<Goal>();
            var playerCount = new int[4];
            for (int i = 0; i < data.grid.Length; i++)
            {
                var cell = new Cell(i % data.width, i / data.width);
                var objects = data.grid[i]?.Objects ?? Array.Empty<CellObjectData>();
                int depth = 0;
                foreach (var item in objects)
                {
                    if (item == null || !Enum.IsDefined(typeof(ObjectKind), item.ObjectType) || !Enum.IsDefined(typeof(Tone), item.Color))
                        throw new ArgumentException("Unsupported object or color at " + cell);
                    if (item.ObjectType == ObjectKind.Empty) continue;
                    if (item.ObjectType == ObjectKind.Wall)
                    {
                        if (walls[i] != Tone.None || depth != 0 || item.Color == Tone.None)
                            throw new ArgumentException("Invalid base wall at " + cell);
                        walls[i] = item.Color;
                    }
                    else
                    {
                        if (item.Color != Tone.White && item.Color != Tone.Black)
                            throw new ArgumentException("Playable objects must be white or black.");
                        if (walls[i] == Tone.Gray) throw new ArgumentException("Object on gray wall at " + cell);
                        if (item.ObjectType == ObjectKind.Player || item.ObjectType == ObjectKind.Box)
                        {
                            pieces.Add(new Piece(item.ObjectType, item.Color, cell, ++depth));
                            if (item.ObjectType == ObjectKind.Player && ++playerCount[(int)item.Color] > 1)
                                throw new ArgumentException("This study supports one player of each color.");
                        }
                        else goals.Add(new Goal(item.ObjectType == ObjectKind.PlayerGoal ? ObjectKind.Player : ObjectKind.Box, item.Color, cell));
                    }
                }
            }
            if (playerCount[1] != 1 || playerCount[2] != 1) throw new ArgumentException("Both player colors are required.");
            if (goals.Count == 0) throw new ArgumentException("At least one goal is required.");
            return new PuzzleDefinition(data.width, data.height, walls, pieces.ToArray(), goals.ToArray());
        }
    }
}
