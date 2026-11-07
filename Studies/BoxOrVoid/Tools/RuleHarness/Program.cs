using BoxOrVoid.Tests;

if(args.Length>0 && args[0]=="solve") return LevelSolver.Run(Directory.GetCurrentDirectory(),args.Skip(1).ToArray());
if(args.Length>0 && args[0]=="trace") return LevelTrace.Run(Directory.GetCurrentDirectory(),args.Skip(1).ToArray());

int failed = 0;
foreach (var test in CoreRuleSpecs.Cases)
{
    try { test.Value(); Console.WriteLine("PASS " + test.Key); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + test.Key + ": " + ex.Message); }
}
Console.WriteLine($"{CoreRuleSpecs.Cases.Count-failed}/{CoreRuleSpecs.Cases.Count} rule cases passed.");
return failed == 0 ? 0 : 1;
