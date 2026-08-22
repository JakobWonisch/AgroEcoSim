using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

public class SpeciesMorphologyTests
{
	[Fact]
	public void UiSpeciesPayload_DoesNotWipeBergeniaRhizomeAndChaining()
	{
		var ui = new SpeciesSettings
		{
			Name = "Bergenia Cordifolia",
			Height = 12f,
			LateralsPerNode = 2,
			LeafLength = 0.24f,
		};
		var resolved = SpeciesMorphology.ResolveForNodeGraphs("Bergenia Cordifolia", new SimulationRequest { Species = [ui] });
		Assert.Equal(0.04f, resolved.RizomeLength);
		Assert.Equal(0.0005f, resolved.pExpandRizome);
		Assert.Equal(0.4f, resolved.crownPitch);
		Assert.Equal(0.015f, resolved.pChaningSeaonns[0]);
		Assert.Equal(0.24f, resolved.LeafLength);
		Assert.Equal(0.04f, resolved.Height);
	}

	[Fact]
	public void UiSerializeJson_DoesNotWipeGeraniumRhizome()
	{
		const string json = """
			{
			  "Name": "Geranium Macrorrhizum",
			  "Height": 12,
			  "LateralsPerNode": 2,
			  "LeafLength": 0.06,
			  "LeafRadius": 0.03,
			  "PetioleLength": 0.15
			}
			""";
		var ui = System.Text.Json.JsonSerializer.Deserialize(json, AgroJsonSerializerContext.Default.SpeciesSettings)
			?? throw new InvalidOperationException("deserialize failed");
		Assert.Equal(0.005f, ui.pExpandRizome);
		Assert.Equal(0.01f, ui.RizomeLength);

		var resolved = SpeciesMorphology.ResolveForNodeGraphs("Geranium Macrorrhizum", new SimulationRequest { Species = [ui] });
		Assert.Equal(0.0012f, resolved.pExpandRizome);
		Assert.Equal(0.045f, resolved.RizomeLength);
		Assert.Equal(0.38f, resolved.crownPitch);
		Assert.Equal(0.06f, resolved.LeafLength);
		Assert.Equal(0.3f, resolved.Height);
		Assert.Equal(0f, resolved.NodeDistance);
		Assert.Equal(0.15f, resolved.PetioleLength);
		Assert.Equal(85f * (MathF.PI / 180f), resolved.LeafPitch, 4);
		Assert.Equal(15f * (MathF.PI / 180f), resolved.LateralPitch, 4);
		Assert.Equal(Behavior.Geranium_Macrorrhizum, resolved.Behavior);
	}

	[Fact]
	public void Geranium_TreeDefaultHud_KeepsInitShootMorphology()
	{
		const float DegToRad = MathF.PI / 180f;
		var ui = new SpeciesSettings
		{
			Name = "Geranium Macrorrhizum",
			Height = 12f,
			NodeDistance = 0.04f,
			NodeDistanceVar = 0.01f,
			LateralsPerNode = 2,
			LateralPitch = 45f * DegToRad,
			LateralRoll = 0f,
			LeafLength = 0.12f,
			LeafLengthVar = 0.02f,
			LeafRadius = 0.04f,
			LeafGrowthTime = 480f,
			LeafPitch = 20f * DegToRad,
			PetioleLength = 0.05f,
			PetioleLengthVar = 0.01f,
			PetioleRadius = 0.0015f,
		};

		var resolved = SpeciesMorphology.ResolveForNodeGraphs("Geranium Macrorrhizum", new SimulationRequest { Species = [ui] });
		Assert.Equal(0.3f, resolved.Height);
		Assert.Equal(0f, resolved.NodeDistance);
		Assert.Equal(0f, resolved.NodeDistanceVar);
		Assert.Equal(0.06f, resolved.LeafLength);
		Assert.Equal(0.01f, resolved.LeafLengthVar);
		Assert.Equal(0.03f, resolved.LeafRadius);
		Assert.Equal(24 * 7, resolved.LeafGrowthTime);
		Assert.Equal(85f * DegToRad, resolved.LeafPitch, 4);
		Assert.Equal(15f * DegToRad, resolved.LateralPitch, 4);
		Assert.Equal(40f * DegToRad, resolved.LateralRoll, 4);
		Assert.Equal(0.15f, resolved.PetioleLength);
		Assert.Equal(0.05f, resolved.PetioleLengthVar);
		Assert.Equal(0.0018f, resolved.PetioleRadius);
		Assert.Equal(Behavior.Geranium_Macrorrhizum, resolved.Behavior);
	}

