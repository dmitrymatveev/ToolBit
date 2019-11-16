using NUnit.Framework;
using System;
using TechTalk.SpecFlow;
using Toolbit.Parsers;

namespace Toolbit.Specs.Parsers.Commander
{
    [Binding]
    [Scope(Feature = "Commander")]
    public class CommanderSteps
    {
        Type Type;
        Toolbit.Parsers.Commander TargetCommander;
        Command Command;

        string LastInput;
        string LastOutput;

        [Given(@"A Commander definition type '(.*)'")]
        public void GivenACommanderDefinitionType(string type)
        {
            Type = Type.GetType(type);
            Assert.IsNotNull(Type, "Target type not found. Did you use an assembly-qualified name of the type?");
            TargetCommander = Toolbit.Parsers.Commander.Create(Type);
            SpecsCommanderTest.OnMethodInvoked = str => LastOutput = str;
        }

        [Then(@"Command '(.*)' should exist")]
        public void ThenCommandShouldExist(string name)
        {
            Assert.AreEqual(true, TargetCommander.Commands.ContainsKey(name), $"Command with name '{name}' not found.");
        }

        [When(@"I inspect command '(.*)'")]
        public void WhenInspectingCommandRecord(string commandName)
        {
            var success = TargetCommander.Commands.TryGetValue(commandName, out Command command);
            Assert.IsTrue(success, $"Command not found. Did you mis-spell '${commandName}'");
            Command = command;
        }

        [Then(@"It's property '(.*)' should equal to '(.*)'")]
        public void ThenCommandHasProperty(string paramName, string paramValue)
        {
            Assert.AreEqual(
                Command.GetType().GetProperty(paramName).GetValue(Command), 
                paramValue,
                $"Property '{paramName}' does not match value '{paramValue}'"
                );
        }

        [When(@"Input string is '(.*)'")]
        public void GivenAnInputString(string input)
        {
            LastInput = input;
            Assert.AreEqual(
                true,
                TargetCommander.Invoke(input),
                $"Input '{input}' did not match any commands."
                );
        }

        [Then(@"Result is '(.*)'")]
        public void ResultIs(string expected)
        {
            Assert.AreEqual(expected, LastOutput, $"For input '{LastInput}'");
        }
    }
}
