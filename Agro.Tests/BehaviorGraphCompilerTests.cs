using System.Text.Json;
using Agro.BehaviorGraph;
using Xunit;

namespace Agro.Tests;

public class BehaviorGraphCompilerTests
{
	static GraphNode N(string id, string label, object? data = null) => new()
	{
		Id = id,
		Label = label,
		Data = data == null ? JsonSerializer.SerializeToElement(new { }) : JsonSerializer.SerializeToElement(data),
		Position = new NodePosition(),
	};

	static GraphConnection C(string id, string source, string sourceOut, string target, string targetIn) => new()
	{
		Id = id,
		Source = source,
		SourceOutput = sourceOut,
		Target = target,
		TargetInput = targetIn,
	};

	/// <summary>Boolean Input(true) → Active.isActive (required for every compile).</summary>
	static (GraphNode[] nodes, GraphConnection[] connections) GatePair(string p)
	{
		var nodes = new[]
		{
			N($"{p}-bool", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
			N($"{p}-act", "Active"),
		};
		var connections = new[]
		{
			C($"{p}-c", $"{p}-bool", "bool", $"{p}-act", "isActive"),
		};
		return (nodes, connections);
	}

	[Fact]
	public void TryCompile_AddChain_TopologicalOrder()
	{
		var (gn, gc) = GatePair("z");
		var g = new ExportedGraph
		{
			Nodes =
			[
				..gn,
				N("a", "Number Input", new Dictionary<string, object> { ["value"] = 2f }),
				N("b", "Number Input", new Dictionary<string, object> { ["value"] = 3f }),
				N("c", "Add"),
			],
			Connections =
			[
				..gc,
				C("e1", "a", "num", "c", "a"),
				C("e2", "b", "num", "c", "b"),
			],
		};

		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		Assert.NotNull(compiled);
		Assert.Equal(5, compiled!.NodesInOrder.Length);
		var kinds = compiled.NodesInOrder.Select(n => n.Kind).ToArray();
		Assert.Contains(GraphNodeKind.NumberInput, kinds);
		Assert.Contains(GraphNodeKind.Add, kinds);
		var add = compiled.NodesInOrder.First(n => n.Kind == GraphNodeKind.Add);
		Assert.True(add.Inputs.ContainsKey("a"));
		Assert.True(add.Inputs.ContainsKey("b"));
	}

	[Fact]
	public void TryCompile_Cycle_Fails()
	{
		var (gn, gc) = GatePair("z");
		var g = new ExportedGraph
		{
			Nodes =
			[
				..gn,
				N("x", "Not"),
				N("y", "Not"),
			],
			Connections =
			[
				..gc,
				C("1", "x", "out", "y", "a"),
				C("2", "y", "out", "x", "a"),
			],
		};

		Assert.False(BehaviorGraphCompiler.TryCompile(g, out _, out var err));
		Assert.NotNull(err);
		Assert.Contains("cycle", err, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void TryCompile_UnknownLabel_Fails()
	{
		var (gn, gc) = GatePair("z");
		var g = new ExportedGraph
		{
			Nodes = [..gn, N("m", "Mystery Node")],
			Connections = [..gc],
		};

		Assert.False(BehaviorGraphCompiler.TryCompile(g, out _, out var err));
		Assert.Contains("Mystery", err);
	}

	[Fact]
	public void TryCompile_Growth_NodeOnly_Ok()
	{
		var (gn, gc) = GatePair("z");
		var g = new ExportedGraph
		{
			Nodes = [..gn, N("gr", "Growth")],
			Connections = [..gc],
		};

		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		Assert.NotNull(compiled);
		Assert.Contains(compiled!.NodesInOrder, n => n.Kind == GraphNodeKind.Growth);
	}

	[Fact]
	public void TryCompile_Growth_WithLengthRadiusInputs()
	{
		var (gn, gc) = GatePair("z");
		var g = new ExportedGraph
		{
			Nodes =
			[
				..gn,
				N("a", "Number Input", new Dictionary<string, object> { ["value"] = 0.01f }),
				N("b", "Number Input", new Dictionary<string, object> { ["value"] = 0.001f }),
				N("gr", "Growth"),
			],
			Connections =
			[
				..gc,
				C("e1", "a", "num", "gr", "Length"),
				C("e2", "b", "num", "gr", "Radius"),
			],
		};

		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		var growth = compiled!.NodesInOrder.Single(n => n.Kind == GraphNodeKind.Growth);
		Assert.True(growth.Inputs.ContainsKey("Length"));
		Assert.True(growth.Inputs.ContainsKey("Radius"));
	}

	[Fact]
	public void TryCompile_NoActive_Fails()
	{
		var g = new ExportedGraph
		{
			Nodes =
			[
				N("a", "Number Input", new Dictionary<string, object> { ["value"] = 1f }),
				N("gr", "Growth"),
			],
			Connections =
			[
				C("e1", "a", "num", "gr", "Length"),
			],
		};

		Assert.False(BehaviorGraphCompiler.TryCompile(g, out _, out var err));
		Assert.Contains("Active", err, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void TryCompile_TwoActive_Fails()
	{
		var g = new ExportedGraph
		{
			Nodes =
			[
				N("b1", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
				N("a1", "Active"),
				N("b2", "Boolean Input", new Dictionary<string, object> { ["bool"] = false }),
				N("a2", "Active"),
			],
			Connections =
			[
				C("c1", "b1", "bool", "a1", "isActive"),
				C("c2", "b2", "bool", "a2", "isActive"),
			],
		};

		Assert.False(BehaviorGraphCompiler.TryCompile(g, out _, out var err));
		Assert.Contains("Active", err, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void TryCompile_ActiveSubtreeMask_ExcludesUnrelatedGrowth()
	{
		var g = new ExportedGraph
		{
			Nodes =
			[
				N("gate-bool", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
				N("gate-act", "Active"),
				N("a", "Number Input", new Dictionary<string, object> { ["value"] = 0.01f }),
				N("b", "Number Input", new Dictionary<string, object> { ["value"] = 0.001f }),
				N("gr", "Growth"),
			],
			Connections =
			[
				C("gc", "gate-bool", "bool", "gate-act", "isActive"),
				C("e1", "a", "num", "gr", "Length"),
				C("e2", "b", "num", "gr", "Radius"),
			],
		};

		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		var mask = compiled!.ActiveSubtreeMask;
		Assert.NotNull(mask);
		var growthTopo = Array.FindIndex(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.Growth);
		Assert.True(growthTopo >= 0);
		Assert.False(mask[growthTopo]);
		var boolTopo = Array.FindIndex(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.BooleanInput);
		Assert.True(boolTopo >= 0);
		Assert.True(mask[boolTopo]);
	}

	[Theory]
	[InlineData("Phase Input")]
	[InlineData("Agent State Input")]
	[InlineData("Formation Input")]
	[InlineData("Irradiance Input")]
	[InlineData("Random Chance Input")]
	[InlineData("Delta Energy")]
	[InlineData("Set trySpawn")]
	[InlineData("Parent Wood Cap")]
	[InlineData("Clamp Max")]
	[InlineData("Make Bud")]
	[InlineData("Spawn Meristem")]
	[InlineData("Death")]
	public void TryCompile_NewNodeLabels_Ok(string label)
	{
		var (gn, gc) = GatePair("z");
		var g = new ExportedGraph
		{
			Nodes = [..gn, N("n", label)],
			Connections = [..gc],
		};
		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		Assert.Contains(compiled!.NodesInOrder, n => n.Kind != GraphNodeKind.Active && n.Kind != GraphNodeKind.BooleanInput);
	}

	[Fact]
	public void TryCompile_AllDefaultSpeciesSubgraphs_Ok()
	{
		var withEffects = new HashSet<string>(StringComparer.Ordinal)
		{
			"Life support",
			"Photosynthesis",
			"Growth leaf",
			"Growth petiole",
			"Growth meristem",
			"Growth stem",
		};

		foreach (var (name, graph) in DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs())
		{
			Assert.True(BehaviorGraphCompiler.TryCompile(graph, out var compiled, out var err),
				$"Subgraph '{name}' failed: {err}");
			Assert.Contains(compiled!.NodesInOrder, n => n.Kind == GraphNodeKind.Active);

			if (name == "Life support")
			{
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.DeltaEnergy);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.SimulationSettingsInput);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.ConfigurationValueInput);
				continue;
			}

			if (name == "Photosynthesis")
			{
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.DeltaEnergy);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.DeltaWater);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.AccumulateProduction);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.AccumulateEnvResources);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.AccumulateEnvResourcesInv);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.ConfigurationValueInput);
				continue;
			}

			if (withEffects.Contains(name))
			{
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.Growth);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.ConfigurationValueInput);
				continue;
			}

