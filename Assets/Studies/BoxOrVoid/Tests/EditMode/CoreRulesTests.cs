using System.Collections.Generic;
using NUnit.Framework;

namespace BoxOrVoid.Tests
{
    public sealed class CoreRulesTests
    {
        public static IEnumerable<string> Cases => CoreRuleSpecs.Cases.Keys;
        [TestCaseSource(nameof(Cases))]
        public void RulesMatchReference(string name) => CoreRuleSpecs.Cases[name]();
    }
}
