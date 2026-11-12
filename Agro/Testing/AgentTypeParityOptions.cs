namespace Agro.Testing;

/// <summary>
/// Options for single-agent organ growth parity: one <see cref="OrganTypes"/> agent per plant,
/// with spawn/death mocked so the formation stays at one agent while length/radius/energy evolve.
/// </summary>
public sealed class AgentTypeParityOptions
{
	/// <summary>Organ type installed as the sole above-ground agent.</summary>
	public required OrganTypes Organ { get; init; }

	/// <summary>
	/// Optional graph-name allow-list for node mode.
	/// Null keeps every graph from the request (recommended — shared life-support graphs still apply).
	/// Use <see cref="AgentTypeGraphCatalog.GraphsFor(OrganTypes)"/> to isolate organ-specific graphs.
	/// </summary>
	public IReadOnlyCollection<string>? IncludeGraphNames { get; init; }

	/// <summary>Override <see cref="SimulationRequest.TotalHours"/>.</summary>
	public int? MaxHours { get; init; }

	/// <summary>Pin plant RNG unit draws (0 = all accum pass, 1 = all fail unless p=1).</summary>
	public float? PlantRngFixedUnit { get; init; }

	/// <summary>Fixed seed position so init does not burn world RNG on placement.</summary>
	public Utils.Json.Vector3XYZ? PlantPosition { get; init; } = new() { X = 0.5f, Y = 0f, Z = 0.5f };

	public float InitialEnergy { get; init; } = 100f;
	public float InitialWater_g { get; init; } = 10f;
	public float? InitialLength { get; init; }
	public float? InitialRadius { get; init; }

	/// <summary>Mock <c>Birth</c> so spawn / create-leaf cannot add agents (default true).</summary>
	public bool SuppressSpawn { get; init; } = true;

	/// <summary>Mock <c>Death</c> so the subject agent is not removed (default true).</summary>
	public bool SuppressDeath { get; init; } = true;
}
