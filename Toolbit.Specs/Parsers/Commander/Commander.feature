Feature: Commander
	In order to ensure correct parsing of input strings
	As a user
	I want every supported permutation of a defined action to be invoked

Scenario: A correct mapping is constructed
	Given A Commander definition type 'Toolbit.Specs.Parsers.Commander.SpecsCommanderTest'
	Then Command 'singleaction' should exist
	And  Command 'SingleAction' should exist
	And  Command 'sa' should exist
	When I inspect command 'singleaction'
	Then It's property 'Alias' should equal to 'sa'
	And  It's property 'Description' should equal to 'An action handler'

Scenario: Command invoked
	Given A Commander definition type 'Toolbit.Specs.Parsers.Commander.SpecsCommanderTest'
	When Input string is '<input>'
	Then Result is '<expected>'

Examples: 
| input            | expected                                    |
| singleaction     | Void SingleAction()                         |
| sa               | Void SingleAction()                         |
| SingleAction     | Void SingleAction()                         |
| do               | Void DoThings()                             |
| do str           | Void DoThings(System.String)                |
| dothings str 10  | Void DoThings(System.String, Int32)         |
| dothings str str | Void DoThings(System.String, System.String) |