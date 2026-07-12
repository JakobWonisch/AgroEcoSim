using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

public class PerseaSpeciesParityTests
{
	static SimulationRequest BuildPerseaNodeRequest(int totalHours = ParityTestLimits.MaxHours, float? plantRngFixedUnit = null)
	{
		var perseaEntry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Persea americana");
		var graphs = perseaEntry.Graphs.Select(g => new SpeciesGraphUploadEntry
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
			Plants = [new PlantRequest { SpeciesName = "Persea americana" }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Persea americana"] = graphs,
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Persea americana"] = [.. PerseaSpeciesGraphBuilder.BuildConfiguration()],
			},
		};
	}

	[Fact]
	public void PerseaSpecies_ExtremalParity_FixedUnit0_Structural()
	{
		RunExtremalStructuralParity(fixedUnit: 0f);
	}

	[Fact]
	public void PerseaSpecies_ExtremalParity_FixedUnit1_Structural()
	{
		RunExtremalStructuralParity(fixedUnit: 1f);
	}

	[Fact]
	public void PerseaSpecies_FullParity_Diagnostic()
	{
		const int maxHours = ParityTestLimits.MaxHours;
		var request = BuildPerseaNodeRequest(totalHours: maxHours);
		var traceDir = Path.Combine("ignore", "parity-traces", "persea");
		Directory.CreateDirectory(traceDir);
		var legacyPath = Path.Combine(traceDir, "legacy.jsonl");
		var nodePath = Path.Combine(traceDir, "node.jsonl");
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);
		var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
		if (mismatch is not null)
		{
			Assert.Fail(
				$"Persea structural parity mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
		}
	}

	static void RunExtremalStructuralParity(float fixedUnit, int maxHours = ParityTestLimits.MaxHours)
	{
		var request = BuildPerseaNodeRequest(totalHours: maxHours, plantRngFixedUnit: fixedUnit);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-persea-legacy-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-persea-node-u{fixedUnit}-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: maxHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: maxHours);

			var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
			if (mismatch is not null)
			{
				Assert.Fail(
					$"Persea extremal u={fixedUnit} structural parity mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}
}
