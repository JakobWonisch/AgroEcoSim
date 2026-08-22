using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

public class SpeciesMorphologyTests
{
	static Dictionary<string, BehaviorConfigEntry> ParseConfig(string speciesName, List<BehaviorConfigUploadEntry> entries) =>
		BehaviorConfigurationCatalog.ParseSpeciesConfiguration(
			new Dictionary<string, List<BehaviorConfigUploadEntry>> { [speciesName] = entries },
			speciesName);

	static SpeciesSettings BuildFromConfig(string speciesName, List<BehaviorConfigUploadEntry> entries) =>
		SpeciesSettingsFromConfiguration.Build(speciesName, ParseConfig(speciesName, entries));

	[Fact]
	public void ConfigBuild_PreservesBergeniaRhizomeAndChaining()
	{
		var config = BerganiaTickGraphBuilder.BuildConfiguration(
			BerganiaTickGraphBuilder.BerganiaGraphOptions.BergeniaCordifolia);
		var resolved = BuildFromConfig("Bergenia Cordifolia", config);
		Assert.Equal(0.04f, resolved.RizomeLength);
		Assert.Equal(0.0005f, resolved.pExpandRizome);
		Assert.Equal(0.4f, resolved.crownPitch);
		Assert.Equal(0.015f, resolved.pChaningSeaonns[0]);
		Assert.Equal(0.24f, resolved.LeafLength);
		Assert.Equal(0.04f, resolved.Height);
	}

	[Fact]
	public void Geranium_ConfigBuild_MatchesCatalogInit()
	{
		const float DegToRad = MathF.PI / 180f;
		var config = BerganiaTickGraphBuilder.BuildConfiguration(
			BerganiaTickGraphBuilder.BerganiaGraphOptions.GeraniumMacrorrhizum);
		var resolved = BuildFromConfig("Geranium Macrorrhizum", config);
		Assert.Equal(0.0012f, resolved.pExpandRizome);
		Assert.Equal(0.045f, resolved.RizomeLength);
		Assert.Equal(0.38f, resolved.crownPitch);
		Assert.Equal(0.06f, resolved.LeafLength);
		Assert.Equal(0.3f, resolved.Height);
		Assert.Equal(0f, resolved.NodeDistance);
		Assert.Equal(0.15f, resolved.PetioleLength);
		Assert.Equal(85f * DegToRad, resolved.LeafPitch, 4);
		Assert.Equal(15f * DegToRad, resolved.LateralPitch, 4);
		Assert.Equal(40f * DegToRad, resolved.LateralRoll, 4);
		Assert.Equal(Behavior.Geranium_Macrorrhizum, resolved.Behavior);
	}

