Feature: Commander
	In order to ensure correct parsing of input strings
	As a user
	I want every supported permutation of a defined action to be invoked

Scenario: A correct mapping is constructed
	Given A Commander definition type 'Toolbit.Specs.Parsers.Commander.TestCommander'
	And I create an instance of Commander using given definition
	Then Command 'testzero' should exist
	And  Command 'TestZero' should exist
	And  Command 'tz' should exist
	When I inspect command 'testzero'
	Then It's property 'Alias' should equal to 'tz'
	And  It's property 'Description' should equal to 'Test command'
