using System.Collections.Generic;

namespace Agro.BehaviorGraph;

public sealed class CompiledBehaviorGraph
{
	public required CompiledNode[] NodesInOrder { get; init; }

	/// <summary>Topological index of the unique <see cref="GraphNodeKind.Active"/> node.</summary>
	public required int ActiveGateTopoIndex { get; init; }

	/// <summary>
	/// Per topo slot: true if this node lies on a path that feeds the Active gate input <c>isActive</c>
	/// (transitive producers only; the Active node itself is not marked).
	/// </summary>
	public required bool[] ActiveSubtreeMask { get; init; }
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
	/// <summary>For <see cref="GraphNodeKind.ConfigurationValueInput"/>.</summary>
	public string? ConfigId { get; init; }
	/// <summary>For <see cref="GraphNodeKind.ConfigurationValueInput"/>.</summary>
	public bool ConfigIsBoolean { get; init; }
}
