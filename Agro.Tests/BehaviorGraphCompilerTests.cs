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

	[Fact]
	public void TryCompile_AddChain_TopologicalOrder()
	{
		var g = new ExportedGraph
		{
			Nodes =
			[
				N("a", "Number Input", new Dictionary<string, object> { ["value"] = 2f }),
				N("b", "Number Input", new Dictionary<string, object> { ["value"] = 3f }),
				N("c", "Add"),
			],
			Connections =
			[
				C("e1", "a", "num", "c", "a"),
				C("e2", "b", "num", "c", "b"),
			],
		};

		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		Assert.NotNull(compiled);
		Assert.Equal(3, compiled!.NodesInOrder.Length);
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
		var g = new ExportedGraph
		{
			Nodes =
			[
				N("x", "Not"),
				N("y", "Not"),
			],
			Connections =
			[
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
		var g = new ExportedGraph
		{
			Nodes = [N("z", "Mystery Node")],
			Connections = [],
		};

		Assert.False(BehaviorGraphCompiler.TryCompile(g, out _, out var err));
		Assert.Contains("Mystery", err);
	}

	[Fact]
	public void TryCompile_Growth_NodeOnly_Ok()
	{
		var g = new ExportedGraph
		{
			Nodes = [N("g", "Growth")],
			Connections = [],
		};

		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		Assert.NotNull(compiled);
		Assert.Single(compiled!.NodesInOrder);
		Assert.Equal(GraphNodeKind.Growth, compiled.NodesInOrder[0].Kind);
	}

	[Fact]
	public void TryCompile_Growth_WithLengthRadiusInputs()
	{
		var g = new ExportedGraph
		{
			Nodes =
			[
				N("a", "Number Input", new Dictionary<string, object> { ["value"] = 0.01f }),
				N("b", "Number Input", new Dictionary<string, object> { ["value"] = 0.001f }),
				N("gr", "Growth"),
			],
			Connections =
			[
				C("e1", "a", "num", "gr", "Length"),
				C("e2", "b", "num", "gr", "Radius"),
			],
		};

		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		var growth = compiled!.NodesInOrder.Single(n => n.Kind == GraphNodeKind.Growth);
		Assert.True(growth.Inputs.ContainsKey("Length"));
		Assert.True(growth.Inputs.ContainsKey("Radius"));
	}
}
