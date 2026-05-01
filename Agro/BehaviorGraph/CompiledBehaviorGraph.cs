using System.Collections.Generic;

namespace Agro.BehaviorGraph;

public sealed class CompiledBehaviorGraph
{
	public required CompiledNode[] NodesInOrder { get; init; }
}

public sealed class CompiledNode
{
	/// <summary>Index into the original exported graph nodes list at compile time.</summary>
	public int GraphNodeIndex { get; init; }
	public required string Id { get; init; }
	public required GraphNodeKind Kind { get; init; }
	/// <summary>Per target input socket: predecessor (node index in graph) and its output socket name. TS uses first connection only per input.</summary>
	public required Dictionary<string, List<(int ProducerIndex, string ProducerSocket)>> Inputs { get; init; }

	public float NumberConst { get; init; }
	public bool BoolConst { get; init; }
	/// <summary>For Greater/Less Than (or Equal): true means inclusive comparison.</summary>
	public bool NumericInclusive { get; init; }
}
