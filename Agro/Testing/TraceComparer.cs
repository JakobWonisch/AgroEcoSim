namespace Agro.Testing;

public sealed class TraceCompareOptions
{
	public float Tolerance { get; init; } = 1e-5f;
	public bool IgnoreRng { get; init; }
	/// <summary>Skip plant-level balances (coupled to roots/RNG); compare agents only.</summary>
	public bool IgnorePlantBalances { get; init; }
	/// <summary>Skip per-agent energy (census redistribution noise under extremal RNG).</summary>
	public bool IgnoreAgentEnergy { get; init; }
	/// <summary>Skip per-agent water (follows photosynthesis / energy drift).</summary>
	public bool IgnoreAgentWater { get; init; }

	/// <summary>
	/// When set, deep-compare only agents whose <see cref="AgentSnapshot.Organ"/> is in this set.
	/// Topology (organ identity + parent + agent counts) is still checked for every agent.
	/// </summary>
	public HashSet<string>? FocusOrgans { get; init; }

	/// <summary>
	/// When set, deep-compare only above-ground agents at these indices (stable under organ transforms).
	/// </summary>
	public HashSet<int>? FocusAboveGroundIndices { get; init; }

	/// <summary>Decision-structure parity: organs, topology, geometry; not energy/water/RNG.</summary>
	public static TraceCompareOptions StructuralParity => new()
	{
		Tolerance = 1e-4f,
		IgnoreRng = true,
		IgnorePlantBalances = true,
		IgnoreAgentEnergy = true,
		IgnoreAgentWater = true,
	};

	/// <summary>Structural parity restricted to one or more organ type names.</summary>
	public static TraceCompareOptions StructuralParityForOrgans(params OrganTypes[] organs) => new()
	{
		Tolerance = 1e-4f,
		IgnoreRng = true,
		IgnorePlantBalances = true,
		IgnoreAgentEnergy = true,
		IgnoreAgentWater = true,
		FocusOrgans = organs.Select(o => o.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase),
	};

	public TraceCompareOptions WithFocusOrgans(IEnumerable<OrganTypes> organs) => new()
	{
		Tolerance = Tolerance,
		IgnoreRng = IgnoreRng,
		IgnorePlantBalances = IgnorePlantBalances,
		IgnoreAgentEnergy = IgnoreAgentEnergy,
		IgnoreAgentWater = IgnoreAgentWater,
		FocusOrgans = organs.Select(o => o.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase),
		FocusAboveGroundIndices = FocusAboveGroundIndices,
	};

	public TraceCompareOptions WithFocusAboveGroundIndices(params int[] indices) => new()
	{
		Tolerance = Tolerance,
		IgnoreRng = IgnoreRng,
		IgnorePlantBalances = IgnorePlantBalances,
		IgnoreAgentEnergy = IgnoreAgentEnergy,
		IgnoreAgentWater = IgnoreAgentWater,
		FocusOrgans = FocusOrgans,
		FocusAboveGroundIndices = indices.ToHashSet(),
	};
}

public sealed class TraceMismatch
{
	public required uint Timestep { get; init; }
	public required string Path { get; init; }
	public string? Expected { get; init; }
	public string? Actual { get; init; }
}

public static class TraceComparer
{
	public static TraceMismatch? CompareFiles(string legacyPath, string nodePath, TraceCompareOptions? options = null)
	{
		options ??= new TraceCompareOptions();
		var legacyHeader = SimulationHarness.ReadHeader(legacyPath);
		var nodeHeader = SimulationHarness.ReadHeader(nodePath);
		if (legacyHeader.Seed != nodeHeader.Seed)
			return new TraceMismatch { Timestep = 0, Path = "header.Seed", Expected = legacyHeader.Seed?.ToString(), Actual = nodeHeader.Seed?.ToString() };

		using var legacySteps = SimulationHarness.ReadSteps(legacyPath).GetEnumerator();
		using var nodeSteps = SimulationHarness.ReadSteps(nodePath).GetEnumerator();
		while (true)
		{
			var hasLegacy = legacySteps.MoveNext();
			var hasNode = nodeSteps.MoveNext();
			if (!hasLegacy && !hasNode)
				return null;
			if (!hasLegacy || !hasNode)
				return new TraceMismatch
				{
					Timestep = hasLegacy ? legacySteps.Current!.Timestep : nodeSteps.Current!.Timestep,
					Path = "trace.length",
					Expected = hasLegacy ? "more steps in legacy" : "more steps in node",
					Actual = hasLegacy ? "node ended early" : "legacy ended early",
				};

			var mismatch = CompareSteps(legacySteps.Current!, nodeSteps.Current!, options);
			if (mismatch != null)
				return mismatch;
		}
	}

