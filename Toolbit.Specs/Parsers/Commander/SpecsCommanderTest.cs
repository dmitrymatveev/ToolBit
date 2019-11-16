using System;
using System.Reflection;
using Toolbit.Parsers;

namespace Toolbit.Specs.Parsers.Commander
{
    class SpecsCommanderTest
    {
        public static Action<string> OnMethodInvoked;

        /*
         * This is a command handler method
         * You can invoke it using it's alias or method name following any arguments.
         * 
         * Only public static methods are checked and they can not return any value
         * as well as `out` and `ref` keywords can not be used.
         * 
         * Ex:
         *      singleaction
         *      SingleAction
         *      sa
         */
        [Command(Alias = "sa", Description = "An action handler")]
        public static void SingleAction() => OnMethodInvoked?.Invoke(MethodBase.GetCurrentMethod().ToString());

        /*
         * Commands can be overloaded.
         * Note: that you do not have to mark each overloaded method with a CommandAttribute.
         * 
         * Ex: 
         *      test
         *      tst
         */
        [Command(Alias = "do")]
        public static void DoThings() => OnMethodInvoked?.Invoke(MethodBase.GetCurrentMethod().ToString());
        /*
         * Overloaded methods are checked for a match in the order they are defined in a class.
         */
        public static void DoThings(string str) => OnMethodInvoked?.Invoke(MethodBase.GetCurrentMethod().ToString());
        /*
         * Care must be taken when defining overloaded methods of equal parameter length since
         * more more generic types will supersede all other types.
         * 
         * Eg: Strings will always match eagerley thus place them behind more specific type definitions.
         */
        public static void DoThings(string str, int i) => OnMethodInvoked?.Invoke(MethodBase.GetCurrentMethod().ToString());
        public static void DoThings(string a, string b) => OnMethodInvoked?.Invoke(MethodBase.GetCurrentMethod().ToString());
    }
}
