using System.Text.Json;
using Agro.BehaviorGraph;
using Xunit;

namespace Agro.Tests;

public class GraphTickInterpreterTests
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

	static CompiledBehaviorGraph CompileGateAndGrowth(bool gateBool, float growthLen)
	{
		var g = new ExportedGraph
		{
			Nodes =
			[
				N("gate-bool", "Boolean Input", new Dictionary<string, object> { ["bool"] = gateBool }),
				N("gate-act", "Active"),
				N("a", "Number Input", new Dictionary<string, object> { ["value"] = growthLen }),
				N("gr", "Growth"),
			],
			Connections =
			[
				C("gc", "gate-bool", "bool", "gate-act", "isActive"),
				C("e1", "a", "num", "gr", "Length"),
			],
		};
		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		return compiled!;
	}

	[Fact]
	public void Execute_GateFalse_SkipsGrowth()
	{
		var compiled = CompileGateAndGrowth(false, 5f);
		var agent = default(AboveGroundAgent);
		agent.Organ = OrganTypes.Stem;
		agent.Length = 10f;
		var len0 = agent.Length;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(len0, agent.Length);
	}

	[Fact]
	public void Execute_GateTrue_AppliesGrowth()
	{
		var compiled = CompileGateAndGrowth(true, 5f);
		var agent = default(AboveGroundAgent);
		agent.Organ = OrganTypes.Stem;
		agent.Length = 10f;
		var len0 = agent.Length;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(len0 + 5f, agent.Length);
	}

	[Fact]
	public void Execute_TwoCompiledGraphs_AccumulatesGrowth()
	{
		var c1 = CompileGateAndGrowth(true, 0.1f);
		var c2 = CompileGateAndGrowth(true, 0.2f);
		var agent = default(AboveGroundAgent);
		agent.Organ = OrganTypes.Stem;
		agent.Length = 1f;
		var len0 = agent.Length;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, c1);
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, c2);
		Assert.Equal(len0 + 0.1f + 0.2f, agent.Length, 5);
	}

	static CompiledBehaviorGraph CompileWithGate(
		(string id, string label, object? data)[] extraNodes,
		(string id, string src, string srcOut, string tgt, string tgtIn)[]? extraConns = null)
	{
		var nodes = new List<GraphNode>
		{
			N("gate-bool", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
			N("gate-act", "Active"),
		};
		var connections = new List<GraphConnection>
		{
			C("gc", "gate-bool", "bool", "gate-act", "isActive"),
		};
		foreach (var (id, label, data) in extraNodes)
			nodes.Add(N(id, label, data));
		if (extraConns is not null)
			foreach (var (id, src, srcOut, tgt, tgtIn) in extraConns)
				connections.Add(C(id, src, srcOut, tgt, tgtIn));

		var g = new ExportedGraph { Nodes = nodes, Connections = connections };
		Assert.True(BehaviorGraphCompiler.TryCompile(g, out var compiled, out var err), err);
		return compiled!;
	}

	[Fact]
	public void Execute_DeltaEnergy_AppliesWhenGated()
	{
		var compiled = CompileWithGate(
			[("n", "Number Input", new Dictionary<string, object> { ["value"] = 3f }), ("d", "Delta Energy", null)],
			[("e1", "n", "num", "d", "amount")]);
		var agent = default(AboveGroundAgent);
		agent.Energy = 10f;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(13f, agent.Energy);
	}

	[Fact]
	public void Execute_SetTrySpawn_WritesFlag()
	{
		var compiled = CompileWithGate(
			[("b", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }), ("s", "Set trySpawn", null)],
			[("c", "b", "bool", "s", "value")]);
		var agent = default(AboveGroundAgent);
		agent.trySpawn = false;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.True(agent.trySpawn);
	}

	[Fact]
	public void Execute_AgentTypeInput_DoesNotMutateAgent()
	{
		var compiled = CompileWithGate([("os", "Agent Type Input", null)]);
		var agent = default(AboveGroundAgent);
		agent.Organ = OrganTypes.Leaf;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(OrganTypes.Leaf, agent.Organ);
	}

	[Fact]
	public void Execute_ParentWoodCap_NoFormation_PassesValueThrough()
	{
		var compiled = CompileWithGate(
			[("n", "Number Input", new Dictionary<string, object> { ["value"] = 0.7f }), ("cap", "Parent Wood Cap", null)],
			[("c", "n", "num", "cap", "value")]);
		var agent = default(AboveGroundAgent);
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(0f, agent.WoodRatio());
	}

	[Fact]
	public void Execute_ClampMax_ComputesMin()
	{
		var compiled = CompileWithGate(
			[
				("v", "Number Input", new Dictionary<string, object> { ["value"] = 1.5f }),
				("m", "Number Input", new Dictionary<string, object> { ["value"] = 1f }),
				("cl", "Clamp Max", null),
				("set", "Set Wood", null),
			],
			[
				("c1", "v", "num", "cl", "value"),
				("c2", "m", "num", "cl", "max"),
				("c3", "cl", "out", "set", "value"),
			]);
		var agent = default(AboveGroundAgent);
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(1f, agent.WoodRatio(), 5);
	}
}