	/// <summary>Collect up to <paramref name="maxCount"/> mismatches across the full traces.</summary>
	public static List<TraceMismatch> CollectMismatches(string legacyPath, string nodePath, TraceCompareOptions? options = null, int maxCount = 25)
	{
		options ??= new TraceCompareOptions();
		var results = new List<TraceMismatch>();
		using var legacySteps = SimulationHarness.ReadSteps(legacyPath).GetEnumerator();
		using var nodeSteps = SimulationHarness.ReadSteps(nodePath).GetEnumerator();
		while (results.Count < maxCount)
		{
			var hasLegacy = legacySteps.MoveNext();
			var hasNode = nodeSteps.MoveNext();
			if (!hasLegacy && !hasNode)
				break;
			if (!hasLegacy || !hasNode)
			{
				results.Add(new TraceMismatch
				{
					Timestep = hasLegacy ? legacySteps.Current!.Timestep : nodeSteps.Current!.Timestep,
					Path = "trace.length",
					Expected = hasLegacy ? "more steps in legacy" : "more steps in node",
					Actual = hasLegacy ? "node ended early" : "legacy ended early",
				});
				break;
			}

			var stepMismatches = CompareStepsAll(legacySteps.Current!, nodeSteps.Current!, options, maxCount - results.Count);
			results.AddRange(stepMismatches);
		}
		return results;
	}

	static TraceMismatch? CompareSteps(StepSnapshot legacy, StepSnapshot node, TraceCompareOptions options)
	{
		var all = CompareStepsAll(legacy, node, options, maxCount: 1);
		return all.Count > 0 ? all[0] : null;
	}

	static List<TraceMismatch> CompareStepsAll(StepSnapshot legacy, StepSnapshot node, TraceCompareOptions options, int maxCount)
	{
		var list = new List<TraceMismatch>();
		void Add(TraceMismatch? m)
		{
			if (m != null && list.Count < maxCount)
				list.Add(m);
		}

		if (legacy.Timestep != node.Timestep)
		{
			Add(Mismatch(legacy.Timestep, "timestep", legacy.Timestep, node.Timestep));
			return list;
		}

		if (legacy.Plants.Length != node.Plants.Length)
		{
			Add(Mismatch(legacy.Timestep, "plants.length", legacy.Plants.Length, node.Plants.Length));
			return list;
		}

		for (var p = 0; p < legacy.Plants.Length && list.Count < maxCount; ++p)
		{
			var prefix = $"plants[{p}]";
			foreach (var m in ComparePlantAll(legacy.Plants[p], node.Plants[p], legacy.Timestep, prefix, options, maxCount - list.Count))
				list.Add(m);
		}
		return list;
	}

