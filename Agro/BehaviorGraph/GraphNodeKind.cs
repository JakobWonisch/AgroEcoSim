namespace Agro.BehaviorGraph;

public enum GraphNodeKind : byte
{
	NumberInput,
	BooleanInput,
	/// <summary>Legacy label "Agent Type"; same semantics as <see cref="OrganSensors"/>.</summary>
	AgentType,
	OrganSensors,
	Active,
	BooleanOutput,
	NumberOutput,
	And,
	Or,
	Xor,
	Not,
	GreaterThanOrEqual,
	LessThanOrEqual,
	EqualTo,
	IfElse,
	Add,
	Subtract,
	Multiply,
	Divide,
	Growth,
}
