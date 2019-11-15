using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Toolbit.Parsers
{
    class Parameter
    {
        public bool IsNullable;
        public bool HasDefaultValue;
        public object DefaultValue;
        public ParamArrayAttribute Params;
        public Type Type;
    }

    public class Command
    {
        internal static readonly int PARAMS = 1000;

        public readonly string Name;
        public readonly string Alias;
        public readonly string Description;

        private  readonly MethodInfo Method;
        private readonly IReadOnlyList<Parameter> Parameters;

        private Lazy<int> RequiredCount;
        private Lazy<int> OptionalCount;
        private Lazy<int> TotalCount;

        private object[] CurrentParsedArgs;

        internal Command(string alias, string description, MethodInfo method, IReadOnlyList<Parameter> parameters)
        {
            Method = method;
            Name = method.Name;
            Alias = alias;
            Description = description;
            Parameters = parameters;

            RequiredCount = new Lazy<int>(GetRequiredCount);
            OptionalCount = new Lazy<int>(GetOptionalCount);
            TotalCount = new Lazy<int>(GetTotalCount);
        }

        /// <summary>
        /// Attempts to match given string to this Command method signature and
        /// keeps parsed arguments untill next invocation.
        /// </summary>
        /// <param name="input">Array of potential method arguments</param>
        /// <returns>True if successful match is found and False otherwise.</returns>
        public bool TryParseArguments(string[] input)
        {
            var total = TotalCount.Value;
            var len = input.Length;
            
            object[] result;
            if ((IsEqualOrParams(len, total) || IsRequiredOrOptional(len)) && TryParseSignature(input, out result))
            {
                CurrentParsedArgs = result;
                return true;
            }
            else if (IsLessThenOptional(len, total) && TryParseSignature(input, out result))
            {
                return true;
            }

            CurrentParsedArgs = null;
            // otherwise reject
            return false;
        }

        public void Invoke()
        {
            Method.Invoke(null, CurrentParsedArgs);
        }

        private bool TryParseSignature(string[] input, out object[] result)
        {
            result = input
                .Select((string str, int i) =>
                {
                    var param = Parameters[i];
                    return param.IsNullable ? null : Convert.ChangeType(str, param.Type);
                })
                .ToArray();
            return true;
        }

        #region Private helper methods

        // equal count OR command is using params operator
        private bool IsEqualOrParams(int len, int total) => (len == total || total >= Command.PARAMS);
        // matches required OR optional count
        private bool IsRequiredOrOptional(int len) => (RequiredCount.Value == len || OptionalCount.Value == len);
        // more then required but less then total
        private bool IsLessThenOptional(int len, int total) => (RequiredCount.Value < len && total > len);

        private int GetRequiredCount()
        {
            var count = 0;
            foreach (var arg in Parameters)
                count += !(arg.HasDefaultValue || arg.Params != null) ? 1 : 0;
            return count;
        }

        private int GetOptionalCount()
        {
            var count = 0;
            foreach (var arg in Parameters)
                if (arg.Params != null) return Command.PARAMS;
                else count += arg.HasDefaultValue ? 1 : 0;
            return count;
        }

        private int GetTotalCount()
        {
            return RequiredCount.Value + OptionalCount.Value;
        }
        #endregion
    }

    public class Commander
    {
        public readonly ReadOnlyDictionary<string, List<Command>> Commands;

        private Commander(ReadOnlyDictionary<string, List<Command>> commands)
        {
            Commands = commands;
        }

        public static Commander Create<T>() => Commander.Create(typeof(T));

        public static Commander Create(Type type)
        {
            // we store a dictionary of all found commands
            var commands = new Dictionary<string, List<Command>>();

            // search in all public static methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var method in methods)
            {
                // Skip all methods that do not declare as CommandAttribute
                CommandAttribute attr = (CommandAttribute) Attribute.GetCustomAttribute(method, typeof(CommandAttribute));
                if (attr == null) continue;

                // Collect method parameters information
                var parameters = new List<Parameter>();
                foreach (var param in method.GetParameters())
                {
                    parameters.Add(new Parameter()
                    {
                        IsNullable = IsNullable(param),
                        HasDefaultValue = param.HasDefaultValue,
                        DefaultValue = param.DefaultValue,
                        Params = Params(param),
                        Type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType,
                    }); ;
                }

                // Create instance of command
                var command = new Command(
                    method: method,
                    alias: attr.Alias ?? null,
                    description: attr.Description ?? null,
                    parameters: parameters.AsReadOnly()
                    );

                // Find existing list for this Command overloaded methods
                if (!commands.TryGetValue(command.Name, out List<Command> grouped))
                {
                    grouped = new List<Command>();
                    commands.Add(command.Name, grouped);
                }
                // Add this command to list of existing overloaded methods
                grouped.Add(command);

                // When Alias is specified attempt to create a separate key/value pair in the existing invokation map
                if (command.Alias != null && commands.TryGetValue(command.Alias, out List<Command> groupedAlias))
                {
                    if (groupedAlias == grouped)
                    {
                        throw new CommanderDefinitionException($"Can not re-define existing alias '{command.Alias}'. Did you try to define Command on overloaded method?");
                    }
                    else
                    {
                        throw new CommanderDefinitionException($"Can not re-define existing alias");
                    }
                }
                else if(command.Alias != null)
                {
                    commands.Add(command.Alias, grouped);
                }

                // Register default aliases for the command
                var defaultAlias = method.Name.ToLower();
                if (!commands.ContainsKey(defaultAlias))
                {
                    commands.Add(defaultAlias, grouped);
                }
            }

            return new Commander(new ReadOnlyDictionary<string, List<Command>>(commands));
        }

        /// <summary>
        /// Returns True when a matching method is found and invoked and False otherwise.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>True when a method is found and executed.</returns>
        public bool Invoke(string input)
        {
            string[] args = input.Split(" ");
            string name = args[0];
            args = args.Skip(1).ToArray();

            if (!Commands.ContainsKey(name)) return false;

            var command = Commands.GetValueOrDefault(name)
                .FirstOrDefault(cmd => cmd.TryParseArguments(args));

            if (command == null) return false;

            command.Invoke();
            return true;
        }

        #region Private helper methods
        static bool IsNullable(ParameterInfo type) => 
            Nullable.GetUnderlyingType(type.ParameterType) != null;

        static ParamArrayAttribute Params(ParameterInfo info) => 
            (ParamArrayAttribute)info.GetCustomAttributes(typeof(ParamArrayAttribute), false).FirstOrDefault();
        #endregion
    }

    public class CommanderDefinitionException : Exception
    {
        public CommanderDefinitionException(string message) : base(message)
        { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Alias;
        public string Description;

        public CommandAttribute()
        { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ArgumentAttribute : Attribute
    {
        public ArgumentAttribute(string name)
        { }
    }
}
