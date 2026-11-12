using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Agro.Tests;

public class FrontendScenarioScratch
{
	readonly ITestOutputHelper _out;
	public FrontendScenarioScratch(ITestOutputHelper output) => _out = output;

	[Fact]
	public void Default_FrontendLike_AllSpeciesConfigs_1440h()
	{
		const int hours = 1440;
		const int hpt = 4;
		var all = PredefinedSpeciesCatalog.All;
		var speciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>();
		var speciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>();
		foreach (var entry in all)
		{
			if (entry.Configuration is not { Count: > 0 }) continue;
			speciesGraphs[entry.Name] = entry.Graphs.Select(g => new SpeciesGraphUploadEntry
			{
				Id = g.Id,
				Name = g.Name,
				Graph = g.Graph,
			}).ToList();
			speciesConfiguration[entry.Name] = [.. entry.Configuration];
		}

		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = hours,
			HoursPerTick = hpt,
			Plants = [new PlantRequest { SpeciesName = "Default" }],
			SpeciesGraphs = speciesGraphs,
			SpeciesConfiguration = speciesConfiguration,
		};

		var legacyPath = Path.Combine(Path.GetTempPath(), "default-legacy-fe.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), "default-node-fe.jsonl");
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: hours);
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: hours);
		var legacy = SimulationHarness.ReadSteps(legacyPath).Last();
		var node = SimulationHarness.ReadSteps(nodePath).Last();
		_out.WriteLine($"Default legacy agents={legacy.Plants[0].AboveGround.Length} leaves={legacy.Plants[0].AboveGround.Count(a => a.Organ == "Leaf")}");
		_out.WriteLine($"Default node agents={node.Plants[0].AboveGround.Length} leaves={node.Plants[0].AboveGround.Count(a => a.Organ == "Leaf")}");
		var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
		Assert.Null(mismatch);
	}

	[Fact]
	public void Persea_FrontendLike_AllSpeciesConfigs_1440h()
	{
		const int hours = 1440;
		const int hpt = 4;
		var all = PredefinedSpeciesCatalog.All;
		var speciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>();
		var speciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>();
		foreach (var entry in all)
		{
			if (entry.Configuration is not { Count: > 0 }) continue;
			speciesGraphs[entry.Name] = entry.Graphs.Select(g => new SpeciesGraphUploadEntry
			{
				Id = g.Id,
				Name = g.Name,
				Graph = g.Graph,
			}).ToList();
			speciesConfiguration[entry.Name] = [.. entry.Configuration];
		}

		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = hours,
			HoursPerTick = hpt,
			Plants = [new PlantRequest { SpeciesName = "Persea americana" }],
			SpeciesGraphs = speciesGraphs,
			SpeciesConfiguration = speciesConfiguration,
		};

		var legacyPath = Path.Combine(Path.GetTempPath(), "persea-legacy-fe.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), "persea-node-fe.jsonl");
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: hours);
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: hours);
		var legacy = SimulationHarness.ReadSteps(legacyPath).Last();
		var node = SimulationHarness.ReadSteps(nodePath).Last();
		_out.WriteLine($"Persea legacy agents={legacy.Plants[0].AboveGround.Length} leaves={legacy.Plants[0].AboveGround.Count(a => a.Organ == "Leaf")}");
		_out.WriteLine($"Persea node agents={node.Plants[0].AboveGround.Length} leaves={node.Plants[0].AboveGround.Count(a => a.Organ == "Leaf")}");
		var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
		Assert.Null(mismatch);
	}

	[Fact]
	public void Default_CorruptedUpload_ZerosIgnoredUsesCatalog()
	{
		var defaultEntry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Default");
		var graphs = defaultEntry.Graphs.Select(g => new SpeciesGraphUploadEntry { Id = g.Id, Name = g.Name, Graph = g.Graph }).ToList();
		var badConfig = defaultEntry.Configuration!.Select(e => new BehaviorConfigUploadEntry
		{
			Id = e.Id, Key = e.Key, Label = e.Label, Usage = e.Usage, Type = e.Type,
			Value = BehaviorGraphJson.Number(0f),
		}).ToList();

		var resolved = BehaviorConfigurationCatalog.ResolveForSpecies(
			new Dictionary<string, List<BehaviorConfigUploadEntry>> { ["Default"] = badConfig },
			"Default");
		Assert.Equal(100f, resolved[DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime].NumberValue);
		Assert.Equal(2f, resolved[DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode].NumberValue);
		Assert.Equal(0.08f, resolved[DefaultSpeciesGraphBuilder.ConfigIds.ShootsGravitaxis].NumberValue, 3);

		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = 200,
			HoursPerTick = 4,
			Plants = [new PlantRequest { SpeciesName = "Default" }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>> { ["Default"] = graphs },
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>> { ["Default"] = badConfig },
		};

		var goodPath = Path.Combine(Path.GetTempPath(), $"default-good-{Guid.NewGuid():N}.jsonl");
		var badPath = Path.Combine(Path.GetTempPath(), $"default-bad-{Guid.NewGuid():N}.jsonl");
		var goodRequest = new SimulationRequest
		{
			Seed = 42,
			TotalHours = 200,
			HoursPerTick = 4,
			Plants = [new PlantRequest { SpeciesName = "Default" }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>> { ["Default"] = graphs },
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Default"] = [.. defaultEntry.Configuration!],
			},
		};
		SimulationHarness.RecordTrace(goodRequest, BehaviorRunMode.Node, goodPath, maxHours: 200);
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, badPath, maxHours: 200);
		var mismatch = TraceComparer.CompareFiles(goodPath, badPath, TraceCompareOptions.StructuralParity);
		Assert.Null(mismatch);
	}
}
