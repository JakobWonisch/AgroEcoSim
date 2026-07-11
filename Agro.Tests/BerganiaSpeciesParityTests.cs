using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

/// <summary>
/// Bergania-tick species legacy vs node traces. Compile tests live in <see cref="BehaviorGraphCompilerTests"/>.
/// Harness tests (~24h) finish in seconds. Diagnostic tests (~72h) take ~20–40s each and are expected to fail until parity tuning.
/// </summary>
public class BerganiaSpeciesParityTests
{
	static SimulationRequest BuildBerganiaNodeRequest(string speciesName, int totalHours = 168, float? plantRngFixedUnit = null)
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
			HoursPerTick = 1,
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

	/// <summary>Fast smoke: 24h legacy + node traces for each Bergania species (~5–10s each).</summary>
	[Theory]
	[InlineData("Geranium Macrorrhizum")]
	[InlineData("Geranium × Cantabrigiense")]
	[InlineData("Bergenia Cordifolia")]
	public void BerganiaSpecies_ParityHarness_RecordsBothTraces(string speciesName)
	{
		var request = BuildBerganiaNodeRequest(speciesName, totalHours: 24);
		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-berg-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-berg-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: 24);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: 24);

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
	/// Records 72h structural parity traces; expected to fail until tuning. Traces under ignore/parity-traces/bergania/.
	/// </summary>
	[Theory]
	[InlineData("Geranium Macrorrhizum")]
	[InlineData("Geranium × Cantabrigiense")]
	public void BerganiaSpecies_StructuralParity_Diagnostic(string speciesName)
	{
		const int maxHours = 72;
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
}
