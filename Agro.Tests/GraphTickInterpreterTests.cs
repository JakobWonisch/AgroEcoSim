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
}
