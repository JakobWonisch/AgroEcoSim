namespace Agro.BehaviorGraph;

/// <summary>Per-tick evaluation context for behavior graph nodes (sensors and effects).</summary>
public readonly struct TickEvalContext
{
	public PlantSubFormation<AboveGroundAgent>? Formation { get; init; }
	public int AgentId { get; init; }
	public uint Timestep { get; init; }

	public bool HasFormation => Formation is not null;
}