	static List<TraceMismatch> ComparePlantAll(PlantSnapshot legacy, PlantSnapshot node, uint timestep, string prefix, TraceCompareOptions options, int maxCount)
	{
		var list = new List<TraceMismatch>();
		void Add(TraceMismatch? m)
		{
			if (m != null && list.Count < maxCount)
				list.Add(m);
		}

		CompareBool(legacy.SeedAlive, node.SeedAlive, $"{prefix}.SeedAlive", timestep, out var m); Add(m);
		if (legacy.SeedAlive && node.SeedAlive && legacy.Seed != null && node.Seed != null)
		{
			CompareFloat(legacy.Seed.Radius, node.Seed.Radius, $"{prefix}.Seed.Radius", timestep, options, out m); Add(m);
			CompareFloat(legacy.Seed.Water_g, node.Seed.Water_g, $"{prefix}.Seed.Water_g", timestep, options, out m); Add(m);
			CompareFloat(legacy.Seed.GerminationProgress, node.Seed.GerminationProgress, $"{prefix}.Seed.GerminationProgress", timestep, options, out m); Add(m);
		}

		if (!options.IgnorePlantBalances)
		{
			CompareFloat(legacy.WaterBalance, node.WaterBalance, $"{prefix}.WaterBalance", timestep, options, out m); Add(m);
			CompareFloat(legacy.WaterBalanceUG, node.WaterBalanceUG, $"{prefix}.WaterBalanceUG", timestep, options, out m); Add(m);
			CompareFloat(legacy.EnergyBalance, node.EnergyBalance, $"{prefix}.EnergyBalance", timestep, options, out m); Add(m);
		}
		CompareFloat(legacy.EnergyProductionMax, node.EnergyProductionMax, $"{prefix}.EnergyProductionMax", timestep, options, out m); Add(m);

		if (!options.IgnoreRng && legacy.Rng != null && node.Rng != null)
		{
			if (legacy.Rng.State != node.Rng.State)
				Add(Mismatch(timestep, $"{prefix}.Rng.State", legacy.Rng.State, node.Rng.State));
			if (legacy.Rng.Increment != node.Rng.Increment)
				Add(Mismatch(timestep, $"{prefix}.Rng.Increment", legacy.Rng.Increment, node.Rng.Increment));
		}

		if (legacy.VirtualRoots != null || node.VirtualRoots != null)
		{
			if (legacy.VirtualRoots == null || node.VirtualRoots == null)
				Add(Mismatch(timestep, $"{prefix}.VirtualRoots", "present", "missing"));
			else
			{
				CompareFloat(legacy.VirtualRoots.Water_g, node.VirtualRoots.Water_g, $"{prefix}.VirtualRoots.Water_g", timestep, options, out m); Add(m);
				CompareFloat(legacy.VirtualRoots.Energy, node.VirtualRoots.Energy, $"{prefix}.VirtualRoots.Energy", timestep, options, out m); Add(m);
			}
		}

		if (legacy.AboveGround.Length != node.AboveGround.Length)
		{
			Add(Mismatch(timestep, $"{prefix}.AboveGround.length", legacy.AboveGround.Length, node.AboveGround.Length));
		}
		else
		{
			for (var i = 0; i < legacy.AboveGround.Length && list.Count < maxCount; ++i)
			{
				foreach (var am in CompareAgentAll(legacy.AboveGround[i], node.AboveGround[i], timestep, $"{prefix}.AboveGround[{i}]", options, aboveGround: true, agentIndex: i, maxCount - list.Count))
					list.Add(am);
			}
		}

		if (legacy.BelowGround.Length != node.BelowGround.Length)
		{
			Add(Mismatch(timestep, $"{prefix}.BelowGround.length", legacy.BelowGround.Length, node.BelowGround.Length));
		}
		else
		{
			for (var i = 0; i < legacy.BelowGround.Length && list.Count < maxCount; ++i)
			{
				foreach (var am in CompareAgentAll(legacy.BelowGround[i], node.BelowGround[i], timestep, $"{prefix}.BelowGround[{i}]", options, aboveGround: false, agentIndex: i, maxCount - list.Count))
					list.Add(am);
			}
		}
		return list;
	}