	[Fact]
	public void UiSpeciesPayload_IgnoredForNodeGraphMorphology()
	{
		var ui = new SpeciesSettings
		{
			Name = "Bergenia Cordifolia",
			Height = 0.08f,
			LeafLength = 0.99f,
			pExpandRizome = 0.99f,
			RizomeLength = 0.5f,
		};
		var config = BerganiaTickGraphBuilder.BuildConfiguration(
			BerganiaTickGraphBuilder.BerganiaGraphOptions.BergeniaCordifolia);
		var profile = PlantSpeciesProfile.Resolve("Bergenia Cordifolia", new SimulationRequest
		{
			Species = [ui],
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Bergenia Cordifolia"] = config,
			},
		});
		Assert.Equal(0.04f, profile.Morphology.Height);
		Assert.Equal(0.24f, profile.Morphology.LeafLength);
		Assert.Equal(0.0005f, profile.Morphology.pExpandRizome);
		Assert.Equal(0.04f, profile.Morphology.RizomeLength);
	}

	[Fact]
	public void PlantSpeciesProfile_ConfigDrivesMorphology()
	{
		var config = BerganiaTickGraphBuilder.BuildConfiguration(
			BerganiaTickGraphBuilder.BerganiaGraphOptions.BergeniaCordifolia);
		config = config
			.Where(e => e.Id != BerganiaTickGraphBuilder.ConfigIds.PExpandRizome)
			.Append(new BehaviorConfigUploadEntry
			{
				Id = BerganiaTickGraphBuilder.ConfigIds.PExpandRizome,
				Key = "P expand rhizome",
				Label = "P expand rhizome",
				Type = "number",
				Value = BehaviorGraphJson.Number(0.99f),
			})
			.ToList();

		var profile = PlantSpeciesProfile.Resolve("Bergenia Cordifolia", new SimulationRequest
		{
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Bergenia Cordifolia"] = config,
			},
		});

		Assert.Equal(0.99f, profile.Morphology.pExpandRizome);
		Assert.Equal(0.04f, profile.Morphology.RizomeLength);
		Assert.Equal(0.4f, profile.Morphology.crownPitch);
		Assert.Equal(0.015f, profile.Morphology.pChaningSeaonns[0]);
		Assert.Equal(0.99f, profile.BehaviorConfiguration[BerganiaTickGraphBuilder.ConfigIds.PExpandRizome].NumberValue);
	}

	[Fact]
	public void Bergenia_ConfigOnly_NodeMatchesWithOrWithoutUiSpeciesPayload()
	{
		const int totalHours = 200;
		var entry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Bergenia Cordifolia");
		var graphs = entry.Graphs.Select(g => new SpeciesGraphUploadEntry
		{
			Id = g.Id,
			Name = g.Name,
			Graph = g.Graph,
		}).ToList();
		var config = BerganiaTickGraphBuilder.BuildConfiguration(
			BerganiaTickGraphBuilder.BerganiaGraphOptions.BergeniaCordifolia);

		SimulationRequest Request(SpeciesSettings[]? species) => new()
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = 1,
			Plants = [new PlantRequest { SpeciesName = "Bergenia Cordifolia" }],
			Species = species,
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Bergenia Cordifolia"] = graphs,
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Bergenia Cordifolia"] = config,
			},
		};

		var ui = new SpeciesSettings
		{
			Name = "Bergenia Cordifolia",
			Height = 12f,
			LeafLength = 0.99f,
			pExpandRizome = 0.99f,
		};

		var catalogPath = Path.Combine(Path.GetTempPath(), $"berg-cat-{Guid.NewGuid():N}.jsonl");
		var uiPath = Path.Combine(Path.GetTempPath(), $"berg-ui-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(Request(null), BehaviorRunMode.Node, catalogPath, maxHours: totalHours);
			SimulationHarness.RecordTrace(Request([ui]), BehaviorRunMode.Node, uiPath, maxHours: totalHours);
			var catalog = SimulationHarness.ReadSteps(catalogPath).Last().Plants[0].AboveGround;
			var fromUi = SimulationHarness.ReadSteps(uiPath).Last().Plants[0].AboveGround;
			Assert.Equal(catalog.Count(a => a.IsRizome), fromUi.Count(a => a.IsRizome));
			Assert.Equal(catalog.Count(a => a.Organ == "Leaf"), fromUi.Count(a => a.Organ == "Leaf"));
		}
		finally
		{
			if (File.Exists(catalogPath)) File.Delete(catalogPath);
			if (File.Exists(uiPath)) File.Delete(uiPath);
		}
	}

	[Fact]
	public void LegacyResolve_ReturnsUiSpeciesAsIs()
	{
		var ui = new SpeciesSettings
		{
			Name = "Bergenia Cordifolia",
			Height = 12f,
			pExpandRizome = 0.99f,
			RizomeLength = 0.5f,
		};
		var resolved = SpeciesMorphology.Resolve("Bergenia Cordifolia", new SimulationRequest { Species = [ui] });
		Assert.Same(ui, resolved);
		Assert.Equal(0.99f, resolved.pExpandRizome);
		Assert.Equal(0.5f, resolved.RizomeLength);
	}

	[Fact]
	public void ConfigBuild_FreshClone_MatchesSharedTemplateAfterInit()
	{
		var shared = SpeciesSettings.Predefined.First(s => s.Name == "Persea americana");
		var config = PerseaSpeciesGraphBuilder.BuildConfiguration();
		var resolved = BuildFromConfig("Persea americana", config);
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
		Assert.Equal(2400f, resolved.WoodGrowthTime);
	}

	[Fact]
	public void ConfigBuild_ShootsGravitaxis_PreInitBeforePlantInit()
	{
		var shared = SpeciesSettings.Predefined.First(s => s.Name == "Persea americana");
		shared.Init(4);
		Assert.Equal(0.08f, shared.ShootsGravitaxis, 3);

		var fresh = BuildFromConfig("Persea americana", PerseaSpeciesGraphBuilder.BuildConfiguration());
		Assert.Equal(0.2f, fresh.ShootsGravitaxis, 3);
		fresh.Init(4);
		Assert.Equal(0.08f, fresh.ShootsGravitaxis, 3);
	}

	[Fact]
	public void ConfigBuild_TwigsBendingApical_PreInitBeforePlantInit()
	{
		var fresh = BuildFromConfig("Persea americana", PerseaSpeciesGraphBuilder.BuildConfiguration());
		Assert.Equal(0.98f, fresh.TwigsBendingApical, 3);
		fresh.Init(4);
		Assert.Equal(0.98f, fresh.TwigsBendingApical, 3);
	}



	[Fact]
	public void Persea_EarlyStructure_HasFourLeavesPerNode()
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
	public void Persea_GraphWithoutUploadedConfig_MatchesCatalogAt1440h()
	{
		var perseaEntry = PredefinedSpeciesCatalog.All.First(s => s.Name == "Persea americana");
		var graphs = perseaEntry.Graphs.Select(g => new SpeciesGraphUploadEntry
		{
			Id = g.Id,
			Name = g.Name,
			Graph = g.Graph,
		}).ToList();

		var requestWithConfig = new SimulationRequest
		{
			Seed = 42,
			TotalHours = 1440,
			HoursPerTick = 4,
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

		var requestNoUpload = new SimulationRequest
		{
			Seed = 42,
			TotalHours = 1440,
			HoursPerTick = 4,
			Plants = [new PlantRequest { SpeciesName = "Persea americana" }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Persea americana"] = graphs,
			},
		};

		var catalogPath = Path.Combine(Path.GetTempPath(), $"persea-cat-{Guid.NewGuid():N}.jsonl");
		var fallbackPath = Path.Combine(Path.GetTempPath(), $"persea-fallback-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(requestWithConfig, BehaviorRunMode.Node, catalogPath, maxHours: 1440);
			SimulationHarness.RecordTrace(requestNoUpload, BehaviorRunMode.Node, fallbackPath, maxHours: 1440);
			var mismatch = TraceComparer.CompareFiles(catalogPath, fallbackPath, TraceCompareOptions.StructuralParity);
			Assert.Null(mismatch);
		}
		finally
		{
			if (File.Exists(catalogPath)) File.Delete(catalogPath);
			if (File.Exists(fallbackPath)) File.Delete(fallbackPath);
		}
	}

	[Fact]
	public void ResolveForSpecies_MergesCatalogWithUpload()
	{
		var resolved = BehaviorConfigurationCatalog.ResolveForSpecies(null, "Persea americana");
		Assert.True(resolved.ContainsKey(DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime));
		Assert.Equal(2400f, resolved[DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime].NumberValue);
		Assert.Equal(4f, resolved[DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode].NumberValue);
	}

	[Fact]
	public void Persea_ConfigOnly_NodeMatchesWithOrWithoutUiSpeciesPayload()
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
		var config = PerseaSpeciesGraphBuilder.BuildConfiguration().ToList();

		var uiSpecies = new SpeciesSettings
		{
			Name = "Persea americana",
			Height = 99f,
			LeafLength = 0.99f,
			LateralsPerNode = 1,
		};

		SimulationRequest Request(SpeciesSettings[]? species) => new()
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = hoursPerTick,
			Plants = [new PlantRequest { SpeciesName = "Persea americana" }],
			Species = species,
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Persea americana"] = graphs,
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Persea americana"] = config,
			},
		};

		var catalogPath = Path.Combine(Path.GetTempPath(), $"agro-persea-cat-{Guid.NewGuid():N}.jsonl");
		var uiPath = Path.Combine(Path.GetTempPath(), $"agro-persea-ui-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(Request(null), BehaviorRunMode.Node, catalogPath, maxHours: totalHours);
			SimulationHarness.RecordTrace(Request([uiSpecies]), BehaviorRunMode.Node, uiPath, maxHours: totalHours);
			var mismatch = TraceComparer.CompareFiles(catalogPath, uiPath, TraceCompareOptions.StructuralParity);
			Assert.Null(mismatch);
		}
		finally
		{
			if (File.Exists(catalogPath)) File.Delete(catalogPath);
			if (File.Exists(uiPath)) File.Delete(uiPath);
		}
	}
}
