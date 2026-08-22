using System.Numerics;
using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

/// <summary>
/// Bergania-tick species legacy vs node traces. Compile tests live in <see cref="BehaviorGraphCompilerTests"/>.
/// Parity comparisons use <see cref="ParityTestLimits.MaxHours"/> hours until green, then expand to
/// <see cref="ParityTestLimits.NextExpansionHours"/>.
/// </summary>
public class BerganiaSpeciesParityTests
{
	static SimulationRequest BuildBerganiaNodeRequest(string speciesName, int totalHours = ParityTestLimits.MaxHours, float? plantRngFixedUnit = null, int hoursPerTick = 1)
	{
		var entry = PredefinedSpeciesCatalog.All.First(s => s.Name == speciesName);
		var graphs = entry.Graphs.Select(g => new SpeciesGraphUploadEntry
		{
			Id = g.Id,
			Name = g.Name,
			Graph = g.Graph,
		}).ToList();

		var config = speciesName switch
		{
			"Geranium Macrorrhizum" => BerganiaTickGraphBuilder.BuildConfiguration(
				BerganiaTickGraphBuilder.BerganiaGraphOptions.GeraniumMacrorrhizum),
			"Geranium × Cantabrigiense" => BerganiaTickGraphBuilder.BuildConfiguration(
				BerganiaTickGraphBuilder.BerganiaGraphOptions.GeraniumCantabrigiense),
			_ => BerganiaTickGraphBuilder.BuildConfiguration(
				BerganiaTickGraphBuilder.BerganiaGraphOptions.BergeniaCordifolia),
		};

		return new SimulationRequest
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = hoursPerTick,
			PlantRngFixedUnit = plantRngFixedUnit,
			Plants = [new PlantRequest { SpeciesName = speciesName }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				[speciesName] = graphs,
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				[speciesName] = config,
			},
		};
	}

	static string ParityTraceDir(string speciesSlug) =>
		Path.Combine(
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
			"ignore", "parity-traces", "bergania", speciesSlug);

	[Fact]
	public void Geranium_Node_At1440Hours_HoursPerTick4_GrowsPastInitialLeaves()
	{
		foreach (var speciesName in new[] { "Geranium Macrorrhizum", "Geranium × Cantabrigiense" })
		{
			const int totalHours = 1440;
			const int hoursPerTick = 4;
			var request = BuildBerganiaNodeRequest(speciesName, totalHours: totalHours, hoursPerTick: hoursPerTick);
			var nodePath = Path.Combine(Path.GetTempPath(), $"g-hpt4-{Guid.NewGuid():N}.jsonl");
			try
			{
				SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: totalHours);
				var leaves = SimulationHarness.ReadSteps(nodePath).Last().Plants[0].AboveGround
					.Count(a => a.Organ == "Leaf");
				Assert.True(leaves > 2,
					$"{speciesName} should chain past the germination leaf pair at 1440h/4h-tick; got {leaves} leaves.");
			}
			finally
			{
				if (File.Exists(nodePath)) File.Delete(nodePath);
			}
		}
	}

	[Fact]
	public void Geranium_OrganCounts_LegacyVsNode_At200Hours()
	{
		foreach (var speciesName in new[] { "Geranium Macrorrhizum", "Geranium × Cantabrigiense" })
		{
			const int totalHours = 200;
			var request = BuildBerganiaNodeRequest(speciesName, totalHours: totalHours, hoursPerTick: 1);
			var legacyPath = Path.Combine(Path.GetTempPath(), $"g-leg-{Guid.NewGuid():N}.jsonl");
			var nodePath = Path.Combine(Path.GetTempPath(), $"g-node-{Guid.NewGuid():N}.jsonl");
			try
			{
				SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: totalHours);
				SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: totalHours);
				var legacy = SimulationHarness.ReadSteps(legacyPath).Last().Plants[0].AboveGround;
				var node = SimulationHarness.ReadSteps(nodePath).Last().Plants[0].AboveGround;
				Assert.Equal(legacy.Count(a => a.Organ == "Leaf"), node.Count(a => a.Organ == "Leaf"));
				Assert.Equal(legacy.Count(a => a.Organ == "Stem" && !a.IsRizome), node.Count(a => a.Organ == "Stem" && !a.IsRizome));
				Assert.Equal(legacy.Count(a => a.Organ == "Petiole"), node.Count(a => a.Organ == "Petiole"));
			}
			finally
			{
				if (File.Exists(legacyPath)) File.Delete(legacyPath);
				if (File.Exists(nodePath)) File.Delete(nodePath);
			}
		}
	}

	[Fact]
	public void BerganiaSpecies_SpringCrownRecruitsRhizomeBuds_AtTimestep3()
	{
		const int maxHours = 4;
		var request = BuildBerganiaNodeRequest("Geranium Macrorrhizum", totalHours: maxHours, plantRngFixedUnit: 0f);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"berg-crown-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"berg-crown-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);

			var legacy = SimulationHarness.ReadSteps(legacyPath).First(s => s.Timestep == 3);
			var node = SimulationHarness.ReadSteps(nodePath).First(s => s.Timestep == 3);
			var legacyBuds = legacy.Plants[0].AboveGround.Count(a => a.Organ == "Bud");
			var nodeBuds = node.Plants[0].AboveGround.Count(a => a.Organ == "Bud");

			Assert.Equal(0, legacyBuds);
			Assert.Equal(0, nodeBuds);
			Assert.Equal(legacy.Plants[0].AboveGround.Length, node.Plants[0].AboveGround.Length);

			static Vector3 Axis(QuaternionSnapshot q) =>
				Vector3.Transform(Vector3.UnitX, new Quaternion(q.X, q.Y, q.Z, q.W));

			var legacyShoots = legacy.Plants[0].AboveGround
				.Where(a => !a.IsRizome && (a.Organ == "Meristem" || a.Organ == "Stem"))
				.Select(a => Axis(a.Orientation).Y)
				.ToList();
			var nodeShoots = node.Plants[0].AboveGround
				.Where(a => !a.IsRizome && (a.Organ == "Meristem" || a.Organ == "Stem"))
				.Select(a => Axis(a.Orientation).Y)
				.ToList();
			Assert.NotEmpty(legacyShoots);
			Assert.Equal(legacyShoots.Count, nodeShoots.Count);
			for (var i = 0; i < legacyShoots.Count; i++)
			{
				Assert.True(legacyShoots[i] > 0.5f, $"legacy shoot[{i}] should be pitched up, Y={legacyShoots[i]}");
				Assert.True(nodeShoots[i] > 0.5f, $"node shoot[{i}] should be pitched up, Y={nodeShoots[i]}");
				Assert.Equal(legacyShoots[i], nodeShoots[i], 3);
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	[Fact]
	public void BerganiaSpecies_AtTimestep2_MatchesLegacyAgentCount()
	{
		const int maxHours = 3;
		var request = BuildBerganiaNodeRequest("Geranium Macrorrhizum", totalHours: maxHours, plantRngFixedUnit: 0f);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"berg-t2-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"berg-t2-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);

			var legacy = SimulationHarness.ReadSteps(legacyPath).First(s => s.Timestep == 2);
			var node = SimulationHarness.ReadSteps(nodePath).First(s => s.Timestep == 2);
			Assert.Equal(legacy.Plants[0].AboveGround.Length, node.Plants[0].AboveGround.Length);
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	/// <summary>Fast smoke: 24h legacy + node traces for each Bergania species (~5–10s each).</summary>
	[Theory]
	[InlineData("Geranium Macrorrhizum")]
	[InlineData("Geranium × Cantabrigiense")]
	[InlineData("Bergenia Cordifolia")]
	public void BerganiaSpecies_ParityHarness_RecordsBothTraces(string speciesName)
	{
		var request = BuildBerganiaNodeRequest(speciesName, totalHours: ParityTestLimits.MaxHours);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-berg-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-berg-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: ParityTestLimits.MaxHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: ParityTestLimits.MaxHours);

			Assert.Equal("legacy", SimulationHarness.ReadHeader(legacyPath).Mode);
			Assert.Equal("node", SimulationHarness.ReadHeader(nodePath).Mode);
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
	/// Records structural parity traces for <see cref="ParityTestLimits.MaxHours"/> hours.
	/// Expand to <see cref="ParityTestLimits.NextExpansionHours"/> once green.
	/// </summary>
	[Theory]
	[InlineData("Geranium Macrorrhizum")]
	[InlineData("Geranium × Cantabrigiense")]
	public void BerganiaSpecies_StructuralParity_Diagnostic(string speciesName)
	{
		const int maxHours = ParityTestLimits.MaxHours;
		var slug = speciesName.Replace(' ', '-').Replace('×', 'x');
		var dir = ParityTraceDir(slug);
		Directory.CreateDirectory(dir);
		var legacyPath = Path.Combine(dir, "legacy-u0.jsonl");
		var nodePath = Path.Combine(dir, "node-u0.jsonl");
		var request = BuildBerganiaNodeRequest(speciesName, totalHours: maxHours, plantRngFixedUnit: 0f);

		SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);

		var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
		if (mismatch is null)
			return;

		Assert.Fail(
			$"{speciesName} structural parity mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}\n" +
			$"Legacy: {legacyPath}\nNode:   {nodePath}");
	}

	[Fact(Skip = "Long-running growth diagnostic; run manually.")]
	public void BergeniaCordifolia_Node_At1440Hours_UserSettings()
	{
		const int totalHours = 1440;
		const int hoursPerTick = 4;
		var request = BuildBerganiaNodeRequest("Bergenia Cordifolia", totalHours: totalHours, hoursPerTick: hoursPerTick, plantRngFixedUnit: null);
		var nodePath = Path.Combine(Path.GetTempPath(), $"berg-1440-{Guid.NewGuid():N}.jsonl");
		var legacyPath = Path.Combine(Path.GetTempPath(), $"berg-legacy-1440-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: totalHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: totalHours);

			var nodeSteps = SimulationHarness.ReadSteps(nodePath).ToList();
			var legacySteps = SimulationHarness.ReadSteps(legacyPath).ToList();
			foreach (var t in new[] { 50u, 100u, 200u, 300u, (uint)(totalHours / hoursPerTick - 1) })
			{
				if (nodeSteps.All(s => s.Timestep != t)) continue;
				var step = nodeSteps.First(s => s.Timestep == t);
				var ag = step.Plants[0].AboveGround;
				var organs = ag.GroupBy(a => a.Organ).ToDictionary(g => g.Key, g => g.Count());
				var leaves = ag.Where(a => a.Organ == "Leaf").ToList();
				Console.WriteLine(
					$"node t={t} ({t * hoursPerTick}h) count={ag.Length} " +
					string.Join(" ", organs.Select(kv => $"{kv.Key}:{kv.Value}")) +
					(leaves.Count > 0 ? $" maxLeafLen={leaves.Max(l => l.Length):F4}" : ""));
			}

			var finalNode = nodeSteps.Last();
			var finalLegacy = legacySteps.Last();
			var nodeLeaves = finalNode.Plants[0].AboveGround.Count(a => a.Organ == "Leaf");
			var legacyLeaves = finalLegacy.Plants[0].AboveGround.Count(a => a.Organ == "Leaf");
			Console.WriteLine($"FINAL node leaves={nodeLeaves} agents={finalNode.Plants[0].AboveGround.Length}");
			Console.WriteLine($"FINAL legacy leaves={legacyLeaves} agents={finalLegacy.Plants[0].AboveGround.Length}");

			Assert.True(nodeLeaves > 2, $"Expected >2 leaves at 1440h; node had {nodeLeaves}, legacy had {legacyLeaves}");
		}
		finally
		{
			if (File.Exists(nodePath)) File.Delete(nodePath);
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
		}
	}

	[Fact]
	public void BergeniaCordifolia_ExtremalParity_FixedUnit0_Structural()
	{
		RunBergeniaExtremalStructuralParity(fixedUnit: 0f);
	}

	[Fact]
	public void BergeniaCordifolia_ExtremalParity_FixedUnit1_Structural()
	{
		RunBergeniaExtremalStructuralParity(fixedUnit: 1f);
	}

	static void RunBergeniaExtremalStructuralParity(float fixedUnit, int maxHours = ParityTestLimits.MaxHours)
	{
		var request = BuildBerganiaNodeRequest("Bergenia Cordifolia", totalHours: maxHours, plantRngFixedUnit: fixedUnit);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"berg-legacy-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"berg-node-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);

			var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
			if (mismatch is not null)
			{
				Assert.Fail(
					$"Bergenia extremal u={fixedUnit} structural parity mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	[Fact]
	public void BergeniaCordifolia_JsonRoundTrip_ResolvesGraphsAndConfiguration()
	{
		var entry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Bergenia Cordifolia");
		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = 1440,
			HoursPerTick = 4,
			Plants = [new PlantRequest { SpeciesName = "Bergenia Cordifolia" }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Bergenia Cordifolia"] = entry.Graphs.Select(g => new SpeciesGraphUploadEntry
				{
					Id = g.Id,
					Name = g.Name,
					Graph = g.Graph,
				}).ToList(),
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Bergenia Cordifolia"] = entry.Configuration!.ToList(),
			},
		};

		var json = System.Text.Json.JsonSerializer.Serialize(request);
		var parsed = System.Text.Json.JsonSerializer.Deserialize<SimulationRequest>(json)
			?? throw new InvalidOperationException("deserialize failed");

		Assert.NotNull(parsed.SpeciesGraphs);
		Assert.True(parsed.SpeciesGraphs!.ContainsKey("Bergenia Cordifolia"));
		Assert.NotNull(parsed.SpeciesConfiguration);
		Assert.True(parsed.SpeciesConfiguration!.ContainsKey("Bergenia Cordifolia"));

		var profile = PlantSpeciesProfile.Resolve("Bergenia Cordifolia", parsed);
		Assert.NotEmpty(profile.BehaviorGraphs);
		Assert.True(profile.BehaviorConfiguration.ContainsKey(BerganiaTickGraphBuilder.ConfigIds.PChaining));
		var pChain = profile.BehaviorConfiguration[BerganiaTickGraphBuilder.ConfigIds.PChaining].FloatArrayValue;
		Assert.Equal(0.015f, pChain[0]);
	}

	[Fact]
	public void BergeniaCordifolia_Node_At200Hours_WithoutSpeciesConfiguration_ChainsFromMorphology()
	{
		const int totalHours = 200;
		var baseRequest = BuildBerganiaNodeRequest("Bergenia Cordifolia", totalHours: totalHours, hoursPerTick: 1);
		var request = new SimulationRequest
		{
			Seed = baseRequest.Seed,
			TotalHours = baseRequest.TotalHours,
			HoursPerTick = baseRequest.HoursPerTick,
			Plants = baseRequest.Plants,
			SpeciesGraphs = baseRequest.SpeciesGraphs,
		};
		var nodePath = Path.Combine(Path.GetTempPath(), $"berg-node-nocfg-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: totalHours);
			var step = SimulationHarness.ReadSteps(nodePath).Last();
			var leafCount = step.Plants[0].AboveGround.Count(a => a.Organ == "Leaf");
			Assert.True(leafCount > 2,
				$"Meristem chaining should use species pChaningSeaonns when graph config is omitted; got {leafCount} leaves.");
		}
		finally
		{
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	[Fact(Skip = "Long-running diagnostic; run manually.")]
	public void BergeniaCordifolia_LegacyVsNode_At200Hours()
	{
		const int totalHours = 200;
		var request = BuildBerganiaNodeRequest("Bergenia Cordifolia", totalHours: totalHours, hoursPerTick: 1);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"berg-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"berg-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: totalHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: totalHours);

			void Dump(string label, string path)
			{
				var step = SimulationHarness.ReadSteps(path).Last();
				var ag = step.Plants[0].AboveGround;
				var leaves = ag.Where(a => a.Organ == "Leaf").ToList();
				var organs = ag.GroupBy(a => a.Organ).ToDictionary(g => g.Key, g => g.Count());
				Console.WriteLine(
					$"{label} t={step.Timestep} count={ag.Length} " +
					string.Join(" ", organs.Select(kv => $"{kv.Key}:{kv.Value}")));
				if (leaves.Count > 0)
				{
					var avgLen = leaves.Average(l => l.Length);
					var avgRad = leaves.Average(l => l.Radius);
					Console.WriteLine($"  leaves avg length={avgLen:F4} radius={avgRad:F4} maxLen={leaves.Max(l => l.Length):F4}");
				}
			}

			Dump("legacy", legacyPath);
			Dump("node", nodePath);
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	[Fact(Skip = "Long-running growth diagnostic; run manually.")]
	public void BergeniaCordifolia_NodeProgression_Diagnostic()
	{
		const int totalHours = 1440;
		const int hoursPerTick = 4;
		var path = Path.Combine(Path.GetTempPath(), $"bergenia-prog-{Guid.NewGuid():N}.jsonl");
		try
		{
			var request = BuildBerganiaNodeRequest("Bergenia Cordifolia", totalHours: totalHours, hoursPerTick: hoursPerTick);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, path, maxHours: totalHours);

			var milestones = new[] { 10u, 50u, 100u, 200u };
			var steps = SimulationHarness.ReadSteps(path).ToList();
			foreach (var t in milestones)
			{
				var step = steps.First(s => s.Timestep == t);
				var ag = step.Plants[0].AboveGround;
				var organs = ag.GroupBy(a => a.Organ).ToDictionary(g => g.Key, g => g.Count());
				Console.WriteLine(
					$"t={t} ({t * hoursPerTick}h) count={ag.Length} " +
					string.Join(" ", organs.Select(kv => $"{kv.Key}:{kv.Value}")));
			}

			var final = steps.Last();
			Assert.True(final.Plants[0].AboveGround.Length > 6,
				$"Expected plant to grow beyond initial 6 agents; got {final.Plants[0].AboveGround.Length} at t={final.Timestep}");
		}
		finally
		{
			if (File.Exists(path)) File.Delete(path);
		}
	}
}
