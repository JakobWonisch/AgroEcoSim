using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

public class SpeciesMorphologyTests
{
	[Fact]
	public void FreshPredefinedClone_MatchesSharedTemplateAfterInit()
	{
		var shared = SpeciesSettings.Predefined.First(s => s.Name == "Persea americana");
		var resolved = SpeciesMorphology.Resolve("Persea americana", new SimulationRequest());
		Assert.NotSame(shared, resolved);
		resolved.Init(4);
		shared.Init(4);
		Assert.Equal(shared.DominanceFactors, resolved.DominanceFactors);
		Assert.Equal(720f, resolved.LeafGrowthTime);
		Assert.Equal(shared.PetioleCoverThreshold, resolved.PetioleCoverThreshold);
	}

	[Fact]
	public void CatalogResolve_Persea_LateralsPerNode_IsFour()
	{
		var resolved = SpeciesMorphology.Resolve("Persea americana", new SimulationRequest());
		Assert.Equal(4, resolved.LateralsPerNode);
	}

	[Fact]
	public void Persea_EarlyStructure_HasFourLeavesPerWhorl()
	{
		const int hoursPerTick = 4;
		const int totalHours = 48;
		var perseaEntry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Persea americana");
		var graphs = perseaEntry.Graphs.Select(g => new SpeciesGraphUploadEntry
		{
			Id = g.Id,
			Name = g.Name,
			Graph = g.Graph,
		}).ToList();
		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = hoursPerTick,
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

		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-persea-early-legacy-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-persea-early-node-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath);
			var legacy = SimulationHarness.ReadSteps(legacyPath).Last();
			var node = SimulationHarness.ReadSteps(nodePath).Last();
			Assert.Equal(legacy.Plants[0].AboveGround.Length, node.Plants[0].AboveGround.Length);
			var legacyLeaves = legacy.Plants[0].AboveGround.Count(a => a.Organ == "Leaf");
			var nodeLeaves = node.Plants[0].AboveGround.Count(a => a.Organ == "Leaf");
			Assert.Equal(4, legacyLeaves);
			Assert.Equal(4, nodeLeaves);
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}

	[Fact]
	public void ResolvePaths_ReportPerseaLeafGrowthTime()
	{
		var shared = SpeciesSettings.Predefined.First(s => s.Name == "Persea americana");
		var uiResolved = SpeciesMorphology.Resolve("Persea americana", new SimulationRequest
		{
			Species = [shared],
		});
		var catalogResolved = SpeciesMorphology.Resolve("Persea americana", new SimulationRequest());
		Assert.Equal(720f, uiResolved.LeafGrowthTime);
		Assert.Equal(720f, catalogResolved.LeafGrowthTime);
		Assert.Equal(720f, shared.LeafGrowthTime);
	}

	[Fact]
	public void CatalogResolve_DominanceFactors_MatchLegacyTable()
	{
		var resolved = SpeciesMorphology.Resolve("Default", new SimulationRequest());
		Assert.Equal(1f, resolved.DominanceFactors[1]);
		Assert.Equal(0.7f, resolved.DominanceFactors[2]);
	}

	[Fact]
	public void Persea_WithUiSpeciesPayload_MatchesLegacyAt1440Hours()
	{
		const int totalHours = 1440;
		const int hoursPerTick = 4;
		var perseaEntry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Persea americana");
		var graphs = perseaEntry.Graphs.Select(g => new SpeciesGraphUploadEntry
		{
			Id = g.Id,
			Name = g.Name,
			Graph = g.Graph,
		}).ToList();

		// Wire shape the UI sends after loadPredefined (post-fix serialize).
		var uiSpecies = new SpeciesSettings
		{
			Name = "Persea americana",
			Aka = "Avocado",
			DominanceFactor = 0.7f,
			Height = 12f,
			LeafLength = 0.2f,
			LeafRadius = 0.04f,
			PetioleLength = 0.05f,
			PetioleRadius = 0.007f,
			LeafGrowthTime = 720f,
			LeafGrowthTimeVar = 120f,
			LeafLengthVar = 0.02f,
			LeafRadiusVar = 0.01f,
			PetioleLengthVar = 0.01f,
			PetioleRadiusVar = 0.0005f,
			ShootsGravitaxis = 0.2f,
			TwigsBendingApical = 0.02f,
			WoodGrowthTime = 100f,
			WoodGrowthTimeVar = 10f,
		};

		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = hoursPerTick,
			Plants = [new PlantRequest { SpeciesName = "Persea americana" }],
			Species = [uiSpecies],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Persea americana"] = graphs,
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Persea americana"] = [.. PerseaSpeciesGraphBuilder.BuildConfiguration()],
			},
		};

		var legacyPath = Path.Combine(Path.GetTempPath(), $"agro-persea-ui-wire-{Guid.NewGuid():N}.jsonl");
		var nodePath = Path.Combine(Path.GetTempPath(), $"agro-persea-node-wire-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, legacyPath, maxHours: totalHours);
			SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, nodePath, maxHours: totalHours);
			var mismatch = TraceComparer.CompareFiles(legacyPath, nodePath, TraceCompareOptions.StructuralParity);
			if (mismatch is not null)
			{
				Assert.Fail(
					$"UI-wire Persea parity mismatch at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(legacyPath)) File.Delete(legacyPath);
			if (File.Exists(nodePath)) File.Delete(nodePath);
		}
	}
}
