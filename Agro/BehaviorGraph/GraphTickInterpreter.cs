using System.Collections.Generic;
using System.Numerics;

namespace Agro.BehaviorGraph;

public static class GraphTickInterpreter
{
	public static void Execute(ref AboveGroundAgent agent, PlantSubFormation<AboveGroundAgent> formation, int agentId, uint timestep, CompiledBehaviorGraph graph)
	{
		var ctx = new TickEvalContext
		{
			Formation = formation,
			AgentId = agentId,
			Timestep = timestep,
			BehaviorConfiguration = formation?.Plant.BehaviorConfiguration,
		};
		var outs = new Dictionary<(int NodeIndex, string Socket), WireValue>();

		for (var t = 0; t < graph.NodesInOrder.Length; t++)
		{
			if (!graph.ActiveSubtreeMask[t])
				continue;
			var node = graph.NodesInOrder[t];
			if (IsDeferredRandomKind(node.Kind))
				continue;
			EvaluateNode(node, ref agent, ctx, outs);
		}

		var gateNode = graph.NodesInOrder[graph.ActiveGateTopoIndex];
		if (!FirstBool(gateNode.Inputs, "isActive", outs))
			return;

		for (var t = 0; t < graph.NodesInOrder.Length; t++)
		{
			if (!graph.ActiveSubtreeMask[t])
				continue;
			var node = graph.NodesInOrder[t];
			if (!IsDeferredRandomKind(node.Kind))
				continue;
			EvaluateNode(node, ref agent, ctx, outs);
		}

		RunInactiveSubtreePasses(ref agent, ctx, outs, graph, orderedMeristemSpawns: NeedsOrderedMeristemSpawns(graph));
	}