	static List<TraceMismatch> CompareAgentAll(AgentSnapshot legacy, AgentSnapshot node, uint timestep, string prefix, TraceCompareOptions options, bool aboveGround, int agentIndex, int maxCount)
	{
		var list = new List<TraceMismatch>();
		void Add(TraceMismatch? m)
		{
			if (m != null && list.Count < maxCount)
				list.Add(m);
		}

		if (legacy.Organ != node.Organ)
		{
			Add(Mismatch(timestep, $"{prefix}.Organ", legacy.Organ, node.Organ));
			return list;
		}
		if (legacy.Parent != node.Parent)
			Add(Mismatch(timestep, $"{prefix}.Parent", legacy.Parent, node.Parent));

		var deepCompare = ShouldDeepCompare(options, legacy.Organ, aboveGround, agentIndex);
		if (!deepCompare)
			return list;

		CompareFloat(legacy.Length, node.Length, $"{prefix}.Length", timestep, options, out var m); Add(m);
		CompareFloat(legacy.Radius, node.Radius, $"{prefix}.Radius", timestep, options, out m); Add(m);
		if (!options.IgnoreAgentEnergy)
		{
			CompareFloat(legacy.Energy, node.Energy, $"{prefix}.Energy", timestep, options, out m); Add(m);
		}
		if (!options.IgnoreAgentWater)
		{
			CompareFloat(legacy.Water_g, node.Water_g, $"{prefix}.Water_g", timestep, options, out m); Add(m);
		}
		CompareFloat(legacy.Auxins, node.Auxins, $"{prefix}.Auxins", timestep, options, out m); Add(m);

		if (legacy.DominanceLevel != node.DominanceLevel)
			Add(Mismatch(timestep, $"{prefix}.DominanceLevel", legacy.DominanceLevel, node.DominanceLevel));
		if (legacy.IsRizome != node.IsRizome)
			Add(Mismatch(timestep, $"{prefix}.IsRizome", legacy.IsRizome, node.IsRizome));

		if (!aboveGround)
			return list;

		CompareFloat(legacy.LateralAngle, node.LateralAngle, $"{prefix}.LateralAngle", timestep, options, out m); Add(m);
		CompareFloat(legacy.ParentRadiusAtBirth, node.ParentRadiusAtBirth, $"{prefix}.ParentRadiusAtBirth", timestep, options, out m); Add(m);
		CompareFloat(legacy.LeafMaxRadiusRatio, node.LeafMaxRadiusRatio, $"{prefix}.LeafMaxRadiusRatio", timestep, options, out m); Add(m);
		if (legacy.BirthTime != node.BirthTime)
			Add(Mismatch(timestep, $"{prefix}.BirthTime", legacy.BirthTime, node.BirthTime));
		CompareFloat(legacy.LengthVar, node.LengthVar, $"{prefix}.LengthVar", timestep, options, out m); Add(m);
		CompareFloat(legacy.RadiusVar, node.RadiusVar, $"{prefix}.RadiusVar", timestep, options, out m); Add(m);
		CompareFloat(legacy.GrowthTimeVar, node.GrowthTimeVar, $"{prefix}.GrowthTimeVar", timestep, options, out m); Add(m);

		if (legacy.Flower.FlowerBase != node.Flower.FlowerBase)
			Add(Mismatch(timestep, $"{prefix}.Flower.FlowerBase", legacy.Flower.FlowerBase, node.Flower.FlowerBase));
		if (legacy.Flower.Debth != node.Flower.Debth)
			Add(Mismatch(timestep, $"{prefix}.Flower.Debth", legacy.Flower.Debth, node.Flower.Debth));

		if (legacy.RizomeInfo!.Test != node.RizomeInfo!.Test)
			Add(Mismatch(timestep, $"{prefix}.RizomeInfo.Test", legacy.RizomeInfo.Test, node.RizomeInfo.Test));
		if (legacy.RizomeInfo.RizomeDepth != node.RizomeInfo.RizomeDepth)
			Add(Mismatch(timestep, $"{prefix}.RizomeInfo.RizomeDepth", legacy.RizomeInfo.RizomeDepth, node.RizomeInfo.RizomeDepth));

		return list;
	}

	static bool ShouldDeepCompare(TraceCompareOptions options, string organ, bool aboveGround, int agentIndex)
	{
		if (options.FocusOrgans is null && options.FocusAboveGroundIndices is null)
			return true;
		if (aboveGround && options.FocusAboveGroundIndices is { Count: > 0 }
			&& options.FocusAboveGroundIndices.Contains(agentIndex))
			return true;
		if (options.FocusOrgans is { Count: > 0 } && options.FocusOrgans.Contains(organ))
			return true;
		// If only index focus is set, non-matching indices skip deep compare.
		if (options.FocusAboveGroundIndices is { Count: > 0 } && options.FocusOrgans is null)
			return false;
		if (options.FocusOrgans is { Count: > 0 } && options.FocusAboveGroundIndices is null)
			return false;
		return false;
	}

	static void CompareBool(bool a, bool b, string path, uint timestep, out TraceMismatch? mismatch)
	{
		mismatch = a == b ? null : Mismatch(timestep, path, a, b);
	}

	static void CompareFloat(float a, float b, string path, uint timestep, TraceCompareOptions options, out TraceMismatch? mismatch)
	{
		var diff = MathF.Abs(a - b);
		mismatch = diff <= options.Tolerance ? null : Mismatch(timestep, path, a, b);
	}

	static TraceMismatch Mismatch(uint timestep, string path, object? expected, object? actual) => new()
	{
		Timestep = timestep,
		Path = path,
		Expected = expected?.ToString(),
		Actual = actual?.ToString(),
	};
}