			Assert.DoesNotContain(compiled.NodesInOrder, n =>
				n.Kind is GraphNodeKind.DeltaEnergy or GraphNodeKind.DeltaWater or GraphNodeKind.Growth
					or GraphNodeKind.Death or GraphNodeKind.MakeBud or GraphNodeKind.SetAuxins
					or GraphNodeKind.AccumulateProduction or GraphNodeKind.AccumulateEnvResources
					or GraphNodeKind.AccumulateEnvResourcesInv);
		}

		Assert.Equal(6, DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs().Count);
	}

	[Fact]
	public void GraphNodePayload_SerializesCommentWithValue()
	{
		var data = GraphNodePayload.FromNumber(1f, "hours per tick placeholder").ToJsonElement();
		Assert.Equal(1f, data.GetProperty("value").GetSingle());
		Assert.Equal("hours per tick placeholder", data.GetProperty("comment").GetString());
	}

	[Fact]
	public void DefaultSpeciesConfiguration_IncludesTickDefaultConstants()
	{
		var entries = DefaultSpeciesGraphBuilder.BuildDefaultConfiguration();
		Assert.Equal(23, entries.Count);

		var leaf = Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.LeafThickness);
		Assert.Equal("Leaf thickness", leaf.Label);
		Assert.Equal(AboveGroundAgent.LeafThickness, leaf.Value.GetSingle());

		var photoEff = Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.PhotoEfficiency);
		Assert.Equal(AboveGroundAgent.mPhotoEfficiency, photoEff.Value.GetSingle());

		var minIr = Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.MinIrradiance);
		Assert.Equal(0.01f, minIr.Value.GetSingle());

		var surface = Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.LeafSurfaceFactor);
		Assert.Equal(2f, surface.Value.GetSingle());

		Assert.Equal(36f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.PetioleAgeBudMinHours).Value.GetSingle());
		Assert.Equal(4032f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.PetioleAgeBudReferenceHours).Value.GetSingle());
		Assert.Equal(48f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.PetioleUnproductiveMinAgeHours).Value.GetSingle());
		Assert.Equal(0.5f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.UnproductiveProductionThreshold).Value.GetSingle());
		Assert.Equal(1f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.MinDominanceForStemDeath).Value.GetSingle());
		Assert.Equal(320f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.EnoughEnergyFactor).Value.GetSingle());
		Assert.Equal(0.004f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.StemDeathProbabilityBase).Value.GetSingle());
		Assert.Equal(5f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.StemDeathHeightCoeff).Value.GetSingle());
		Assert.Equal(4f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.StemDeathEfficiencyCoeff).Value.GetSingle());
		Assert.Equal(20f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.StemDeathRadiusCoeff).Value.GetSingle());

		var cover = Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.PetioleCoverThreshold);
		var s = SpeciesSettings.Default;
		var expectedCover = MathF.Cos(MathF.PI * 0.5f - s.LateralPitch) * s.PetioleLength * 0.25f;
		Assert.Equal(expectedCover, cover.Value.GetSingle(), 6);

		Assert.Equal(s.LeafLength, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.LeafLength).Value.GetSingle());
		Assert.Equal(s.LeafRadius, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius).Value.GetSingle());
		Assert.Equal(s.PetioleLength, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength).Value.GetSingle());
		Assert.Equal(s.PetioleRadius, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius).Value.GetSingle());
		Assert.Equal(1e-3f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.MeristemGrowthLength).Value.GetSingle());
		Assert.Equal(2e-5f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.MeristemGrowthRadius).Value.GetSingle());
		Assert.Equal(2e-5f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.StemGrowthRadius).Value.GetSingle());
		Assert.Equal(1f, Assert.Single(entries, e => e.Id == DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactor).Value.GetSingle());
	}

	[Fact]
	public void GrowthLeafSubgraph_ReferencesSizeLimitConfig()
	{
		var leaf = DefaultSpeciesGraphBuilder.BuildGrowthLeafSubgraph();
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.LeafLength,
			leaf.Nodes.Find(n => n.Id == "gr-leaf-cfg-len")!.Data.GetProperty("configId").GetString());
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius,
			leaf.Nodes.Find(n => n.Id == "gr-leaf-cfg-rad")!.Data.GetProperty("configId").GetString());
		Assert.Contains(leaf.Nodes, n => n.Label == "Growth");
	}

	[Fact]
	public void StubSubgraphs_ReferenceConfigurationValues()
	{
		var petioleAge = DefaultSpeciesGraphBuilder.BuildPetioleAgeBudSubgraph();
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.PetioleAgeBudMinHours,
			petioleAge.Nodes.Find(n => n.Id == "pab-min-age")!.Data.GetProperty("configId").GetString());

		var stemDeath = DefaultSpeciesGraphBuilder.BuildStemDominanceDeathSubgraph();
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.MinDominanceForStemDeath,
			stemDeath.Nodes.Find(n => n.Id == "sdd-min-dom")!.Data.GetProperty("configId").GetString());

		var cover = DefaultSpeciesGraphBuilder.BuildPetioleCoverBudSubgraph();
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.PetioleCoverThreshold,
			cover.Nodes.Find(n => n.Id == "pcb-cover-threshold")!.Data.GetProperty("configId").GetString());

		var unproductive = DefaultSpeciesGraphBuilder.BuildPetioleUnproductiveDeathSubgraph();
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.PetioleUnproductiveMinAgeHours,
			unproductive.Nodes.Find(n => n.Id == "pud-min-age")!.Data.GetProperty("configId").GetString());
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.UnproductiveProductionThreshold,
			unproductive.Nodes.Find(n => n.Id == "pud-prod-threshold")!.Data.GetProperty("configId").GetString());
	}

	[Fact]
	public void LifeSupportSubgraph_ReferencesLeafThicknessConfig()
	{
		var life = DefaultSpeciesGraphBuilder.BuildLifeSupportSubgraph();
		var leafThick = life.Nodes.Find(n => n.Id == "ls-leaf-thick");
		Assert.NotNull(leafThick);
		Assert.Equal("Configuration Value Input", leafThick.Label);
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.LeafThickness,
			leafThick.Data.GetProperty("configId").GetString());
	}

	[Fact]
	public void PhotosynthesisSubgraph_ReferencesConfigurationValues()
	{
		var photo = DefaultSpeciesGraphBuilder.BuildPhotosynthesisSubgraph();

		var photoEff = photo.Nodes.Find(n => n.Id == "photo-photo-eff");
		Assert.NotNull(photoEff);
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.PhotoEfficiency,
			photoEff.Data.GetProperty("configId").GetString());

		var minIr = photo.Nodes.Find(n => n.Id == "photo-min-ir");
		Assert.NotNull(minIr);
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.MinIrradiance,
			minIr.Data.GetProperty("configId").GetString());

		var surfaceFactor = photo.Nodes.Find(n => n.Id == "photo-surface-factor");
		Assert.NotNull(surfaceFactor);
		Assert.Equal(DefaultSpeciesGraphBuilder.ConfigIds.LeafSurfaceFactor,
			surfaceFactor.Data.GetProperty("configId").GetString());
	}

	[Fact]
	public void DefaultSpeciesSubgraphs_IncludeEditorComments()
	{
		var life = DefaultSpeciesGraphBuilder.BuildLifeSupportSubgraph();
		var simNode = life.Nodes.Find(n => n.Id == "ls-sim");
		Assert.NotNull(simNode);
		Assert.Equal("Simulation Settings Input", simNode.Label);

		var photo = DefaultSpeciesGraphBuilder.BuildPhotosynthesisSubgraph();
		var accEnv = photo.Nodes.Find(n => n.Id == "photo-acc-env");
		Assert.NotNull(accEnv);
		Assert.Equal("Accumulate Env Resources", accEnv.Label);
	}

	[Fact]
	public void TryCompile_SimulationSettingsInput_Ok()
	{
		var (gn, gc) = GatePair("sim");
		var g = new ExportedGraph
		{
			Nodes = [..gn, N("n", "Simulation Settings Input", new { })],
			Connections = [..gc],
		};
		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		Assert.Contains(compiled!.NodesInOrder, n => n.Kind == GraphNodeKind.SimulationSettingsInput);
	}

	[Fact]
	public void TryCompile_ConfigurationValueInput_Ok()
	{
		var (gn, gc) = GatePair("cfg");
		var g = new ExportedGraph
		{
			Nodes = [..gn, N("n", "Configuration Value Input", new { configId = "abc", configType = "number" })],
			Connections = [..gc],
		};
		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		Assert.Contains(compiled!.NodesInOrder, n => n.Kind == GraphNodeKind.ConfigurationValueInput);
	}
}
