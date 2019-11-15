using System;
using Toolbit.Parsers;

namespace Toolbit.Specs.Parsers.Commander
{
    class TestCommander
    {
        public static Action<string> OnMethodInvoked;

        [Command(Alias = "tz")]
        public static void TestZero()
        {
            OnMethodInvoked?.Invoke(string.Empty);
        }
    }
}