	[Fact]
	public void Bergenia_CustomHudHeight_OverlaysInit()
	{
		var ui = new SpeciesSettings
		{
			Name = "Bergenia Cordifolia",
			Height = 0.08f,
			LeafLength = 0.24f,
		};
		var resolved = SpeciesMorphology.ResolveForNodeGraphs("Bergenia Cordifolia", new SimulationRequest { Species = [ui] });
		Assert.Equal(0.08f, resolved.Height);
		Assert.Equal(0.24f, resolved.LeafLength);
	}

	[Fact]
	public void SpeciesConfiguration_DoesNotChangeLegacyInitMorphology()
	{
		var ui = new SpeciesSettings
		{
			Name = "Bergenia Cordifolia",
			Height = 12f,
			LeafLength = 0.24f,
		};

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
			Species = [ui],
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Bergenia Cordifolia"] = config,
			},
		});

		Assert.Equal(0.0005f, profile.Morphology.pExpandRizome);
		Assert.Equal(0.04f, profile.Morphology.RizomeLength);
		Assert.Equal(0.4f, profile.Morphology.crownPitch);
		Assert.Equal(0.015f, profile.Morphology.pChaningSeaonns[0]);
		Assert.Equal(0.99f, profile.BehaviorConfiguration[BerganiaTickGraphBuilder.ConfigIds.PExpandRizome].NumberValue);
	}

	[Fact]
	public void UiSerializeJson_ConfigBackedFields_DoNotOverrideLegacyInit()
	{
		const string json = """
			{
			  "Name": "Bergenia Cordifolia",
			  "Height": 0.04,
			  "LeafLength": 0.24,
			  "pExpandRizome": 0.99,
			  "RizomeLength": 0.5,
			  "crownPitch": 0.1,
			  "pChaningSeaonns": [0.0015, 0.0005, 0.001, 0]
			}
			""";
		var ui = System.Text.Json.JsonSerializer.Deserialize(json, AgroJsonSerializerContext.Default.SpeciesSettings)
			?? throw new InvalidOperationException("deserialize failed");
		var resolved = SpeciesMorphology.ResolveForNodeGraphs("Bergenia Cordifolia", new SimulationRequest { Species = [ui] });
		Assert.Equal(0.0005f, resolved.pExpandRizome);
		Assert.Equal(0.04f, resolved.RizomeLength);
		Assert.Equal(0.4f, resolved.crownPitch);
		Assert.Equal(0.015f, resolved.pChaningSeaonns[0]);
	}

	[Fact]
	public void Bergenia_UiSpeciesPayload_NodeMatchesCatalogStructureAt200Hours()
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

		// Frontend serialize() after loadPredefined sends HUD morphology, but omits
		// rhizome / seasonal chaining. Those must stay on the Init() template.
		var ui = new SpeciesSettings
		{
			Name = "Bergenia Cordifolia",
			Height = 0.04f,
			NodeDistance = 0f,
			NodeDistanceVar = 0f,
			LateralsPerNode = 2,
			LeafLength = 0.24f,
			LeafRadius = 0.09f,
			LeafGrowthTime = 24 * 7 * 12,
			PetioleLength = 0.005f,
			PetioleRadius = 0.004f,
		};
		Assert.Equal(0.005f, ui.pExpandRizome);
		Assert.Equal(0.0015f, ui.pChaningSeaonns[0]);

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
	public void FreshPredefinedClone_MatchesSharedTemplateAfterInit()
	{
		var shared = SpeciesSettings.Predefined.First(s => s.Name == "Persea americana");
		var resolved = SpeciesMorphology.ResolveForNodeGraphs("Persea americana", new SimulationRequest());
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
	public void NodeMorphology_Gravitaxis_NotDoubleInitedAfterLegacySharedInit()
	{
		var shared = SpeciesSettings.Predefined.First(s => s.Name == "Persea americana");
		shared.Init(4);
		Assert.Equal(0.08f, shared.ShootsGravitaxis, 3);

		var fresh = SpeciesMorphology.ResolveForNodeGraphs("Persea americana", new SimulationRequest());
		Assert.Equal(0.2f, fresh.ShootsGravitaxis, 3);
		fresh.Init(4);
		Assert.Equal(0.08f, fresh.ShootsGravitaxis, 3);
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
	public void Persea_WithUiSpeciesPayload_NodeMatchesCatalogAt1440Hours()
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
			LateralsPerNode = 4,
			ShootsGravitaxis = 0.2f,
			TwigsBendingApical = 0.02f,
			WoodGrowthTime = 2400f,
			WoodGrowthTimeVar = 240f,
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
			if (mismatch is not null)
			{
				Assert.Fail(
					$"UI-wire Persea node mismatch vs catalog at t={mismatch.Timestep} path {mismatch.Path}: expected {mismatch.Expected}, actual {mismatch.Actual}");
			}
		}
		finally
		{
			if (File.Exists(catalogPath)) File.Delete(catalogPath);
			if (File.Exists(uiPath)) File.Delete(uiPath);
		}
	}
}
