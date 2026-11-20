namespace Agro.BehaviorGraph;

/// <summary>Per-tick evaluation context for behavior graph nodes (sensors and effects).</summary>
public readonly struct TickEvalContext
{
	public PlantSubFormation<AboveGroundAgent>? Formation { get; init; }
	public int AgentId { get; init; }
	public uint Timestep { get; init; }

	public IReadOnlyDictionary<string, BehaviorConfigEntry>? BehaviorConfiguration { get; init; }

	/// <summary>Mutable per-graph-execution state shared between effect nodes.</summary>
	public TickScratch? Scratch { get; init; }

	public bool HasFormation => Formation is not null;
}

public sealed class TickScratch
{
	/// <summary>
	/// Parent copy left by the last Spawn Meristem with <c>keepParentResources</c>; a following Create Leaves
	/// with the same flag derives leaf energy from it (legacy passes the agent by value into its spawn helpers).
	/// </summary>
	public AboveGroundAgent? DetachedParent;
}
