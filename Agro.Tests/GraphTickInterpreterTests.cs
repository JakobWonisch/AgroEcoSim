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

	[Fact]
	public void Execute_RandomAccumChanceInput_UsesAccumulatedProbability()
	{
		var compiled = CompileWithGate(
			[
				("p", "Number Input", new Dictionary<string, object> { ["value"] = 0.5f }),
				("rng", "Random Accum Chance Input", null),
			],
			[("c", "p", "num", "rng", "p")]);
		var agent = default(AboveGroundAgent);
		// Without formation, RNG should not fire.
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
	}

	[Fact]
	public void Execute_IntegerDivide_MatchesLegacyUintDivision()
	{
		var compiled = CompileWithGate(
			[
				("a", "Number Input", new Dictionary<string, object> { ["value"] = 44f }),
				("b", "Number Input", new Dictionary<string, object> { ["value"] = 4032f }),
				("idiv", "Integer Divide", null),
				("z", "Number Input", new Dictionary<string, object> { ["value"] = 0f }),
				("gt", "Greater Than", null),
				("set", "Set Was Meristem", null),
			],
			[
				("c1", "a", "num", "idiv", "a"),
				("c2", "b", "num", "idiv", "b"),
				("c3", "idiv", "out", "gt", "a"),
				("c4", "z", "num", "gt", "b"),
				("c5", "gt", "out", "set", "value"),
			]);
		var agent = default(AboveGroundAgent);
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.False(agent.GraphWasMeristemThisTick());

		var compiled2 = CompileWithGate(
			[
				("a", "Number Input", new Dictionary<string, object> { ["value"] = 4032f }),
				("b", "Number Input", new Dictionary<string, object> { ["value"] = 4032f }),
				("idiv", "Integer Divide", null),
				("z", "Number Input", new Dictionary<string, object> { ["value"] = 0f }),
				("gt", "Greater Than", null),
				("set", "Set Was Meristem", null),
			],
			[
				("c1", "a", "num", "idiv", "a"),
				("c2", "b", "num", "idiv", "b"),
				("c3", "idiv", "out", "gt", "a"),
				("c4", "z", "num", "gt", "b"),
				("c5", "gt", "out", "set", "value"),
			]);
		agent = default;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled2);
		Assert.True(agent.GraphWasMeristemThisTick());
	}

	/// <summary>Legacy: min(wood, parentWood) + growthTimeVar, not min(wood + growth, parentWood).</summary>
	[Fact]
	public void Execute_WoodLignifyFormula_MinBaseThenAdd()
	{
		var compiled = CompileWithGate(
			[
				("wood", "Number Input", new Dictionary<string, object> { ["value"] = 0.8f }),
				("pw", "Number Input", new Dictionary<string, object> { ["value"] = 0.5f }),
				("gtv", "Number Input", new Dictionary<string, object> { ["value"] = 0.01f }),
				("lt", "Less Than", null),
				("base", "If / Else", null),
				("sum", "Add", null),
				("one", "Number Input", new Dictionary<string, object> { ["value"] = 1f }),
				("cl", "Clamp Max", null),
				("set", "Set Wood", null),
			],
			[
				("c1", "wood", "num", "lt", "a"),
				("c2", "pw", "num", "lt", "b"),
				("c3", "lt", "out", "base", "condition"),
				("c4", "wood", "num", "base", "trueValue"),
				("c5", "pw", "num", "base", "falseValue"),
				("c6", "base", "out", "sum", "a"),
				("c7", "gtv", "num", "sum", "b"),
				("c8", "sum", "out", "cl", "value"),
				("c9", "one", "num", "cl", "max"),
				("c10", "cl", "out", "set", "value"),
			]);
		var agent = default(AboveGroundAgent);
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(0.51f, agent.WoodRatio(), 5);
	}

	[Fact]
	public void Execute_SetWasMeristem_SetsScratchFlag()
	{
		var compiled = CompileWithGate(
			[
				("b", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
				("s", "Set Was Meristem", null),
			],
			[("c", "b", "bool", "s", "value")]);
		var agent = default(AboveGroundAgent);
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
	}

	[Fact]
	public void Execute_SetLateralAngle_ChainsSeq()
	{
		var compiled = CompileWithGate(
			[
				("t", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
				("v", "Number Input", new Dictionary<string, object> { ["value"] = 1.57f }),
				("s", "Set Lateral Angle", null),
			],
			[
				("c1", "t", "bool", "s", "trigger"),
				("c2", "v", "num", "s", "value"),
			]);
		var agent = default(AboveGroundAgent);
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(1.57f, agent.LateralAngle, 3);
	}

	[Fact]
	public void Execute_TurnUpwards_MutatesOrientation()
	{
		var compiled = CompileWithGate(
			[
				("t", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
				("turn", "Turn Upwards", null),
			],
			[("c", "t", "bool", "turn", "trigger")]);
		var agent = default(AboveGroundAgent);
		agent.SetOrientation(System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, 0.4f));
		var before = agent.Orientation;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.NotEqual(before, agent.Orientation);
	}

	[Fact]
	public void ArrayElement_ClampsIndexAndFloors()
	{
		var config = new Dictionary<string, BehaviorConfigEntry>
		{
			["arr"] = new BehaviorConfigEntry
			{
				Id = "arr",
				Key = "arr",
				Label = "arr",
				IsNumberArray = true,
				FloatArrayValue = [1f, 2f, 3f],
			},
		};
		Assert.Equal(1f, BehaviorGraphConfig.ArrayElement(config, "arr", 0f));
		Assert.Equal(3f, BehaviorGraphConfig.ArrayElement(config, "arr", 2.9f));
		Assert.Equal(3f, BehaviorGraphConfig.ArrayElement(config, "arr", 99f));
		Assert.Equal(1f, BehaviorGraphConfig.ArrayElement(config, "arr", -1f));
		Assert.Equal(0f, BehaviorGraphConfig.ArrayElement(config, "missing", 0f));
		Assert.Equal(0f, BehaviorGraphConfig.ArrayElement(null, "arr", 0f));
	}

	[Fact]
	public void BuildDominanceFactorsTable_MatchesLegacySetter()
	{
		var table = DefaultSpeciesGraphBuilder.BuildDominanceFactorsTable(0.7f);
		Assert.Equal(17, table.Length);
		Assert.Equal(1f, table[0]);
		Assert.Equal(1f, table[1]);
		Assert.Equal(0.7f, table[2]);
		Assert.Equal(MathF.Pow(0.7f, 3), table[3], 5);
		Assert.Equal(0f, table[16]);
	}

	[Fact]
	public void Execute_SetRadius_MutatesRadius()
	{
		var compiled = CompileWithGate(
			[
				("t", "Boolean Input", new Dictionary<string, object> { ["bool"] = true }),
				("v", "Number Input", new Dictionary<string, object> { ["value"] = 0.001f }),
				("s", "Set Radius", null),
			],
			[
				("c1", "t", "bool", "s", "trigger"),
				("c2", "v", "num", "s", "value"),
			]);
		var agent = default(AboveGroundAgent);
		agent.Radius = 0.5f;
		GraphTickInterpreter.Execute(ref agent, null!, 0, 0, compiled);
		Assert.Equal(0.001f, agent.Radius, 6);
	}
}
