using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        List<Command> TargetCommandList;

        bool LastResult;
        string LastOutput;

        [Given(@"A Commander definition type '(.*)'")]
        public void GivenACommanderDefinitionType(string type)
        {
            Type = Type.GetType(type);
            Assert.IsNotNull(Type, "Target type not found. Did you use an assembly-qualified name of the type?");
        }

        [Given(@"I create an instance of Commander using given definition")]
        public void GivenICreateInstanceOfCommander()
        {
            TargetCommander = Toolbit.Parsers.Commander.Create(Type);
            TestCommander.OnMethodInvoked = str => LastOutput = str;
        }

        [Then(@"Command '(.*)' should exist")]
        public void ThenCommandShouldExist(string name)
        {
            Assert.AreEqual(true, TargetCommander.Commands.ContainsKey(name), $"Command with name '{name}' not found.");
        }

        [When(@"I inspect command '(.*)'")]
        public void WhenInspectingCommandRecord(string commandName)
        {
            var success = TargetCommander.Commands.TryGetValue(commandName, out List<Command> command);
            Assert.IsTrue(success, $"Command not found. Did you mis-spell '${commandName}'");
            TargetCommandList = command;
        }

        [Then(@"Command has property '(.*)' equal to '(.*)'")]
        public void ThenCommandHasProperty(string paramName, string paramValue)
        {

        }

        [Given(@"Input '(.*)'")]
        public void GivenAnInputString(string input)
        {

        }
    }
}
