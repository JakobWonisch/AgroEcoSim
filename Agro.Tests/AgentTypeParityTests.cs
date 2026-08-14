using Agro.Testing;
using Xunit;

namespace Agro.Tests;

/// <summary>
/// Per-organ growth parity for every predefined species: one subject agent per plant,
/// spawn/death mocked, legacy tick vs behavior graphs.
/// </summary>
public class AgentTypeParityTests
{
	static readonly string[] SpeciesNames =
		[.. PredefinedSpeciesCatalog.All.Select(s => s.Name)];

	static SimulationRequest BuildRequest(string speciesName, int totalHours = ParityTestLimits.MaxHours, float? plantRngFixedUnit = null)
	{
		var entry = PredefinedSpeciesCatalog.All.First(s => s.Name == speciesName);
		var graphs = entry.Graphs.Select(g => new SpeciesGraphUploadEntry
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
				SpeciesName = speciesName,
				Position = new Utils.Json.Vector3XYZ { X = 0.5f, Y = 0f, Z = 0.5f },
			}],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				[speciesName] = graphs,
			},
			SpeciesConfiguration = entry.Configuration is { Count: > 0 } config
				? new Dictionary<string, List<BehaviorConfigUploadEntry>> { [speciesName] = config }
				: null,
		};
	}

	public static IEnumerable<object[]> SpeciesOrgansAndFixedUnits()
	{
		foreach (var species in SpeciesNames)
		{
			foreach (var organ in AgentTypeGraphCatalog.DefaultFocusOrgans)
			{
				yield return [species, organ, 0f];
				yield return [species, organ, 1f];
			}
		}
	}

	public static IEnumerable<object[]> SpeciesAndOrgans()
	{
		foreach (var species in SpeciesNames)
		{
			foreach (var organ in AgentTypeGraphCatalog.DefaultFocusOrgans)
				yield return [species, organ];
		}
	}

	public static IEnumerable<object[]> SpeciesAndDiagnosticOrgans()
	{
		foreach (var species in SpeciesNames)
		{
			yield return [species, OrganTypes.Leaf];
			yield return [species, OrganTypes.Meristem];
		}
	}

	static int SubjectIndex(OrganTypes organ) => SimulationHarness.SingleAgentSubjectIndex;

	[Theory]
	[MemberData(nameof(SpeciesOrgansAndFixedUnits))]
	public void SingleAgent_StaysAlone_AndMatchesStructurally(string speciesName, OrganTypes organ, float fixedUnit)
	{
		var request = BuildRequest(speciesName, plantRngFixedUnit: fixedUnit);
		var options = new AgentTypeParityOptions
		{
			Organ = organ,
			MaxHours = ParityTestLimits.MaxHours,
			PlantRngFixedUnit = fixedUnit,
		};

		var subjectIndex = SubjectIndex(organ);
		var compareOptions = TraceCompareOptions.StructuralParity.WithFocusAboveGroundIndices(subjectIndex);

		var slug = SpeciesSlug(speciesName);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-single-legacy-{slug}-{organ}-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-single-node-{slug}-{organ}-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordSingleAgentTrace(request, BehaviorRunMode.Legacy, legacyPath, options);
			SimulationHarness.RecordSingleAgentTrace(request, BehaviorRunMode.Node, nodePath, options);

			foreach (var step in SimulationHarness.ReadSteps(legacyPath))
			{
				Assert.Single(step.Plants);
				Assert.False(step.Plants[0].SeedAlive);
				Assert.Equal(2, step.Plants[0].AboveGround.Length);
				Assert.True(step.Plants[0].AboveGround.Length > subjectIndex);
			}

			foreach (var step in SimulationHarness.ReadSteps(nodePath))
			{
				Assert.Single(step.Plants);
				Assert.False(step.Plants[0].SeedAlive);
				Assert.Equal(2, step.Plants[0].AboveGround.Length);
				Assert.True(step.Plants[0].AboveGround.Length > subjectIndex);
			}

			var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, compareOptions);
			if (mismatch is not null)
			{
				Assert.Fail(
					$"{speciesName} single-agent {organ} u={fixedUnit} mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected} actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	[Theory]
	[MemberData(nameof(SpeciesAndOrgans))]
	public void SingleAgent_OrganSpecificGraphs_Smoke(string speciesName, OrganTypes organ)
	{
		var request = BuildRequest(speciesName, totalHours: 4, plantRngFixedUnit: 1f);
		var options = new AgentTypeParityOptions
		{
			Organ = organ,
			MaxHours = 4,
			PlantRngFixedUnit = 1f,
		};

		var mismatch = SimulationHarness.CompareSingleAgentParity(
			request,
			options,
			TraceCompareOptions.StructuralParity.WithFocusAboveGroundIndices(SubjectIndex(organ)));

		if (mismatch is not null)
		{
			Assert.Fail(
				$"{speciesName} organ-graph smoke {organ} mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
		}
	}

	static string ParityTraceDir()
	{
		var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		return Path.Combine(repoRoot, "ignore", "parity-traces", "agent-type");
	}

	static string SpeciesSlug(string speciesName) =>
		speciesName.Replace(' ', '-').Replace('×', 'x');

	/// <summary>
	/// Diagnostic: keeps traces under ignore/parity-traces/agent-type/ for leaf and meristem of each species.
	/// </summary>
	[Theory]
	[MemberData(nameof(SpeciesAndDiagnosticOrgans))]
	public void SingleAgent_FullParity_Diagnostic(string speciesName, OrganTypes organ)
	{
		const int maxHours = ParityTestLimits.MaxHours;
		var dir = Path.Combine(ParityTraceDir(), SpeciesSlug(speciesName));
		Directory.CreateDirectory(dir);
		var legacyPath = Path.Combine(dir, $"legacy-{organ}.jsonl");
		var nodePath = Path.Combine(dir, $"node-{organ}.jsonl");
		var request = BuildRequest(speciesName, totalHours: maxHours);
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
			$"Species:      {speciesName}",
			$"Legacy trace: {legacyPath}",
			$"Node trace:   {nodePath}",
			$"First {mismatches.Count} mismatch(es) for {organ}:",
		};
		foreach (var m in mismatches)
			lines.Add($"  t={m.Timestep}  {m.Path}  expected={m.Expected}  actual={m.Actual}");

		Assert.Fail(string.Join(Environment.NewLine, lines));
	}
}
