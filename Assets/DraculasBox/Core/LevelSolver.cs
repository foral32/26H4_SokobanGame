using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DraculasBox.Core
{
    public enum SearchOutcome { Solved, Unsolvable, Limit, Invalid }

    public sealed class SearchResult
    {
        public readonly SearchOutcome Outcome;
        public readonly string Route, Error;
        public readonly int Pushes, Visited;
        internal readonly LevelData SolvedBoard;

        internal SearchResult(SearchOutcome outcome, int visited, string route = null, int pushes = 0, string error = null, LevelData solvedBoard=null)
        { Outcome=outcome; Visited=visited; Route=route; Pushes=pushes; Error=error; SolvedBoard=solvedBoard; }
    }

    public static class LevelSolver
    {
        public const string Directions = "URDL";
        public static bool Proves(LevelData data, string route, out int pushes, CancellationToken token=default)
        {
            pushes=0;
            if(string.IsNullOrEmpty(route))return false;
            try { return Trace(new Definition(data),route,out pushes,token); }
            catch(ArgumentException) { return false; }
        }

        private static bool Trace(Definition definition, string route, out int pushes, CancellationToken token,
            Stopwatch clock=null, int milliseconds=0)
        {
            pushes=0;
            var state=definition.Initial();
            foreach(char key in route)
            {
                token.ThrowIfCancellationRequested();
                if(clock!=null&&clock.ElapsedMilliseconds>=milliseconds)return false;
                int direction=Directions.IndexOf(key);
                if(direction<0||state.Won)return false;
                var move=Rules.Move(state,(Command)direction);
                if(!move.Changed||move.After.Dead)return false;
                if(move.PushedCount>0)pushes++;
                state=move.After;
            }
            return state.Won;
        }

        private sealed class Node
        {
            public State state;
            public string key;
            public int depth, parent, pushes;
            public char move;
        }

        // Facing is cosmetic. Door state and exposure follow from these positions on a fixed board.
        private static string Key(State state)
        {
            int width=state.Level.Width;
            var cells=new char[state.Level.PieceCount+1];
            cells[0]=(char)(state.Player.y*width+state.Player.x);
            for(int i=1;i<cells.Length;i++){var p=state.Position(i-1);cells[i]=(char)(p.y*width+p.x);}
            return new string(cells);
        }

        private static int[] FloorDistances(Definition definition)
        {
            var distance=new int[definition.Width*definition.Height];
            for(int i=0;i<distance.Length;i++)distance[i]=-1;
            int At(Cell p)=>p.y*definition.Width+p.x;
            var queue=new Queue<Cell>();
            queue.Enqueue(definition.Coffin);distance[At(definition.Coffin)]=0;
            while(queue.Count>0)
            {
                var current=queue.Dequeue();
                foreach(var direction in LightField.Directions)
                {
                    var next=current+direction;
                    if(!definition.Floor(next)||distance[At(next)]>=0)continue;
                    distance[At(next)]=distance[At(current)]+1;
                    queue.Enqueue(next);
                }
            }
            return distance;
        }

        // A* counts every successful walk or push as one step. Ignoring hazards and objects
        // gives a lower bound even for a door that opens later or a push that moves several pieces.
        public static SearchResult Solve(LevelData data, CancellationToken token=default, int maxStates=65000, int milliseconds=6000)
        {
            token.ThrowIfCancellationRequested();
            var clock=Stopwatch.StartNew();
            Definition definition;
            try { definition=new Definition(data); }
            catch(ArgumentException e) { return new SearchResult(SearchOutcome.Invalid,0,error:e.Message); }
            var initial=definition.Initial();
            var distance=FloorDistances(definition);
            int Remaining(Cell p)=>distance[p.y*definition.Width+p.x];
            if(Remaining(initial.Player)<0)return new SearchResult(SearchOutcome.Unsolvable,1);
            var incumbent=data.solution;
            int incumbentPushes=0;
            if(!string.IsNullOrEmpty(incumbent)&&!Trace(definition,incumbent,out incumbentPushes,token,clock,milliseconds))
            {
                if(clock.ElapsedMilliseconds>=milliseconds)return new SearchResult(SearchOutcome.Limit,1);
                incumbent=null;
            }
            var nodes=new List<Node>();
            var bestDepth=new Dictionary<string,int>();
            // At equal estimates prefer the deeper node, then stable URDL discovery order.
            var frontier=new SortedSet<(int estimate,int negativeDepth,int index)>();
            void Add(State state, string key, int depth, int parent, char move, int pushes)
            {
                bestDepth[key]=depth;
                frontier.Add((depth+Remaining(state.Player),-depth,nodes.Count));
                nodes.Add(new Node{state=state,key=key,depth=depth,parent=parent,move=move,pushes=pushes});
            }
            Add(initial,Key(initial),0,-1,' ',0);
            while(frontier.Count>0)
            {
                token.ThrowIfCancellationRequested();
                var entry=frontier.Min;
                if(!string.IsNullOrEmpty(incumbent)&&entry.estimate>=incumbent.Length)
                    return new SearchResult(SearchOutcome.Solved,nodes.Count,incumbent,incumbentPushes,solvedBoard:definition.Export());
                var current=nodes[entry.index];
                var state=current.state;
                frontier.Remove(entry);
                current.state=null;
                if(bestDepth[current.key]!=current.depth)continue;
                current.key=null;
                if(state.Won)
                {
                    var path=new char[current.depth];
                    for(var n=current;n.parent>=0;n=nodes[n.parent])path[n.depth-1]=n.move;
                    return new SearchResult(SearchOutcome.Solved,nodes.Count,new string(path),current.pushes,solvedBoard:definition.Export());
                }
                if(clock.ElapsedMilliseconds>=milliseconds)return new SearchResult(SearchOutcome.Limit,nodes.Count,incumbent,incumbentPushes);
                for(int i=0;i<4;i++)
                {
                    var move=Rules.Move(state,(Command)i);
                    if(!move.Changed||move.After.Dead)continue;
                    string key=Key(move.After);
                    if(bestDepth.TryGetValue(key,out int depth)&&depth<=current.depth+1)continue;
                    if(nodes.Count>=maxStates)return new SearchResult(SearchOutcome.Limit,nodes.Count,incumbent,incumbentPushes);
                    Add(move.After,key,current.depth+1,entry.index,Directions[i],current.pushes+(move.PushedCount>0?1:0));
                }
            }
            return new SearchResult(SearchOutcome.Unsolvable,nodes.Count);
        }
    }
}
