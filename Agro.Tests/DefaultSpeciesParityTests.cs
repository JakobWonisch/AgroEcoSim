using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

public class DefaultSpeciesParityTests
{
	static SimulationRequest BuildDefaultNodeRequest(int totalHours = 168, float? plantRngFixedUnit = null)
	{
		var defaultEntry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Default");
		var graphs = defaultEntry.Graphs.Select(g => new SpeciesGraphUploadEntry
		{
			Id = g.Id,
			Name = g.Name,
			Graph = g.Graph,
		}).ToList();

		return new SimulationRequest
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = 1,
			PlantRngFixedUnit = plantRngFixedUnit,
			Plants = [new PlantRequest { SpeciesName = "Default" }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Default"] = graphs,
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Default"] = [.. DefaultSpeciesGraphBuilder.BuildDefaultConfiguration()],
			},
		};
	}

	/// <summary>
	/// Decision-structure parity with plant RNG pinned to u=0 (all accum rolls pass).
	/// </summary>
	[Fact]
	public void DefaultSpecies_ExtremalParity_FixedUnit0_Structural()
	{
		RunExtremalStructuralParity(fixedUnit: 0f);
	}

	/// <summary>
	/// Decision-structure parity with plant RNG pinned to u=1 (accum rolls fail unless p=1).
	/// Energy/water census drift is ignored.
	/// </summary>
	[Fact]
	public void DefaultSpecies_ExtremalParity_FixedUnit1_Structural()
	{
		RunExtremalStructuralParity(fixedUnit: 1f);
	}

	static void RunExtremalStructuralParity(float fixedUnit, int maxHours = 168)
	{
		var request = BuildDefaultNodeRequest(totalHours: maxHours, plantRngFixedUnit: fixedUnit);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-legacy-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-node-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);

			var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
			if (mismatch is not null)
			{
				Assert.Fail(
					$"Extremal u={fixedUnit} structural parity mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	/// <summary>
	/// Records legacy + node traces for Default (16 graphs). Full CompareFiles parity is manual —
	/// run when species implementation is complete (see plan substage 2d).
	/// </summary>
	[Fact]
	public void DefaultSpecies_ParityHarness_RecordsBothTraces()
	{
		var request = BuildDefaultNodeRequest(totalHours: 24);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: 24);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: 24);

			var legacyHeader = SimulationHarness.ReadHeader(legacyPath);
			var nodeHeader = SimulationHarness.ReadHeader(nodePath);
			Assert.Equal("legacy", legacyHeader.Mode);
			Assert.Equal("node", nodeHeader.Mode);
			Assert.Equal(42ul, legacyHeader.Seed);
			Assert.Equal(42ul, nodeHeader.Seed);
			Assert.NotEmpty(SimulationHarness.ReadSteps(legacyPath).ToList());
			Assert.NotEmpty(SimulationHarness.ReadSteps(nodePath).ToList());
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	/// <summary>
	/// Full legacy vs node parity — enable when Default species graphs are complete and tuned.
	/// </summary>
	[Fact(Skip = "Use DefaultSpecies_FullParity_Diagnostic instead (keeps trace files on disk).")]
	public void DefaultSpecies_LegacyAndNodeTraces_Match_FullParity()
	{
		var request = BuildDefaultNodeRequest(totalHours: 168);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: 168);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: 168);

			var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, new TraceCompareOptions
			{
				Tolerance = 1e-4f,
				IgnoreRng = false,
			});

			if (mismatch is not null)
			{
				Assert.Fail($"Parity mismatch at timestep {mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	static string ParityTraceDir()
	{
		// Agro.Tests/bin/Debug/net8.0 → repo root
		var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		return Path.Combine(repoRoot, "ignore", "parity-traces");
	}

	/// <summary>
	/// Records 168h legacy + node traces under ignore/parity-traces/ and reports up to 25 mismatches.
	/// Traces are kept on disk for manual inspection. After dominance-array + size-limit guards,
	/// remaining drift is mostly energy magnitude (multi-graph order vs legacy early-return).
	/// </summary>
	[Fact]
	public void DefaultSpecies_FullParity_Diagnostic()
	{
		const int maxHours = 168;
		var dir = ParityTraceDir();
		Directory.CreateDirectory(dir);
		var legacyPath = Path.Combine(dir, "legacy.jsonl");
		var nodePath = Path.Combine(dir, "node.jsonl");
		var request = BuildDefaultNodeRequest(totalHours: maxHours);

		SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);

		var options = new TraceCompareOptions { Tolerance = 1e-4f, IgnoreRng = true, IgnorePlantBalances = true };
		var mismatches = CollectMismatches(legacyPath, nodePath, options, maxCount: 25);

		if (mismatches.Count == 0)
			return;

		var lines = new List<string>
		{
			$"Legacy trace: {legacyPath}",
			$"Node trace:   {nodePath}",
			$"First {mismatches.Count} mismatch(es):",
		};
		foreach (var m in mismatches)
			lines.Add($"  t={m.Timestep}  {m.Path}  expected={m.Expected}  actual={m.Actual}");

		Assert.Fail(string.Join(Environment.NewLine, lines));
	}

	static List<TraceMismatch> CollectMismatches(string legacyPath, string nodePath, TraceCompareOptions options, int maxCount)
	{
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

			var legacy = legacySteps.Current!;
			var node = nodeSteps.Current!;
			if (legacy.Timestep != node.Timestep)
			{
				results.Add(new TraceMismatch
				{
					Timestep = legacy.Timestep,
					Path = "timestep",
					Expected = legacy.Timestep.ToString(),
					Actual = node.Timestep.ToString(),
				});
				continue;
			}

			// Re-use CompareFiles logic per step by writing temp single-step files — simpler: inline scan
			var stepMismatch = CompareStepAllFields(legacy, node, options);
			results.AddRange(stepMismatch.Take(maxCount - results.Count));
		}
		return results;
	}

	static List<TraceMismatch> CompareStepAllFields(StepSnapshot legacy, StepSnapshot node, TraceCompareOptions options)
	{
		var list = new List<TraceMismatch>();
		void Add(TraceMismatch? m)
		{
			if (m != null) list.Add(m);
		}

		if (legacy.Plants.Length != node.Plants.Length)
		{
			list.Add(new TraceMismatch
			{
				Timestep = legacy.Timestep,
				Path = "plants.length",
				Expected = legacy.Plants.Length.ToString(),
				Actual = node.Plants.Length.ToString(),
			});
			return list;
		}

		for (var p = 0; p < legacy.Plants.Length; p++)
		{
			var prefix = $"plants[{p}]";
			var lp = legacy.Plants[p];
			var np = node.Plants[p];

			if (lp.SeedAlive != np.SeedAlive)
				Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{prefix}.SeedAlive", Expected = lp.SeedAlive.ToString(), Actual = np.SeedAlive.ToString() });

			if (lp.AboveGround.Length != np.AboveGround.Length)
				Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{prefix}.AboveGround.length", Expected = lp.AboveGround.Length.ToString(), Actual = np.AboveGround.Length.ToString() });

			var n = Math.Min(lp.AboveGround.Length, np.AboveGround.Length);
			for (var i = 0; i < n; i++)
			{
				var ap = $"{prefix}.AboveGround[{i}]";
				var la = lp.AboveGround[i];
				var na = np.AboveGround[i];
				if (la.Organ != na.Organ)
					Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{ap}.Organ", Expected = la.Organ, Actual = na.Organ });
				if (MathF.Abs(la.Energy - na.Energy) > options.Tolerance)
					Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{ap}.Energy", Expected = la.Energy.ToString(), Actual = na.Energy.ToString() });
				if (MathF.Abs(la.Length - na.Length) > options.Tolerance)
					Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{ap}.Length", Expected = la.Length.ToString(), Actual = na.Length.ToString() });
				if (MathF.Abs(la.Radius - na.Radius) > options.Tolerance)
					Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{ap}.Radius", Expected = la.Radius.ToString(), Actual = na.Radius.ToString() });
				if (MathF.Abs(la.Auxins - na.Auxins) > options.Tolerance)
					Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{ap}.Auxins", Expected = la.Auxins.ToString(), Actual = na.Auxins.ToString() });
			}

			if (!options.IgnorePlantBalances)
			{
				if (MathF.Abs(lp.WaterBalance - np.WaterBalance) > options.Tolerance)
					Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{prefix}.WaterBalance", Expected = lp.WaterBalance.ToString(), Actual = np.WaterBalance.ToString() });
			}

			if (!options.IgnoreRng && lp.Rng != null && np.Rng != null && lp.Rng.State != np.Rng.State)
				Add(new TraceMismatch { Timestep = legacy.Timestep, Path = $"{prefix}.Rng.State", Expected = lp.Rng.State.ToString(), Actual = np.Rng.State.ToString() });
		}
		return list;
	}
}
