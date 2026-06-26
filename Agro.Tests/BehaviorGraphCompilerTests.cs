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
	[InlineData("Parent Input")]
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
		};

		foreach (var (name, graph) in DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs())
		{
			Assert.True(BehaviorGraphCompiler.TryCompile(graph, out var compiled, out var err),
				$"Subgraph '{name}' failed: {err}");
			Assert.Contains(compiled!.NodesInOrder, n => n.Kind == GraphNodeKind.Active);

			if (name == "Life support")
			{
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.DeltaEnergy);
				continue;
			}

			if (name == "Photosynthesis")
			{
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.DeltaEnergy);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.DeltaWater);
				Assert.Contains(compiled.NodesInOrder, n => n.Kind == GraphNodeKind.AccumulateProduction);
				continue;
			}

			Assert.DoesNotContain(compiled.NodesInOrder, n =>
				n.Kind is GraphNodeKind.DeltaEnergy or GraphNodeKind.DeltaWater or GraphNodeKind.Growth
					or GraphNodeKind.Death or GraphNodeKind.MakeBud or GraphNodeKind.SetAuxins
					or GraphNodeKind.AccumulateProduction);
		}

		Assert.Equal(2, DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs().Count);
	}

	[Fact]
	public void GraphNodePayload_SerializesCommentWithValue()
	{
		var data = GraphNodePayload.FromNumber(1f, "hours per tick placeholder").ToJsonElement();
		Assert.Equal(1f, data.GetProperty("value").GetSingle());
		Assert.Equal("hours per tick placeholder", data.GetProperty("comment").GetString());
	}

	[Fact]
	public void DefaultSpeciesSubgraphs_IncludeEditorComments()
	{
		var life = DefaultSpeciesGraphBuilder.BuildLifeSupportSubgraph();
		var hoursNode = life.Nodes.Find(n => n.Id == "ls-hours-per-tick");
		Assert.NotNull(hoursNode);
		Assert.Equal("Placeholder: AgroWorld.HoursPerTick (no simulation input node yet)",
			hoursNode.Data.GetProperty("comment").GetString());

		var photo = DefaultSpeciesGraphBuilder.BuildPhotosynthesisSubgraph();
		var accProd = photo.Nodes.Find(n => n.Id == "photo-acc-prod");
		Assert.NotNull(accProd);
		Assert.True(accProd.Data.TryGetProperty("comment", out _));
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
