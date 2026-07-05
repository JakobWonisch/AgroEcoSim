using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Agro.BehaviorGraph;

public static class BehaviorGraphCompiler
{
	public static bool TryCompile(global::ExportedGraph graph, [NotNullWhen(true)] out CompiledBehaviorGraph? compiled, [NotNullWhen(false)] out string? error)
	{
		compiled = null;
		error = null;

		if (graph.Nodes is not { Count: > 0 })
		{
			error = "Graph has no nodes.";
			return false;
		}

		var nodes = graph.Nodes;
		var connections = graph.Connections ?? [];

		var idToIndex = new Dictionary<string, int>(nodes.Count);
		for (var i = 0; i < nodes.Count; i++)
		{
			var id = nodes[i].Id;
			if (string.IsNullOrEmpty(id))
			{
				error = $"Node at index {i} has empty id.";
				return false;
			}

			if (!idToIndex.TryAdd(id, i))
			{
				error = $"Duplicate node id '{id}'.";
				return false;
			}
		}

		var incoming = new List<(int Source, string SourceOutput, string TargetInput)>[nodes.Count];
		for (var i = 0; i < incoming.Length; i++)
			incoming[i] = [];

		foreach (var c in connections)
		{
			if (c is null || string.IsNullOrEmpty(c.Target) || string.IsNullOrEmpty(c.Source))
				continue;
			if (!idToIndex.TryGetValue(c.Target, out var ti))
			{
				error = $"Connection references unknown target '{c.Target}'.";
				return false;
			}

			if (!idToIndex.TryGetValue(c.Source, out var si))
			{
				error = $"Connection references unknown source '{c.Source}'.";
				return false;
			}

			var so = c.SourceOutput ?? "out";
			var tin = c.TargetInput ?? "a";
			incoming[ti].Add((si, so, tin));
		}

		var kinds = new GraphNodeKind[nodes.Count];
		var payloads = new (float num, bool boo)[nodes.Count];

		for (var i = 0; i < nodes.Count; i++)
		{
			if (!TryMapNode(nodes[i], out kinds[i], out payloads[i], out var err))
			{
				error = err;
				return false;
			}
		}

		if (!TryTopologicalOrder(nodes.Count, incoming, out var topo, out var cycleErr))
		{
			error = cycleErr;
			return false;
		}

		var compiledNodes = new CompiledNode[topo.Count];
		for (var t = 0; t < topo.Count; t++)
		{
			var idx = topo[t];
			var n = nodes[idx];
			var inputs = new Dictionary<string, List<(int, string)>>();
			foreach (var (src, srcOut, tgtIn) in incoming[idx])
			{
				if (!inputs.TryGetValue(tgtIn, out var list))
				{
					list = [];
					inputs[tgtIn] = list;
				}

				list.Add((src, srcOut));
			}

			var p = payloads[idx];
			var (configId, configIsBool) = ReadConfigurationBinding(n.Data);
			compiledNodes[t] = new CompiledNode
			{
				GraphNodeIndex = idx,
				Id = n.Id,
				Kind = kinds[idx],
				Inputs = inputs,
				NumberConst = p.num,
				BoolConst = p.boo,
				ConfigId = configId,
				ConfigIsBoolean = configIsBool,
			};
		}

		var activeOrigIndices = new List<int>();
		for (var i = 0; i < kinds.Length; i++)
		{
			if (kinds[i] == GraphNodeKind.Active)
				activeOrigIndices.Add(i);
		}

		if (activeOrigIndices.Count != 1)
		{
			error = "Graph must contain exactly one Active node.";
			return false;
		}

		var activeOrig = activeOrigIndices[0];
		var origToTopo = new int[nodes.Count];
		for (var ti = 0; ti < topo.Count; ti++)
			origToTopo[topo[ti]] = ti;

		var activeTopoIndex = origToTopo[activeOrig];
		var activeSubtreeMask = new bool[compiledNodes.Length];

		void MarkUpstream(int origIdx, HashSet<int> visited)
		{
			if (!visited.Add(origIdx))
				return;
			var slot = origToTopo[origIdx];
			activeSubtreeMask[slot] = true;
			var cn = compiledNodes[slot];
			foreach (var kv in cn.Inputs)
			{
				foreach (var (prodOrig, _) in kv.Value)
					MarkUpstream(prodOrig, visited);
			}
		}

		var gateCompiled = compiledNodes[activeTopoIndex];
		if (gateCompiled.Inputs.TryGetValue("isActive", out var activeConns))
		{
			foreach (var (prodOrig, _) in activeConns)
				MarkUpstream(prodOrig, new HashSet<int>());
		}

		compiled = new CompiledBehaviorGraph
		{
			NodesInOrder = compiledNodes,
			ActiveGateTopoIndex = activeTopoIndex,
			ActiveSubtreeMask = activeSubtreeMask,
		};
		return true;
	}

