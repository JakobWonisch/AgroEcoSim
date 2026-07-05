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

	/// <summary>Decision-structure parity: organs, topology, geometry; not energy/water/RNG.</summary>
	public static TraceCompareOptions StructuralParity => new()
	{
		Tolerance = 1e-4f,
		IgnoreRng = true,
		IgnorePlantBalances = true,
		IgnoreAgentEnergy = true,
		IgnoreAgentWater = true,
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

	static TraceMismatch? CompareSteps(StepSnapshot legacy, StepSnapshot node, TraceCompareOptions options)
	{
		if (legacy.Timestep != node.Timestep)
			return Mismatch(legacy.Timestep, "timestep", legacy.Timestep, node.Timestep);

		if (legacy.Plants.Length != node.Plants.Length)
			return Mismatch(legacy.Timestep, "plants.length", legacy.Plants.Length, node.Plants.Length);

		for (var p = 0; p < legacy.Plants.Length; ++p)
		{
			var prefix = $"plants[{p}]";
			var lm = ComparePlant(legacy.Plants[p], node.Plants[p], legacy.Timestep, prefix, options);
			if (lm != null)
				return lm;
		}
		return null;
	}

	static TraceMismatch? ComparePlant(PlantSnapshot legacy, PlantSnapshot node, uint timestep, string prefix, TraceCompareOptions options)
	{
		CompareBool(legacy.SeedAlive, node.SeedAlive, $"{prefix}.SeedAlive", timestep, out var m); if (m != null) return m;
		if (legacy.SeedAlive)
		{
			CompareFloat(legacy.Seed!.Radius, node.Seed!.Radius, $"{prefix}.Seed.Radius", timestep, options, out m); if (m != null) return m;
			CompareFloat(legacy.Seed.Water_g, node.Seed.Water_g, $"{prefix}.Seed.Water_g", timestep, options, out m); if (m != null) return m;
			CompareFloat(legacy.Seed.GerminationProgress, node.Seed.GerminationProgress, $"{prefix}.Seed.GerminationProgress", timestep, options, out m); if (m != null) return m;
		}

		if (!options.IgnorePlantBalances)
		{
			CompareFloat(legacy.WaterBalance, node.WaterBalance, $"{prefix}.WaterBalance", timestep, options, out m); if (m != null) return m;
			CompareFloat(legacy.WaterBalanceUG, node.WaterBalanceUG, $"{prefix}.WaterBalanceUG", timestep, options, out m); if (m != null) return m;
			CompareFloat(legacy.EnergyBalance, node.EnergyBalance, $"{prefix}.EnergyBalance", timestep, options, out m); if (m != null) return m;
		}
		CompareFloat(legacy.EnergyProductionMax, node.EnergyProductionMax, $"{prefix}.EnergyProductionMax", timestep, options, out m); if (m != null) return m;

		if (!options.IgnoreRng && legacy.Rng != null && node.Rng != null)
		{
			if (legacy.Rng.State != node.Rng.State)
				return Mismatch(timestep, $"{prefix}.Rng.State", legacy.Rng.State, node.Rng.State);
			if (legacy.Rng.Increment != node.Rng.Increment)
				return Mismatch(timestep, $"{prefix}.Rng.Increment", legacy.Rng.Increment, node.Rng.Increment);
		}

		if (legacy.VirtualRoots != null || node.VirtualRoots != null)
		{
			if (legacy.VirtualRoots == null || node.VirtualRoots == null)
				return Mismatch(timestep, $"{prefix}.VirtualRoots", "present", "missing");
			CompareFloat(legacy.VirtualRoots.Water_g, node.VirtualRoots.Water_g, $"{prefix}.VirtualRoots.Water_g", timestep, options, out m); if (m != null) return m;
			CompareFloat(legacy.VirtualRoots.Energy, node.VirtualRoots.Energy, $"{prefix}.VirtualRoots.Energy", timestep, options, out m); if (m != null) return m;
		}

		if (legacy.AboveGround.Length != node.AboveGround.Length)
			return Mismatch(timestep, $"{prefix}.AboveGround.length", legacy.AboveGround.Length, node.AboveGround.Length);

		for (var i = 0; i < legacy.AboveGround.Length; ++i)
		{
			var am = CompareAgent(legacy.AboveGround[i], node.AboveGround[i], timestep, $"{prefix}.AboveGround[{i}]", options, aboveGround: true);
			if (am != null)
				return am;
		}

		if (legacy.BelowGround.Length != node.BelowGround.Length)
			return Mismatch(timestep, $"{prefix}.BelowGround.length", legacy.BelowGround.Length, node.BelowGround.Length);

		for (var i = 0; i < legacy.BelowGround.Length; ++i)
		{
			var am = CompareAgent(legacy.BelowGround[i], node.BelowGround[i], timestep, $"{prefix}.BelowGround[{i}]", options, aboveGround: false);
			if (am != null)
				return am;
		}
		return null;
	}

	static TraceMismatch? CompareAgent(AgentSnapshot legacy, AgentSnapshot node, uint timestep, string prefix, TraceCompareOptions options, bool aboveGround)
	{
		if (legacy.Organ != node.Organ)
			return Mismatch(timestep, $"{prefix}.Organ", legacy.Organ, node.Organ);
		if (legacy.Parent != node.Parent)
			return Mismatch(timestep, $"{prefix}.Parent", legacy.Parent, node.Parent);

		CompareFloat(legacy.Length, node.Length, $"{prefix}.Length", timestep, options, out var m); if (m != null) return m;
		CompareFloat(legacy.Radius, node.Radius, $"{prefix}.Radius", timestep, options, out m); if (m != null) return m;
		if (!options.IgnoreAgentEnergy)
		{
			CompareFloat(legacy.Energy, node.Energy, $"{prefix}.Energy", timestep, options, out m); if (m != null) return m;
		}
		if (!options.IgnoreAgentWater)
		{
			CompareFloat(legacy.Water_g, node.Water_g, $"{prefix}.Water_g", timestep, options, out m); if (m != null) return m;
		}
		CompareFloat(legacy.Auxins, node.Auxins, $"{prefix}.Auxins", timestep, options, out m); if (m != null) return m;

		if (legacy.DominanceLevel != node.DominanceLevel)
			return Mismatch(timestep, $"{prefix}.DominanceLevel", legacy.DominanceLevel, node.DominanceLevel);
		if (legacy.IsRizome != node.IsRizome)
			return Mismatch(timestep, $"{prefix}.IsRizome", legacy.IsRizome, node.IsRizome);

		if (!aboveGround)
			return null;

		CompareFloat(legacy.LateralAngle, node.LateralAngle, $"{prefix}.LateralAngle", timestep, options, out m); if (m != null) return m;
		CompareFloat(legacy.ParentRadiusAtBirth, node.ParentRadiusAtBirth, $"{prefix}.ParentRadiusAtBirth", timestep, options, out m); if (m != null) return m;
		CompareFloat(legacy.LeafMaxRadiusRatio, node.LeafMaxRadiusRatio, $"{prefix}.LeafMaxRadiusRatio", timestep, options, out m); if (m != null) return m;
		if (legacy.BirthTime != node.BirthTime)
			return Mismatch(timestep, $"{prefix}.BirthTime", legacy.BirthTime, node.BirthTime);
		CompareFloat(legacy.LengthVar, node.LengthVar, $"{prefix}.LengthVar", timestep, options, out m); if (m != null) return m;
		CompareFloat(legacy.RadiusVar, node.RadiusVar, $"{prefix}.RadiusVar", timestep, options, out m); if (m != null) return m;
		CompareFloat(legacy.GrowthTimeVar, node.GrowthTimeVar, $"{prefix}.GrowthTimeVar", timestep, options, out m); if (m != null) return m;

		if (legacy.Flower.FlowerBase != node.Flower.FlowerBase)
			return Mismatch(timestep, $"{prefix}.Flower.FlowerBase", legacy.Flower.FlowerBase, node.Flower.FlowerBase);
		if (legacy.Flower.Debth != node.Flower.Debth)
			return Mismatch(timestep, $"{prefix}.Flower.Debth", legacy.Flower.Debth, node.Flower.Debth);

		if (legacy.RizomeInfo!.Test != node.RizomeInfo!.Test)
			return Mismatch(timestep, $"{prefix}.RizomeInfo.Test", legacy.RizomeInfo.Test, node.RizomeInfo.Test);
		if (legacy.RizomeInfo.RizomeDepth != node.RizomeInfo.RizomeDepth)
			return Mismatch(timestep, $"{prefix}.RizomeInfo.RizomeDepth", legacy.RizomeInfo.RizomeDepth, node.RizomeInfo.RizomeDepth);

		return null;
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
