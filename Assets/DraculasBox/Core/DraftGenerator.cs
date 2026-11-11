using System;
using System.Linq;
using System.Threading;

namespace DraculasBox.Core
{
    [Flags] public enum DraftMechanic { None=0, Sun=1, Lamp=2, Circuit=4, Mirror=8 }
    public enum DraftComplexity { Intro, Standard, Advanced }

    public static class DraftGenerator
    {
        public static DraftPreset[] Choices(DraftMechanic allowed,DraftComplexity complexity)
        {
            if((allowed&~(DraftMechanic)15)!=0||!Enum.IsDefined(typeof(DraftComplexity),complexity))throw new ArgumentOutOfRangeException();
            if(allowed.HasFlag(DraftMechanic.Mirror))allowed|=DraftMechanic.Lamp;
            // Toggles allow mechanics; they do not demand one serial room per toggle.
            // Nonempty selection prefers presets that actually use a selected mechanic.
            var eligible=DraftPresets.All.Where(p=>(p.Mechanics&~allowed)==0&&(allowed==0?p.Mechanics==0:p.Mechanics!=0)).ToArray();
            int distance=eligible.Min(p=>Math.Abs((int)p.Complexity-(int)complexity));
            return eligible.Where(p=>Math.Abs((int)p.Complexity-(int)complexity)==distance).ToArray();
        }

        public static LevelData Generate(DraftMechanic allowed,DraftComplexity complexity,int seed,CancellationToken token=default,string previousTitle=null)
        {
            token.ThrowIfCancellationRequested();
            var choices=Choices(allowed,complexity);
            if(choices.Length>1)choices=choices.Where(p=>p.Title!=previousTitle).ToArray();
            var random=new Random(seed);
            var level=choices[random.Next(choices.Length)].Create(random.Next(8));
            // Validate the complete transformed board with the same rules as play.
            if(!LevelSolver.Proves(level,level.solution,out _,token))throw new InvalidOperationException("预设解法校验失败。");
            return level;
        }
    }
}
