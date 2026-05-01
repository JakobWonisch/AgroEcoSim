using System.Collections.Generic;

namespace Agro.BehaviorGraph;

public static class GraphTickInterpreter
{
	public static void Execute(ref AboveGroundAgent agent, PlantSubFormation<AboveGroundAgent> _formation, int _agentId, uint _timestep, CompiledBehaviorGraph graph)
	{
		var outs = new Dictionary<(int NodeIndex, string Socket), WireValue>();

		for (var t = 0; t < graph.NodesInOrder.Length; t++)
		{
			if (!graph.ActiveSubtreeMask[t])
				continue;
			EvaluateNode(graph.NodesInOrder[t], ref agent, outs);
		}

		var gateNode = graph.NodesInOrder[graph.ActiveGateTopoIndex];
		if (!FirstBool(gateNode.Inputs, "isActive", outs))
			return;

		for (var t = 0; t < graph.NodesInOrder.Length; t++)
		{
			if (graph.ActiveSubtreeMask[t])
				continue;
			EvaluateNode(graph.NodesInOrder[t], ref agent, outs);
		}
	}

	static void EvaluateNode(CompiledNode node, ref AboveGroundAgent agent, Dictionary<(int NodeIndex, string Socket), WireValue> outs)
	{
		var g = node.GraphNodeIndex;
		switch (node.Kind)
		{
			case GraphNodeKind.NumberInput:
				outs[(g, "num")] = WireValue.OfFloat(node.NumberConst);
				break;
			case GraphNodeKind.BooleanInput:
				outs[(g, "bool")] = WireValue.OfBool(node.BoolConst);
				break;
			case GraphNodeKind.AgentType:
			case GraphNodeKind.OrganSensors:
				WriteOrganSensors(ref agent, outs, g);
				break;
			case GraphNodeKind.Add:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfFloat(a + b);
				break;
			}
			case GraphNodeKind.Subtract:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfFloat(a - b);
				break;
			}
			case GraphNodeKind.Multiply:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfFloat(a * b);
				break;
			}
			case GraphNodeKind.Divide:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfFloat(b != 0f ? a / b : 0f);
				break;
			}
			case GraphNodeKind.And:
			{
				var a = FirstBool(node.Inputs, "a", outs);
				var b = FirstBool(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(a && b);
				break;
			}
			case GraphNodeKind.Or:
			{
				var a = FirstBool(node.Inputs, "a", outs);
				var b = FirstBool(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(a || b);
				break;
			}
			case GraphNodeKind.Xor:
			{
				var a = FirstBool(node.Inputs, "a", outs);
				var b = FirstBool(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(a ^ b);
				break;
			}
			case GraphNodeKind.Not:
			{
				var a = FirstBool(node.Inputs, "a", outs);
				outs[(g, "out")] = WireValue.OfBool(!a);
				break;
			}
			case GraphNodeKind.GreaterThanOrEqual:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				var ok = node.NumericInclusive ? a >= b : a > b;
				outs[(g, "out")] = WireValue.OfBool(ok);
				break;
			}
			case GraphNodeKind.LessThanOrEqual:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				var ok = node.NumericInclusive ? a <= b : a < b;
				outs[(g, "out")] = WireValue.OfBool(ok);
				break;
			}
			case GraphNodeKind.EqualTo:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(MathF.Abs(a - b) < 1e-6f);
				break;
			}
			case GraphNodeKind.IfElse:
			{
				var cond = FirstBool(node.Inputs, "condition", outs);
				var t = FirstFloat(node.Inputs, "trueValue", outs);
				var f = FirstFloat(node.Inputs, "falseValue", outs);
				outs[(g, "out")] = WireValue.OfFloat(cond ? t : f);
				break;
			}
			case GraphNodeKind.Active:
			case GraphNodeKind.BooleanOutput:
			case GraphNodeKind.NumberOutput:
				break;
			case GraphNodeKind.Growth:
			{
				var dLen = FirstFloat(node.Inputs, "Length", outs);
				var dRad = FirstFloat(node.Inputs, "Radius", outs);
				agent.Length += dLen;
				agent.Radius += dRad;
				break;
			}
			default:
				break;
		}
	}

	static void WriteOrganSensors(ref AboveGroundAgent agent, Dictionary<(int, string), WireValue> outs, int g)
	{
		var o = agent.Organ;
		outs[(g, "type1")] = WireValue.OfBool(o == OrganTypes.Leaf);
		outs[(g, "type2")] = WireValue.OfBool(o == OrganTypes.Stem);
		outs[(g, "type3")] = WireValue.OfBool(o == OrganTypes.Meristem);
		outs[(g, "type4")] = WireValue.OfBool(o == OrganTypes.Petiole);
		outs[(g, "type5")] = WireValue.OfBool(o == OrganTypes.Bud);
	}

	static float FirstFloat(Dictionary<string, List<(int ProducerIndex, string ProducerSocket)>> inputs, string key, Dictionary<(int, string), WireValue> outs)
	{
		if (!inputs.TryGetValue(key, out var list) || list is not { Count: > 0 })
			return 0f;
		var (pi, ps) = list[0];
		return outs.TryGetValue((pi, ps), out var w) ? w.AsFloat() : 0f;
	}

	static bool FirstBool(Dictionary<string, List<(int ProducerIndex, string ProducerSocket)>> inputs, string key, Dictionary<(int, string), WireValue> outs)
	{
		if (!inputs.TryGetValue(key, out var list) || list is not { Count: > 0 })
			return false;
		var (pi, ps) = list[0];
		return outs.TryGetValue((pi, ps), out var w) && w.AsBool();
	}
}