	static void RunInactiveSubtreePasses(
		ref AboveGroundAgent agent,
		TickEvalContext ctx,
		Dictionary<(int NodeIndex, string Socket), WireValue> outs,
		CompiledBehaviorGraph graph,
		bool orderedMeristemSpawns)
	{
		// Config/inputs first, then effect-side deferred RNG, then refresh pure nodes that consume RNG.
		RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.PureCompute);
		RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.DeferredRandom);
		RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.PureComputeRefresh);

		if (orderedMeristemSpawns)
		{
			RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.Effects);
			RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.FlowerMeristemSpawns);
			RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.MeristemSpawns);
		}
		else
		{
			RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.Effects);
			RunInactivePass(ref agent, ctx, outs, graph, InactivePassKind.MeristemSpawns);
		}
	}

	static void RunInactivePass(
		ref AboveGroundAgent agent,
		TickEvalContext ctx,
		Dictionary<(int NodeIndex, string Socket), WireValue> outs,
		CompiledBehaviorGraph graph,
		InactivePassKind pass)
	{
		for (var t = 0; t < graph.NodesInOrder.Length; t++)
		{
			if (graph.ActiveSubtreeMask[t])
				continue;
			EvaluateInactiveNode(graph.NodesInOrder[t], ref agent, ctx, outs, pass);
		}
	}

	static bool NeedsOrderedMeristemSpawns(CompiledBehaviorGraph graph)
	{
		var hasFlowerMeristem = false;
		var hasMeristem = false;
		foreach (var node in graph.NodesInOrder)
		{
			if (node.Kind == GraphNodeKind.SpawnFlowerMeristem)
				hasFlowerMeristem = true;
			else if (node.Kind == GraphNodeKind.SpawnMeristem)
				hasMeristem = true;
		}

		return hasFlowerMeristem && hasMeristem;
	}

	enum InactivePassKind { DeferredRandom, PureCompute, PureComputeRefresh, Effects, FlowerMeristemSpawns, MeristemSpawns }

	static void EvaluateInactiveNode(
		CompiledNode node,
		ref AboveGroundAgent agent,
		TickEvalContext ctx,
		Dictionary<(int NodeIndex, string Socket), WireValue> outs,
		InactivePassKind pass)
	{
		var run = pass switch
		{
			InactivePassKind.DeferredRandom => IsDeferredRandomKind(node.Kind),
			InactivePassKind.PureCompute => IsPureComputeKind(node.Kind),
			InactivePassKind.PureComputeRefresh => IsPureComputeRefreshKind(node.Kind),
			InactivePassKind.FlowerMeristemSpawns => node.Kind == GraphNodeKind.SpawnFlowerMeristem,
			InactivePassKind.MeristemSpawns => node.Kind is GraphNodeKind.SpawnMeristem or GraphNodeKind.CreateLeaves,
			InactivePassKind.Effects => IsEffectKind(node.Kind) && !IsOrderedSpawnKind(node.Kind),
			_ => false,
		};
		if (run)
			EvaluateNode(node, ref agent, ctx, outs);
	}

	static bool IsDeferredRandomKind(GraphNodeKind kind) =>
		kind is GraphNodeKind.RandomAccumChanceInput or GraphNodeKind.RandomFloatVarInput;

	static bool IsPureComputeKind(GraphNodeKind kind) => kind switch
	{
		GraphNodeKind.NumberInput or GraphNodeKind.BooleanInput
			or GraphNodeKind.ConfigurationValueInput or GraphNodeKind.ConfigurationArrayInput
			or GraphNodeKind.AgentTypeInput or GraphNodeKind.PhaseInput or GraphNodeKind.AgentStateInput
			or GraphNodeKind.AgentIdInput or GraphNodeKind.FormationInput or GraphNodeKind.IrradianceInput
			or GraphNodeKind.SimulationSettingsInput or GraphNodeKind.RandomChanceInput
			or GraphNodeKind.ParentWoodCap or GraphNodeKind.ClampMax
			or GraphNodeKind.Add or GraphNodeKind.Subtract or GraphNodeKind.Multiply or GraphNodeKind.Divide
			or GraphNodeKind.IntegerDivide
			or GraphNodeKind.And or GraphNodeKind.Or or GraphNodeKind.Xor or GraphNodeKind.Not
			or GraphNodeKind.GreaterThan or GraphNodeKind.GreaterThanOrEqual or GraphNodeKind.LessThan
			or GraphNodeKind.LessThanOrEqual or GraphNodeKind.EqualTo or GraphNodeKind.IfElse
			or GraphNodeKind.Active => true,
		_ => false,
	};

	static bool IsPureComputeRefreshKind(GraphNodeKind kind) =>
		IsPureComputeKind(kind) && kind != GraphNodeKind.RandomChanceInput;

	static bool IsEffectKind(GraphNodeKind kind) =>
		!IsPureComputeKind(kind) && !IsDeferredRandomKind(kind);

	static bool IsOrderedSpawnKind(GraphNodeKind kind) => kind switch
	{
		GraphNodeKind.SpawnMeristem or GraphNodeKind.SpawnDichotomousMeristems or GraphNodeKind.CreateLeaves
			or GraphNodeKind.SpawnFlowerMeristem => true,
		_ => false,
	};

	static void EvaluateNode(CompiledNode node, ref AboveGroundAgent agent, TickEvalContext ctx, Dictionary<(int NodeIndex, string Socket), WireValue> outs)
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
			case GraphNodeKind.ConfigurationValueInput:
				if (node.ConfigIsBoolean)
				{
					var b = ResolveConfigBool(node.ConfigId, ctx);
					outs[(g, "bool")] = WireValue.OfBool(b);
				}
				else
				{
					var n = ResolveConfigNumber(node.ConfigId, ctx);
					outs[(g, "num")] = WireValue.OfFloat(n);
				}
				break;
			case GraphNodeKind.ConfigurationArrayInput:
			{
				var index = FirstFloat(node.Inputs, "index", outs);
				var value = ResolveConfigArrayElement(node.ConfigId, index, ctx);
				outs[(g, "out")] = WireValue.OfFloat(value);
				break;
			}
			case GraphNodeKind.AgentTypeInput:
				WriteAgentTypeInput(ref agent, outs, g);
				break;
			case GraphNodeKind.PhaseInput:
				WritePhaseInput(ctx, outs, g);
				break;
			case GraphNodeKind.AgentStateInput:
				WriteAgentStateInput(ref agent, ctx, outs, g);
				break;
			case GraphNodeKind.AgentIdInput:
				outs[(g, "agentId")] = WireValue.OfFloat(ctx.AgentId);
				break;
			case GraphNodeKind.FormationInput:
				WriteFormationInput(ref agent, ctx, outs, g);
				break;
			case GraphNodeKind.IrradianceInput:
				WriteIrradianceInput(ctx, outs, g);
				break;
			case GraphNodeKind.SimulationSettingsInput:
				WriteSimulationSettingsInput(ctx, outs, g);
				break;
			case GraphNodeKind.RandomChanceInput:
			{
				var p = FirstFloat(node.Inputs, "p", outs);
				var hit = ctx.HasFormation && ctx.Formation!.Plant.RNG.NextFloat(0f, 1f) < p;
				outs[(g, "out")] = WireValue.OfBool(hit);
				break;
			}
			case GraphNodeKind.RandomAccumChanceInput:
			{
				var p = FirstFloat(node.Inputs, "p", outs);
				var hoursPerTick = ctx.HasFormation ? ctx.Formation!.Plant.World.HoursPerTick : 1;
				var hit = ctx.HasFormation && ctx.Formation!.Plant.RNG.NextFloatAccum(p, hoursPerTick);
				outs[(g, "out")] = WireValue.OfBool(hit);
				break;
			}
			case GraphNodeKind.RandomFloatVarInput:
			{
				var variance = FirstFloat(node.Inputs, "variance", outs);
				var value = ctx.HasFormation ? ctx.Formation!.Plant.RNG.NextFloatVar(variance) : 0f;
				outs[(g, "out")] = WireValue.OfFloat(value);
				break;
			}
			case GraphNodeKind.ParentWoodCap:
			{
				var value = FirstFloat(node.Inputs, "value", outs);
				outs[(g, "out")] = WireValue.OfFloat(ParentWoodCap(ref agent, ctx, value));
				break;
			}
			case GraphNodeKind.ClampMax:
			{
				var value = FirstFloat(node.Inputs, "value", outs);
				var max = FirstFloat(node.Inputs, "max", outs);
				outs[(g, "out")] = WireValue.OfFloat(MathF.Min(value, max));
				break;
			}
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
			case GraphNodeKind.IntegerDivide:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfFloat(IntegerDivideUint(a, b));
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
			case GraphNodeKind.GreaterThan:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(a > b);
				break;
			}
			case GraphNodeKind.GreaterThanOrEqual:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(a >= b);
				break;
			}
			case GraphNodeKind.LessThan:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(a < b);
				break;
			}
			case GraphNodeKind.LessThanOrEqual:
			{
				var a = FirstFloat(node.Inputs, "a", outs);
				var b = FirstFloat(node.Inputs, "b", outs);
				outs[(g, "out")] = WireValue.OfBool(a <= b);
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
				break;
			case GraphNodeKind.Growth:
			{
				var dLen = FirstFloat(node.Inputs, "Length", outs);
				var dRad = FirstFloat(node.Inputs, "Radius", outs);
				agent.Length += dLen;
				agent.Radius += dRad;
				break;
			}
			case GraphNodeKind.DeltaEnergy:
				agent.Energy += FirstFloat(node.Inputs, "amount", outs);
				break;
			case GraphNodeKind.DeltaWater:
				agent.Water_g += FirstFloat(node.Inputs, "amount", outs);
				break;
			case GraphNodeKind.DeltaWood:
				agent.GraphDeltaWood(FirstFloat(node.Inputs, "amount", outs));
				break;
			case GraphNodeKind.SetWood:
				agent.GraphSetWood(FirstFloat(node.Inputs, "value", outs));
				break;
			case GraphNodeKind.MultiplyEnergy:
				agent.Energy *= FirstFloat(node.Inputs, "factor", outs);
				break;
			case GraphNodeKind.MultiplyWater:
				agent.Water_g *= FirstFloat(node.Inputs, "factor", outs);
				break;
			case GraphNodeKind.SetEnergy:
				if (node.Inputs.TryGetValue("trigger", out var setEnergyTriggers) && setEnergyTriggers.Count > 0)
				{
					if (!FirstBool(node.Inputs, "trigger", outs))
						break;
				}
				agent.Energy = FirstFloat(node.Inputs, "value", outs);
				break;
			case GraphNodeKind.SetEnergyToCapacity:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.GraphSetEnergyToCapacity();
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.SetAuxins:
				agent.Auxins = FirstFloat(node.Inputs, "value", outs);
				break;
			case GraphNodeKind.SetTrySpawn:
				if (node.Inputs.TryGetValue("trigger", out var setTryTriggers) && setTryTriggers.Count > 0)
				{
					if (!FirstBool(node.Inputs, "trigger", outs))
						break;
				}
				agent.trySpawn = FirstBool(node.Inputs, "value", outs);
				break;
			case GraphNodeKind.SetRizomeTest:
				if (node.Inputs.TryGetValue("trigger", out var setRizomeTestTriggers) && setRizomeTestTriggers.Count > 0)
				{
					if (!FirstBool(node.Inputs, "trigger", outs))
						break;
				}
				agent.rizomeInfo.test = FirstBool(node.Inputs, "value", outs);
				break;
			case GraphNodeKind.SetRizomeTest2:
				if (node.Inputs.TryGetValue("trigger", out var setRizomeTest2Triggers) && setRizomeTest2Triggers.Count > 0)
				{
					if (!FirstBool(node.Inputs, "trigger", outs))
						break;
				}
				agent.rizomeInfo.test2 = FirstBool(node.Inputs, "value", outs);
				break;
			case GraphNodeKind.SetRizomeTest3:
				if (node.Inputs.TryGetValue("trigger", out var setRizomeTest3Triggers) && setRizomeTest3Triggers.Count > 0)
				{
					if (!FirstBool(node.Inputs, "trigger", outs))
						break;
				}
				agent.rizomeInfo.test3 = FirstBool(node.Inputs, "value", outs);
				break;
			case GraphNodeKind.SetRizomeTest4:
				if (node.Inputs.TryGetValue("trigger", out var setRizomeTest4Triggers) && setRizomeTest4Triggers.Count > 0)
				{
					if (!FirstBool(node.Inputs, "trigger", outs))
						break;
				}
				agent.rizomeInfo.test4 = FirstBool(node.Inputs, "value", outs);
				break;
			case GraphNodeKind.AccumulateProduction:
				agent.GraphAccumulateProduction(FirstFloat(node.Inputs, "amount", outs));
				break;
			case GraphNodeKind.AccumulateEnvResources:
				agent.GraphAccumulateEnvResources(FirstFloat(node.Inputs, "amount", outs));
				break;
			case GraphNodeKind.AccumulateEnvResourcesInv:
				agent.GraphAccumulateEnvResourcesInv(FirstFloat(node.Inputs, "amount", outs));
				break;
			case GraphNodeKind.MakeBud:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var organBefore = agent.Organ;
					var children = ctx.Formation!.GetChildren(ctx.AgentId);
					agent.MakeBud(ctx.Formation, children);
					// Bergania energy depletion: non-petiole organs on a rhizome parent
					// re-align to the parent so the next spring crown pitches up from the ground plane.
					if (organBefore != OrganTypes.Petiole && agent.Parent >= 0
						&& ctx.Formation.GetIsRizome(agent.Parent))
					{
						agent.SetOrientation(ctx.Formation.GetDirection(agent.Parent));
						agent.baseOrientation = agent.Orientation;
						agent.restOrientation = agent.Orientation;
						agent.targetOrientation = agent.Orientation;
					}
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.CreateLeaves:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var plant = ctx.Formation!.Plant;
					var lateral = node.Inputs.ContainsKey("lateralAngle") && node.Inputs["lateralAngle"].Count > 0
						? FirstFloat(node.Inputs, "lateralAngle", outs)
						: agent.LateralAngle + ResolveConfigNumber(DefaultSpeciesGraphBuilder.ConfigIds.LateralRoll, ctx);
					var meristemId = ctx.AgentId;
					if (node.Inputs.TryGetValue("meristemId", out var merIn) && merIn.Count > 0)
					{
						var (pi, ps) = merIn[0];
						if (outs.TryGetValue((pi, ps), out var merWire))
							meristemId = (int)merWire.AsFloat();
					}
					if (meristemId >= 0)
					{
						var layout = MorphologyGraphInputs.ReadLeafLayout(node, outs, ctx);
						AboveGroundAgent.GraphCreateLeaves(ref agent, plant, lateral, meristemId, layout);
					}
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.Death:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
					ctx.Formation!.Death(ctx.AgentId);
				break;
			case GraphNodeKind.DeathParent:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs) && agent.Parent >= 0)
					ctx.Formation!.Death(agent.Parent);
				break;
			case GraphNodeKind.DeathChildren:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					foreach (var child in ctx.Formation!.GetChildren(ctx.AgentId))
						ctx.Formation.Death(child);
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.BecomeMeristem:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.Organ = OrganTypes.Meristem;
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.SetLateralAngle:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.GraphSetLateralAngle(FirstFloat(node.Inputs, "value", outs));
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.DeltaDominance:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.GraphDeltaDominance(FirstFloat(node.Inputs, "count", outs));
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.SetDominance:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.GraphSetDominance(FirstFloat(node.Inputs, "value", outs));
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.SetLengthVar:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.GraphSetLengthVar(FirstFloat(node.Inputs, "value", outs));
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.SetRadius:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.GraphSetRadius(FirstFloat(node.Inputs, "value", outs));
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.TurnUpwards:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					agent.GraphTurnUpwards();
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.ApplyCrownPitch:
				if (FirstBool(node.Inputs, "trigger", outs))
				{
					if (ctx.HasFormation)
					{
						// Legacy draws unused initialYaw immediately before applying crown pitch.
						_ = ctx.Formation!.Plant.RNG.NextFloat(-MathF.PI, MathF.PI);
					}
					var pitch = node.Inputs.ContainsKey("crownPitch") && node.Inputs["crownPitch"].Count > 0
						? FirstFloat(node.Inputs, "crownPitch", outs)
						: ResolveConfigNumber(BerganiaTickGraphBuilder.ConfigIds.CrownPitch, ctx);
					agent.GraphApplyCrownPitch(pitch);
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.SetWasMeristem:
				if (FirstBool(node.Inputs, "value", outs))
					agent.GraphSetWasMeristemThisTick(true);
				break;
			case GraphNodeKind.BecomeStem:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var plant = ctx.Formation!.Plant;
					var world = plant.World;
					var woodTime = ResolveConfigNumber(DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime, ctx);
					if (woodTime <= 0f)
						woodTime = DefaultSpeciesGraphBuilder.DefaultTickConstants.WoodGrowthTime;
					var woodTimeVar = ResolveConfigNumber(DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTimeVar, ctx);
					if (woodTimeVar <= 0f)
						woodTimeVar = DefaultSpeciesGraphBuilder.DefaultTickConstants.WoodGrowthTimeVar;
					agent.Organ = OrganTypes.Stem;
					agent.GraphSetGrowthTimeVar(world.HoursPerTick / (woodTime + plant.RNG.NextFloatVar(woodTimeVar)));
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.BecomeFlowerStem:
				if (FirstBool(node.Inputs, "trigger", outs))
					agent.Organ = OrganTypes.FlowerStem;
				break;
			case GraphNodeKind.BecomeFlowerMeristem:
				if (FirstBool(node.Inputs, "trigger", outs))
					agent.Organ = OrganTypes.FlowerMeristem;
				break;
			case GraphNodeKind.SpawnMeristem:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					var childId = SpawnEffects.SpawnChild(
						ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, OrganTypes.Meristem, twig, lateralRoll);
					outs[(g, "childId")] = WireValue.OfFloat(childId);
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
				{
					outs[(g, "childId")] = WireValue.OfFloat(-1f);
					outs[(g, "seq")] = WireValue.OfBool(false);
				}
				break;
			case GraphNodeKind.SpawnDichotomousMeristems:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					var (m1, m2, pitch) = SpawnEffects.SpawnDichotomousMeristems(
						ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, ctx.BehaviorConfiguration, twig, lateralRoll);
					outs[(g, "childId1")] = WireValue.OfFloat(m1);
					outs[(g, "childId2")] = WireValue.OfFloat(m2);
					outs[(g, "lateralPitch")] = WireValue.OfFloat(pitch);
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
				{
					outs[(g, "childId1")] = WireValue.OfFloat(-1f);
					outs[(g, "childId2")] = WireValue.OfFloat(-1f);
					outs[(g, "lateralPitch")] = WireValue.OfFloat(0f);
					outs[(g, "seq")] = WireValue.OfBool(false);
				}
				break;
			case GraphNodeKind.SpawnBud:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					SpawnEffects.SpawnChild(ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, OrganTypes.Bud, twig, lateralRoll);
				}
				break;
			case GraphNodeKind.SpawnStem:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					SpawnEffects.SpawnChild(ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, OrganTypes.Stem, twig, lateralRoll);
				}
				break;
			case GraphNodeKind.SpawnFlowerStem:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					SpawnEffects.SpawnChild(ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, OrganTypes.FlowerStem, twig, lateralRoll);
				}
				break;
			case GraphNodeKind.SpawnFlowerMeristem:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					SpawnEffects.SpawnFlowerMeristemChild(
						ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, twig, lateralRoll);
					outs[(g, "seq")] = WireValue.OfBool(true);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			case GraphNodeKind.SpawnFlowerBud:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					SpawnEffects.SpawnChild(ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, OrganTypes.FlowerBud, twig, lateralRoll);
				}
				break;
			case GraphNodeKind.SpawnFlowerPadel:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var twig = MorphologyGraphInputs.ReadTwigOrientation(node, outs, ctx);
					var lateralRoll = MorphologyGraphInputs.ReadLateralRoll(node, outs, ctx);
					SpawnEffects.SpawnChild(ref agent, ctx.Formation!, ctx.AgentId, ctx.Timestep, OrganTypes.FlowerPadel, twig, lateralRoll);
				}
				break;
			case GraphNodeKind.SpawnRhizome:
				if (ctx.HasFormation && FirstBool(node.Inputs, "trigger", outs))
				{
					var yawOffset = node.Inputs.ContainsKey("yawOffset") && node.Inputs["yawOffset"].Count > 0
						? FirstFloat(node.Inputs, "yawOffset", outs)
						: 0f;
					var rootYaw = node.Inputs.ContainsKey("rootYawOffset") && node.Inputs["rootYawOffset"].Count > 0
						? FirstFloat(node.Inputs, "rootYawOffset", outs)
						: yawOffset;
					var rhizome = MorphologyGraphInputs.ReadRhizomeSpawn(node, outs, ctx);
					var spawned = SpawnEffects.TrySpawnRhizome(
						ref agent, ctx.Formation!, ctx.AgentId, rhizome, yawOffset, rootYaw);
					outs[(g, "seq")] = WireValue.OfBool(spawned);
				}
				else
					outs[(g, "seq")] = WireValue.OfBool(false);
				break;
			default:
				break;
		}
	}

	static void WriteAgentTypeInput(ref AboveGroundAgent agent, Dictionary<(int, string), WireValue> outs, int g)
	{
		var o = agent.Organ;
		outs[(g, "leaf")] = WireValue.OfBool(o == OrganTypes.Leaf);
		outs[(g, "stem")] = WireValue.OfBool(o == OrganTypes.Stem);
		outs[(g, "meristem")] = WireValue.OfBool(o == OrganTypes.Meristem);
		outs[(g, "petiole")] = WireValue.OfBool(o == OrganTypes.Petiole);
		outs[(g, "bud")] = WireValue.OfBool(o == OrganTypes.Bud);
		outs[(g, "flowerStem")] = WireValue.OfBool(o == OrganTypes.FlowerStem);
		outs[(g, "flowerMeristem")] = WireValue.OfBool(o == OrganTypes.FlowerMeristem);
		outs[(g, "flowerBud")] = WireValue.OfBool(o == OrganTypes.FlowerBud);
		outs[(g, "flowerPadel")] = WireValue.OfBool(o == OrganTypes.FlowerPadel);
		outs[(g, "flowerPetiol")] = WireValue.OfBool(o == OrganTypes.FlowerPetiol);
		outs[(g, "flowerBaseBud")] = WireValue.OfBool(o == OrganTypes.FlowerBaseBud);
		outs[(g, "fruit")] = WireValue.OfBool(o == OrganTypes.Fruit);
		outs[(g, "rizomeMeristem")] = WireValue.OfBool(o == OrganTypes.RizomeMeristem);
	}

	static void WritePhaseInput(TickEvalContext ctx, Dictionary<(int, string), WireValue> outs, int g)
	{
		if (!ctx.HasFormation)
		{
			outs[(g, "preFlower")] = WireValue.OfBool(false);
			outs[(g, "flowering")] = WireValue.OfBool(false);
			outs[(g, "postFlower")] = WireValue.OfBool(false);
			outs[(g, "resetPending")] = WireValue.OfBool(false);
			outs[(g, "phaseIndex")] = WireValue.OfFloat(0f);
			return;
		}

		var plant = ctx.Formation!.Plant;
		var species = plant.Parameters;
		var ageHours = ctx.Timestep * plant.World.HoursPerTick;
		var ageTemp = ageHours % (365f * 24f);
		var floweringStart = species.FloweringStartAgeHours;
		var floweringEnd = species.FloweringEndAgeHours;

		SeasonalPhase phase;
		if (ageTemp < floweringStart)
			phase = SeasonalPhase.PreFlower;
		else if (ageTemp <= floweringEnd)
			phase = SeasonalPhase.Flowering;
		else if (ageTemp < floweringEnd + 24f * 100f)
			phase = SeasonalPhase.PostFlower;
		else
			phase = SeasonalPhase.ResetPending;

		outs[(g, "preFlower")] = WireValue.OfBool(phase == SeasonalPhase.PreFlower);
		outs[(g, "flowering")] = WireValue.OfBool(phase == SeasonalPhase.Flowering);
		outs[(g, "postFlower")] = WireValue.OfBool(phase == SeasonalPhase.PostFlower);
		outs[(g, "resetPending")] = WireValue.OfBool(phase == SeasonalPhase.ResetPending);
		outs[(g, "phaseIndex")] = WireValue.OfFloat((float)phase);
	}

	static void WriteAgentStateInput(ref AboveGroundAgent agent, TickEvalContext ctx, Dictionary<(int, string), WireValue> outs, int g)
	{
		var world = ctx.HasFormation ? ctx.Formation!.Plant.World : null;
		var ageHours = world is not null ? agent.AgeHours(ctx.Timestep, world) : 0f;
		outs[(g, "energy")] = WireValue.OfFloat(agent.Energy);
		outs[(g, "water")] = WireValue.OfFloat(agent.Water_g);
		outs[(g, "length")] = WireValue.OfFloat(agent.Length);
		outs[(g, "radius")] = WireValue.OfFloat(agent.Radius);
		outs[(g, "wood")] = WireValue.OfFloat(agent.WoodRatio());
		outs[(g, "ageHours")] = WireValue.OfFloat(ageHours);
		outs[(g, "isRizome")] = WireValue.OfBool(agent.isRizome);
		outs[(g, "trySpawn")] = WireValue.OfBool(agent.trySpawn);
		outs[(g, "lengthVar")] = WireValue.OfFloat(agent.GraphLengthVar());
		outs[(g, "radiusVar")] = WireValue.OfFloat(agent.GraphRadiusVar());
		outs[(g, "growthTimeVar")] = WireValue.OfFloat(agent.GraphGrowthTimeVar());
		outs[(g, "dominanceLevel")] = WireValue.OfFloat(agent.GraphDominanceLevel());
		outs[(g, "parentRadiusAtBirth")] = WireValue.OfFloat(agent.GraphParentRadiusAtBirth());
		outs[(g, "previousDayEnvResources")] = WireValue.OfFloat(agent.GraphPreviousDayEnvResources());
		outs[(g, "previousDayProductionInv")] = WireValue.OfFloat(agent.GraphPreviousDayProductionInv());
		outs[(g, "energyStorageCapacity")] = WireValue.OfFloat(agent.GraphEnergyStorageCapacity());
		outs[(g, "wasMeristemThisTick")] = WireValue.OfBool(agent.GraphWasMeristemThisTick());
		outs[(g, "rizomeDepth")] = WireValue.OfFloat(agent.rizomeInfo.rizomeDepth);
		outs[(g, "rizomeTest")] = WireValue.OfBool(agent.rizomeInfo.test);
		outs[(g, "rizomeTest2")] = WireValue.OfBool(agent.rizomeInfo.test2);
		outs[(g, "rizomeTest3")] = WireValue.OfBool(agent.rizomeInfo.test3);
		outs[(g, "rizomeTest4")] = WireValue.OfBool(agent.rizomeInfo.test4);
	}

	static void WriteFormationInput(ref AboveGroundAgent agent, TickEvalContext ctx, Dictionary<(int, string), WireValue> outs, int g)
	{
		if (!ctx.HasFormation || agent.Parent < 0)
		{
			outs[(g, "parentIsRhizome")] = WireValue.OfBool(false);
			outs[(g, "parentWood")] = WireValue.OfFloat(agent.WoodRatio());
			outs[(g, "parentLeaf")] = WireValue.OfBool(false);
			outs[(g, "parentStem")] = WireValue.OfBool(false);
			outs[(g, "parentMeristem")] = WireValue.OfBool(false);
			outs[(g, "parentPetiole")] = WireValue.OfBool(false);
			outs[(g, "parentBud")] = WireValue.OfBool(false);
			outs[(g, "parentAuxins")] = WireValue.OfFloat(0f);
			outs[(g, "grandparentAuxins")] = WireValue.OfFloat(0f);
			outs[(g, "parentDominance")] = WireValue.OfFloat(0f);
			outs[(g, "parentBaseRadius")] = WireValue.OfFloat(float.MaxValue);
			outs[(g, "hasChildren")] = WireValue.OfBool(false);
			outs[(g, "childrenProductionSum")] = WireValue.OfFloat(0f);
			outs[(g, "agentHeightRatio")] = WireValue.OfFloat(0f);
			outs[(g, "auxinLocalMinimum")] = WireValue.OfBool(false);
		}
		else
		{
			var formation = ctx.Formation!;
			var parent = agent.Parent;
			var parentOrgan = formation.GetOrgan(parent);
			var parentIsRhizome = formation.GetIsRizome(parent);
			outs[(g, "parentIsRhizome")] = WireValue.OfBool(parentIsRhizome);
			outs[(g, "parentWood")] = WireValue.OfFloat(formation.GetWoodRatio(parent));
			outs[(g, "parentLeaf")] = WireValue.OfBool(parentOrgan == OrganTypes.Leaf);
			outs[(g, "parentStem")] = WireValue.OfBool(parentOrgan == OrganTypes.Stem);
			outs[(g, "parentMeristem")] = WireValue.OfBool(parentOrgan == OrganTypes.Meristem);
			outs[(g, "parentPetiole")] = WireValue.OfBool(parentOrgan == OrganTypes.Petiole);
			outs[(g, "parentBud")] = WireValue.OfBool(parentOrgan == OrganTypes.Bud);
			outs[(g, "parentAuxins")] = WireValue.OfFloat(formation.GetAuxins(parent));
			var grandparent = formation.GetParent(parent);
			outs[(g, "grandparentAuxins")] = WireValue.OfFloat(
				grandparent < 0 ? 0f : formation.GetAuxins(grandparent));
			outs[(g, "parentDominance")] = WireValue.OfFloat(formation.GetDominance(parent));
			outs[(g, "parentBaseRadius")] = WireValue.OfFloat(formation.GetBaseRadius(parent));
			var children = formation.GetChildren(ctx.AgentId);
			outs[(g, "hasChildren")] = WireValue.OfBool(children is { Count: > 0 });
			var productionSum = 0f;
			if (children is not null)
			{
				for (var i = 0; i < children.Count; i++)
					productionSum += formation.GetDailyProductionInv(children[i]);
			}
			outs[(g, "childrenProductionSum")] = WireValue.OfFloat(productionSum);
			var height = formation.Height;
			outs[(g, "agentHeightRatio")] = WireValue.OfFloat(
				height > 1e-6f ? formation.GetBaseCenter(ctx.AgentId).Y / height : 0f);
			outs[(g, "auxinLocalMinimum")] = WireValue.OfBool(
				ComputeAuxinLocalMinimum(formation, ref agent, ctx));
		}

		if (ctx.HasFormation)
		{
			var formation = ctx.Formation!;
			var plant = formation.Plant;
			outs[(g, "dailyProductionMax")] = WireValue.OfFloat(formation.DailyProductionMax);
			outs[(g, "dailyResourceMax")] = WireValue.OfFloat(formation.DailyResourceMax);
			outs[(g, "dailyEfficiencyMax")] = WireValue.OfFloat(formation.DailyEfficiencyMax);
			outs[(g, "waterBalance")] = WireValue.OfFloat(plant.WaterBalance);
			outs[(g, "energyProductionMax")] = WireValue.OfFloat(plant.EnergyProductionMax);
		}
		else
		{
			outs[(g, "dailyProductionMax")] = WireValue.OfFloat(0f);
			outs[(g, "dailyResourceMax")] = WireValue.OfFloat(0f);
			outs[(g, "dailyEfficiencyMax")] = WireValue.OfFloat(0f);
			outs[(g, "waterBalance")] = WireValue.OfFloat(0f);
			outs[(g, "energyProductionMax")] = WireValue.OfFloat(0f);
		}
	}

	static bool ComputeAuxinLocalMinimum(
		PlantSubFormation<AboveGroundAgent> formation,
		ref AboveGroundAgent agent,
		TickEvalContext ctx)
	{
		if (agent.Parent < 0)
			return false;

		var auxinsThreshold = BehaviorGraphConfig.Number(
			ctx.BehaviorConfiguration, DefaultSpeciesGraphBuilder.ConfigIds.AuxinsThreshold,
			DefaultSpeciesGraphBuilder.DefaultTickConstants.AuxinsThreshold);
		var parentAuxins = formation.GetAuxins(agent.Parent);
		if (parentAuxins >= auxinsThreshold)
			return false;

		var ascendantIndex = formation.GetParent(agent.Parent);
		var localMinimum = ascendantIndex < 0 || formation.GetAuxins(ascendantIndex) >= parentAuxins;

		if (localMinimum)
		{
			foreach (var child in formation.GetChildren(agent.Parent))
			{
				if (formation.GetOrgan(child) == OrganTypes.Stem && formation.GetAuxins(child) <= parentAuxins)
				{
					localMinimum = false;
					break;
				}
			}
		}

		return localMinimum;
	}

	static void WriteIrradianceInput(TickEvalContext ctx, Dictionary<(int, string), WireValue> outs, int g)
	{
		if (!ctx.HasFormation)
		{
			outs[(g, "irradiance")] = WireValue.OfFloat(0f);
			return;
		}

		var ir = ctx.Formation!.Plant.World.Irradiance.GetIrradiance(ctx.Formation, ctx.AgentId);
		outs[(g, "irradiance")] = WireValue.OfFloat(ir);
	}

	static void WriteSimulationSettingsInput(TickEvalContext ctx, Dictionary<(int, string), WireValue> outs, int g)
	{
		var hoursPerTick = ctx.HasFormation ? ctx.Formation!.Plant.World.HoursPerTick : 0f;
		outs[(g, "hoursPerTick")] = WireValue.OfFloat(hoursPerTick);
	}

	/// <summary>Caps a post-increment value to parent wood (utility node; lignify uses min-then-add in graph).</summary>
	static float ParentWoodCap(ref AboveGroundAgent agent, TickEvalContext ctx, float value)
	{
		if (!ctx.HasFormation || agent.Parent < 0)
			return value;

		var parentWood = ctx.Formation!.GetWoodRatio(agent.Parent);
		return value <= parentWood ? value : parentWood;
	}

	static float ResolveConfigNumber(string? configId, TickEvalContext ctx)
	{
		if (BehaviorGraphConfig.TryNumber(ctx.BehaviorConfiguration, configId ?? "", out var fromConfig))
			return fromConfig;
		return 0f;
	}

	internal static float ResolveConfigNumberPublic(string? configId, TickEvalContext ctx) => ResolveConfigNumber(configId, ctx);

	internal static float FirstFloatPublic(
		Dictionary<string, List<(int ProducerIndex, string ProducerSocket)>> inputs,
		string key,
		Dictionary<(int, string), WireValue> outs)
		=> FirstFloat(inputs, key, outs);

	static float ResolveConfigArrayElement(string? configId, float index, TickEvalContext ctx)
	{
		if (BehaviorGraphConfig.TryArrayElement(ctx.BehaviorConfiguration, configId ?? "", index, out var fromConfig))
			return fromConfig;
		return 0f;
	}

	static bool ResolveConfigBool(string? configId, TickEvalContext ctx)
	{
		if (string.IsNullOrWhiteSpace(configId) || ctx.BehaviorConfiguration is null)
			return false;
		return ctx.BehaviorConfiguration.TryGetValue(configId, out var entry) && entry.BoolValue;
	}

	/// <summary>Matches legacy C# <c>uint ageHours / int divisor</c> (e.g. TickDefault petiole age bud).</summary>
	static float IntegerDivideUint(float a, float b)
	{
		if (b == 0f)
			return 0f;
		return (uint)a / (uint)b;
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
