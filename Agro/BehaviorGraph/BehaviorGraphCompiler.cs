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
		var payloads = new (float num, bool boo, bool inclusive)[nodes.Count];

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
			compiledNodes[t] = new CompiledNode
			{
				GraphNodeIndex = idx,
				Id = n.Id,
				Kind = kinds[idx],
				Inputs = inputs,
				NumberConst = p.num,
				BoolConst = p.boo,
				NumericInclusive = p.inclusive,
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

	static bool TryMapNode(global::GraphNode node, out GraphNodeKind kind, out (float num, bool boo, bool inclusive) payload, [NotNullWhen(false)] out string? error)
	{
		payload = (0f, false, false);
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
			case "Agent Type":
				kind = GraphNodeKind.AgentType;
				return true;
			case "Organ Sensors":
				kind = GraphNodeKind.OrganSensors;
				return true;
			case "Active":
				kind = GraphNodeKind.Active;
				return true;
			case "Boolean Output":
				kind = GraphNodeKind.BooleanOutput;
				return true;
			case "Number Output":
				kind = GraphNodeKind.NumberOutput;
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
			case "Greater Than (or Equal)":
				kind = GraphNodeKind.GreaterThanOrEqual;
				payload.inclusive = ReadInclusiveEqual(node.Data);
				return true;
			case "Less Than (or Equal)":
				kind = GraphNodeKind.LessThanOrEqual;
				payload.inclusive = ReadInclusiveEqual(node.Data);
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

	static bool ReadInclusiveEqual(JsonElement data)
	{
		if (data.ValueKind != JsonValueKind.Object)
			return false;
		if (data.TryGetProperty("equal", out var e) && e.TryGetSingle(out var f))
			return f > 0f;
		return false;
	}
}
