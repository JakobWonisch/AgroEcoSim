using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

/// <summary>
/// Per-organ growth parity: one agent per plant, spawn/death mocked, legacy tick vs behavior graphs.
/// </summary>
public class AgentTypeParityTests
{
	static SimulationRequest BuildDefaultRequest(int totalHours = ParityTestLimits.MaxHours, float? plantRngFixedUnit = null)
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
			Plants = [new PlantRequest
			{
				SpeciesName = "Default",
				Position = new Utils.Json.Vector3XYZ { X = 0.5f, Y = 0f, Z = 0.5f },
			}],
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

	public static IEnumerable<object[]> VegetativeOrgansAndFixedUnits()
	{
		foreach (var organ in AgentTypeGraphCatalog.DefaultFocusOrgans)
		{
			yield return [organ, 0f];
			yield return [organ, 1f];
		}
	}

	static int SubjectIndex(OrganTypes organ) => organ switch
	{
		OrganTypes.Stem or OrganTypes.Meristem or OrganTypes.RizomeMeristem => 0,
		_ => 1,
	};

	[Theory]
	[MemberData(nameof(VegetativeOrgansAndFixedUnits))]
	public void SingleAgent_StaysAlone_AndMatchesStructurally(OrganTypes organ, float fixedUnit)
	{
		var request = BuildDefaultRequest(plantRngFixedUnit: fixedUnit);
		var options = new AgentTypeParityOptions
		{
			Organ = organ,
			MaxHours = ParityTestLimits.MaxHours,
			PlantRngFixedUnit = fixedUnit,
		};

		var subjectIndex = SubjectIndex(organ);
		var compareOptions = TraceCompareOptions.StructuralParity.WithFocusAboveGroundIndices(subjectIndex);

		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-single-legacy-{organ}-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-single-node-{organ}-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordSingleAgentTrace(request, BehaviorRunMode.Legacy, legacyPath, options);
			SimulationHarness.RecordSingleAgentTrace(request, BehaviorRunMode.Node, nodePath, options);

			foreach (var step in SimulationHarness.ReadSteps(legacyPath))
			{
				Assert.Single(step.Plants);
				Assert.False(step.Plants[0].SeedAlive);
				Assert.InRange(step.Plants[0].AboveGround.Length, 1, 2);
				Assert.True(step.Plants[0].AboveGround.Length > subjectIndex);
			}

			foreach (var step in SimulationHarness.ReadSteps(nodePath))
			{
				Assert.Single(step.Plants);
				Assert.False(step.Plants[0].SeedAlive);
				Assert.InRange(step.Plants[0].AboveGround.Length, 1, 2);
				Assert.True(step.Plants[0].AboveGround.Length > subjectIndex);
			}

			var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, compareOptions);

			if (mismatch is not null)
			{
				Assert.Fail(
					$"Single-agent {organ} u={fixedUnit} mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected} actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	[Theory]
	[InlineData(OrganTypes.Leaf)]
	[InlineData(OrganTypes.Stem)]
	[InlineData(OrganTypes.Meristem)]
	[InlineData(OrganTypes.Petiole)]
	[InlineData(OrganTypes.Bud)]
	public void SingleAgent_OrganSpecificGraphs_Smoke(OrganTypes organ)
	{
		var request = BuildDefaultRequest(totalHours: 4, plantRngFixedUnit: 1f);
		var options = new AgentTypeParityOptions
		{
			Organ = organ,
			MaxHours = 4,
			PlantRngFixedUnit = 1f,
			// Keep all graphs — filtering drops shared/transform paths and is not a fair legacy compare.
		};

		var mismatch = SimulationHarness.CompareSingleAgentParity(
			request,
			options,
			TraceCompareOptions.StructuralParity.WithFocusAboveGroundIndices(SubjectIndex(organ)));

		if (mismatch is not null)
		{
			Assert.Fail(
				$"Organ-graph smoke {organ} mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
		}
	}

	static string ParityTraceDir()
	{
		var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		return Path.Combine(repoRoot, "ignore", "parity-traces", "agent-type");
	}

	/// <summary>
	/// Diagnostic: keeps traces under ignore/parity-traces/agent-type/ for one organ.
	/// </summary>
	[Theory]
	[InlineData(OrganTypes.Leaf)]
	[InlineData(OrganTypes.Meristem)]
	public void SingleAgent_FullParity_Diagnostic(OrganTypes organ)
	{
		const int maxHours = ParityTestLimits.MaxHours;
		var dir = ParityTraceDir();
		Directory.CreateDirectory(dir);
		var legacyPath = Path.Combine(dir, $"legacy-{organ}.jsonl");
		var nodePath = Path.Combine(dir, $"node-{organ}.jsonl");
		var request = BuildDefaultRequest(totalHours: maxHours);
		var options = new AgentTypeParityOptions
		{
			Organ = organ,
			MaxHours = maxHours,
		};

		SimulationHarness.RecordSingleAgentTrace(request, BehaviorRunMode.Legacy, legacyPath, options);
		SimulationHarness.RecordSingleAgentTrace(request, BehaviorRunMode.Node, nodePath, options);

		var compareOptions = new TraceCompareOptions
		{
			Tolerance = 1e-4f,
			IgnoreRng = false,
			IgnorePlantBalances = true,
			FocusAboveGroundIndices = new HashSet<int> { SubjectIndex(organ) },
		};
		var mismatches = TraceComparer.CollectMismatches(legacyPath, nodePath, compareOptions, maxCount: 25);
		if (mismatches.Count == 0)
			return;

		var lines = new List<string>
		{
			$"Legacy trace: {legacyPath}",
			$"Node trace:   {nodePath}",
			$"First {mismatches.Count} mismatch(es) for {organ}:",
		};
		foreach (var m in mismatches)
			lines.Add($"  t={m.Timestep}  {m.Path}  expected={m.Expected}  actual={m.Actual}");

		Assert.Fail(string.Join(Environment.NewLine, lines));
	}
}