	static bool TryTopologicalOrder(int n, List<(int Source, string SourceOutput, string TargetInput)>[] incoming, out List<int> order, [NotNullWhen(false)] out string? error)
	{
		order = [];
		error = null;
		var inDegree = new int[n];
		for (var i = 0; i < n; i++)
			foreach (var (src, _, _) in incoming[i])
				inDegree[i]++;

		var q = new Queue<int>();
		for (var i = 0; i < n; i++)
		{
			if (inDegree[i] == 0)
				q.Enqueue(i);
		}

		while (q.Count > 0)
		{
			var u = q.Dequeue();
			order.Add(u);
			for (var v = 0; v < n; v++)
			{
				foreach (var (src, _, _) in incoming[v])
				{
					if (src != u)
						continue;
					inDegree[v]--;
					if (inDegree[v] == 0)
						q.Enqueue(v);
				}
			}
		}

		if (order.Count != n)
		{
			error = "Graph contains a cycle (or unresolved dependencies).";
			return false;
		}

		return true;
	}

	static bool TryMapNode(global::GraphNode node, out GraphNodeKind kind, out (float num, bool boo) payload, [NotNullWhen(false)] out string? error)
	{
		payload = (0f, false);
		error = null;
		var label = node.Label ?? "";

		switch (label)
		{
			case "Number Input":
				kind = GraphNodeKind.NumberInput;
				payload.num = ReadNumber(node.Data);
				return true;
			case "Boolean Input":
				kind = GraphNodeKind.BooleanInput;
				payload.boo = ReadBool(node.Data);
				return true;
			case "Configuration Value Input":
				kind = GraphNodeKind.ConfigurationValueInput;
				if (string.IsNullOrWhiteSpace(ReadConfigId(node.Data)))
				{
					error = $"Configuration Value Input node '{node.Id}' requires data.configId.";
					return false;
				}
				return true;
			case "Agent Type Input":
				kind = GraphNodeKind.AgentTypeInput;
				return true;
			case "Phase Input":
				kind = GraphNodeKind.PhaseInput;
				return true;
			case "Agent State Input":
				kind = GraphNodeKind.AgentStateInput;
				return true;
			case "Formation Input":
				kind = GraphNodeKind.FormationInput;
				return true;
			case "Irradiance Input":
				kind = GraphNodeKind.IrradianceInput;
				return true;
			case "Simulation Settings Input":
				kind = GraphNodeKind.SimulationSettingsInput;
				return true;
			case "Random Chance Input":
				kind = GraphNodeKind.RandomChanceInput;
				return true;
			case "Random Accum Chance Input":
				kind = GraphNodeKind.RandomAccumChanceInput;
				return true;
			case "Random Float Var Input":
				kind = GraphNodeKind.RandomFloatVarInput;
				return true;
			case "Parent Wood Cap":
				kind = GraphNodeKind.ParentWoodCap;
				return true;
			case "Clamp Max":
				kind = GraphNodeKind.ClampMax;
				return true;
			case "Delta Energy":
				kind = GraphNodeKind.DeltaEnergy;
				return true;
			case "Delta Water":
				kind = GraphNodeKind.DeltaWater;
				return true;
			case "Delta Wood":
				kind = GraphNodeKind.DeltaWood;
				return true;
			case "Set Wood":
				kind = GraphNodeKind.SetWood;
				return true;
			case "Multiply Energy":
				kind = GraphNodeKind.MultiplyEnergy;
				return true;
			case "Multiply Water":
				kind = GraphNodeKind.MultiplyWater;
				return true;
			case "Set Energy":
				kind = GraphNodeKind.SetEnergy;
				return true;
			case "Set Auxins":
				kind = GraphNodeKind.SetAuxins;
				return true;
			case "Set trySpawn":
				kind = GraphNodeKind.SetTrySpawn;
				return true;
			case "Accumulate Production":
				kind = GraphNodeKind.AccumulateProduction;
				return true;
			case "Accumulate Env Resources":
				kind = GraphNodeKind.AccumulateEnvResources;
				return true;
			case "Accumulate Env Resources Inv":
				kind = GraphNodeKind.AccumulateEnvResourcesInv;
				return true;
			case "Make Bud":
				kind = GraphNodeKind.MakeBud;
				return true;
			case "Create Leaves":
				kind = GraphNodeKind.CreateLeaves;
				return true;
			case "Death":
				kind = GraphNodeKind.Death;
				return true;
			case "Death Parent":
				kind = GraphNodeKind.DeathParent;
				return true;
			case "Death Children":
				kind = GraphNodeKind.DeathChildren;
				return true;
			case "Become Meristem":
				kind = GraphNodeKind.BecomeMeristem;
				return true;
			case "Set Lateral Angle":
				kind = GraphNodeKind.SetLateralAngle;
				return true;
			case "Delta Dominance":
				kind = GraphNodeKind.DeltaDominance;
				return true;
			case "Set Length Var":
				kind = GraphNodeKind.SetLengthVar;
				return true;
			case "Turn Upwards":
				kind = GraphNodeKind.TurnUpwards;
				return true;
			case "Set Was Meristem":
				kind = GraphNodeKind.SetWasMeristem;
				return true;
			case "Become Stem":
				kind = GraphNodeKind.BecomeStem;
				return true;
			case "Become Flower Stem":
				kind = GraphNodeKind.BecomeFlowerStem;
				return true;
			case "Become Flower Meristem":
				kind = GraphNodeKind.BecomeFlowerMeristem;
				return true;
			case "Spawn Meristem":
				kind = GraphNodeKind.SpawnMeristem;
				return true;
			case "Spawn Dichotomous Meristems":
				kind = GraphNodeKind.SpawnDichotomousMeristems;
				return true;
			case "Spawn Bud":
				kind = GraphNodeKind.SpawnBud;
				return true;
			case "Spawn Stem":
				kind = GraphNodeKind.SpawnStem;
				return true;
			case "Spawn Flower Stem":
				kind = GraphNodeKind.SpawnFlowerStem;
				return true;
			case "Spawn Flower Meristem":
				kind = GraphNodeKind.SpawnFlowerMeristem;
				return true;
			case "Spawn Flower Bud":
				kind = GraphNodeKind.SpawnFlowerBud;
				return true;
			case "Spawn Flower Padel":
				kind = GraphNodeKind.SpawnFlowerPadel;
				return true;
			case "Spawn Rhizome":
				kind = GraphNodeKind.SpawnRhizome;
				return true;
			case "Active":
				kind = GraphNodeKind.Active;
				return true;
			case "And":
				kind = GraphNodeKind.And;
				return true;
			case "Or":
				kind = GraphNodeKind.Or;
				return true;
			case "Xor":
				kind = GraphNodeKind.Xor;
				return true;
			case "Not":
				kind = GraphNodeKind.Not;
				return true;
			case "Greater Than":
				kind = GraphNodeKind.GreaterThan;
				return true;
			case "Greater Than or Equal":
				kind = GraphNodeKind.GreaterThanOrEqual;
				return true;
			case "Less Than":
				kind = GraphNodeKind.LessThan;
				return true;
			case "Less Than or Equal":
				kind = GraphNodeKind.LessThanOrEqual;
				return true;
			case "Equal To":
				kind = GraphNodeKind.EqualTo;
				return true;
			case "If / Else":
				kind = GraphNodeKind.IfElse;
				return true;
			case "Add":
				kind = GraphNodeKind.Add;
				return true;
			case "Subtract":
				kind = GraphNodeKind.Subtract;
				return true;
			case "Multiply":
				kind = GraphNodeKind.Multiply;
				return true;
			case "Divide":
				kind = GraphNodeKind.Divide;
				return true;
			case "Integer Divide":
				kind = GraphNodeKind.IntegerDivide;
				return true;
			case "Growth":
				kind = GraphNodeKind.Growth;
				return true;
			default:
				error = $"Unsupported node label '{label}' (id '{node.Id}').";
				kind = default;
				return false;
		}
	}

	static float ReadNumber(JsonElement data)
	{
		if (data.ValueKind == JsonValueKind.Object)
		{
			if (data.TryGetProperty("value", out var v) && v.TryGetSingle(out var f))
				return f;
			if (data.TryGetProperty("num", out var n) && n.TryGetSingle(out var f2))
				return f2;
		}

		return 0f;
	}

	static bool ReadBool(JsonElement data)
	{
		if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("bool", out var b))
		{
			return b.ValueKind switch
			{
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Number => b.TryGetInt32(out var i) && i != 0,
				_ => false,
			};
		}

		return false;
	}

	static string? ReadConfigId(JsonElement data)
	{
		if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("configId", out var id))
			return id.GetString();
		return null;
	}

	static (string? ConfigId, bool IsBoolean) ReadConfigurationBinding(JsonElement data)
	{
		var configId = ReadConfigId(data);
		var isBool = data.ValueKind == JsonValueKind.Object
			&& data.TryGetProperty("configType", out var type)
			&& string.Equals(type.GetString(), "boolean", StringComparison.OrdinalIgnoreCase);
		return (configId, isBool);
	}
}
