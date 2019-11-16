using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Toolbit.Parsers
{
    /// <summary>
    /// Marks target method as a command handler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Alias;
        public string Description;
    }

    /// <summary>
    /// Object representation of a Command method.
    /// </summary>
    public class Command
    {
        public string Name { get; internal set; }
        public string Alias { get; internal set; }
        public string Description { get; internal set; }

        internal object[] CurrentParsedArgs;
        internal List<Callback> Methods = new List<Callback>();

        internal Command(string name, string alias, string description)
        {
            Name = name;
            Alias = alias;
            Description = description;
        }
    }

    /// <summary>
    /// Given a type, this wrapper will translate all public static methods which are marked by 
    /// a CommandAttribute into a list of commands that can be invoked using a string value.
    /// </summary>
    public class Commander
    {
        public ReadOnlyDictionary<string, Command> Commands { get; private set; }

        private Commander(ReadOnlyDictionary<string, Command> commands)
        {
            Commands = commands;
        }

        public static Commander Create<T>() => Commander.Create(typeof(T));

        public static Commander Create(Type type)
        {
            // we store a dictionary of all found commands
            var commands = new Dictionary<string, Command>();

            // search in all public static methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var method in methods)
            {
                // Skip all methods that do not declare as CommandAttribute
                var attr = (CommandAttribute) Attribute.GetCustomAttribute(method, typeof(CommandAttribute));
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

                var defaultAlias = method.Name.ToLower();
                var callback = new Callback(
                    method: method,
                    parameters: parameters.AsReadOnly()
                    );

                // Find existing list for this Command overloaded methods
                if (!commands.TryGetValue(defaultAlias, out Command command))
                {
                    // Create instance of command
                    command = new Command(
                        name: defaultAlias,
                        alias: attr.Alias ?? null,
                        description: attr.Description ?? null
                        );
                    commands.Add(defaultAlias, command);
                }

                // Add this command to list of existing overloaded methods
                command.Methods.Add(callback);
                command.Alias = attr.Alias ?? command.Alias;
                command.Description = attr.Description ?? command.Description;

                // When Alias is specified attempt to create a separate key/value pair in the existing invokation map
                if (attr.Alias != null && commands.TryGetValue(attr.Alias, out Command groupedAlias))
                {
                    if (groupedAlias == command)
                    {
                        throw new CommanderDefinitionException($"Can not re-define existing alias " +
                            $"'{command.Alias}'. Did you try to define Command on overloaded method?");
                    }
                    else
                    {
                        throw new CommanderDefinitionException($"Can not re-define existing alias");
                    }
                }
                else if(attr.Alias != null)
                {
                    commands.Add(attr.Alias, command);
                }

                // Register default aliases for the command
                if (!commands.ContainsKey(method.Name))
                {
                    commands.Add(method.Name, command);
                }
            }

            return new Commander(new ReadOnlyDictionary<string, Command>(commands));
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

            var command = Commands.GetValueOrDefault(name);
            if (command == null) return false;

            command
                .Methods
                .FirstOrDefault(cb => cb.TryParseArguments(args, ref command.CurrentParsedArgs))
                ?.Invoke(command.CurrentParsedArgs);

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

    #region Internal wrapper classes around reflection info
    class Parameter
    {
        public bool IsNullable;
        public bool HasDefaultValue;
        public object DefaultValue;
        public ParamArrayAttribute Params;
        public Type Type;
    }

    class Callback
    {
        internal static readonly int PARAMS = 1000;

        private readonly MethodInfo Method;
        private readonly IReadOnlyList<Parameter> Parameters;

        private readonly Lazy<int> RequiredCount;
        private readonly Lazy<int> OptionalCount;
        private readonly Lazy<int> TotalCount;

        internal Callback(MethodInfo method, IReadOnlyList<Parameter> parameters)
        {
            Method = method;
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
        /// <param name="currentParsedArgs">Parsed parameters from input string</param>
        /// <returns>True if successful match is found and False otherwise.</returns>
        internal bool TryParseArguments(string[] input, ref object[] currentParsedArgs)
        {
            var total = TotalCount.Value;
            var len = input.Length;

            object[] result;
            if ((IsEqualOrParams(len, total) || IsRequiredOrOptional(len)) && TryParseSignature(input, out result))
            {
                currentParsedArgs = result;
                return true;
            }
            else if (IsLessThenOptional(len, total) && TryParseSignature(input, out result))
            {
                return true;
            }

            currentParsedArgs = null;
            // otherwise reject
            return false;
        }

        /// <summary>
        /// Invokes current callback
        /// </summary>
        /// <param name="currentParsedArgs">Array of parsed arguments</param>
        internal void Invoke(object[] currentParsedArgs)
        {
            Method.Invoke(null, currentParsedArgs);
        }

        #region Private helper methods
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

        // equal count OR command is using params operator
        private bool IsEqualOrParams(int len, int total) => (len == total || total >= PARAMS);
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
                if (arg.Params != null) return PARAMS;
                else count += arg.HasDefaultValue ? 1 : 0;
            return count;
        }

        private int GetTotalCount()
        {
            return RequiredCount.Value + OptionalCount.Value;
        }
        #endregion
    }
    #endregion
}
