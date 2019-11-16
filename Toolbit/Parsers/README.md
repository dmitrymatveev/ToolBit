# Commander

Commander is a command line parser that uses CommandAttribute and method signature to
match string input to it's corresponding handler method.

When matches are tryed for arguments are parsed into each method signature untill a one is
found.

Given a type, this wrapper will translate all public static methods which are marked by 

### Usage

	class MyCommands
    {
        /*
         * This is a command handler method
         * You can invoke it using it's alias or method name following any arguments.
         * 
         * Only public static methods are checked and they can not return any value
         * as well as `out` and `ref` keywords can not be used.
         * 
         * Ex:
         *      action
         *      Action
         *      ac
         */
        [Command(Alias = "ac", Description = "An action handler")]
        public static void Action() => { /*do things*/ };

        /*
         * Commands can be overloaded.
         * Note: that you do not have to mark each overloaded method with a CommandAttribute.
         * 
         * Ex: 
         *      dothings
         *      do
         */
        [Command(Alias = "do")]
        public static void DoThings() => { /*do things*/ };
        /*
         * Overloaded methods are checked for a match in the order they are defined in a class.
         */
        public static void DoThings(string str) => { /*do things*/ };
        /*
         * Care must be taken when defining overloaded methods of equal parameter length since
         * more more generic types will supersede all other types.
         * 
         * Eg: Strings will always match eagerley thus place them behind more specific type definitions.
         */
        public static void DoThings(string str, int i) => { /*do things*/ };
        public static void DoThings(string a, string b) => { /*do things*/ };
    }



    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter command:");
            var commander = Commander.Create<MyCommands>();
            while(commander.Invoke(Console.ReadLine()))
            {}
        }
    }