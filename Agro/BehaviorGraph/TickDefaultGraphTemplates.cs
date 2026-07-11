namespace Agro.BehaviorGraph;

	sealed class SubgraphBuilder
	{
		readonly string _prefix;
		readonly List<global::GraphNode> _nodes = [];
		readonly List<global::GraphConnection> _connections = [];
		int _connSeq;
		SubgraphBuilder(string prefix) => _prefix = prefix;

		public static SubgraphBuilder Create(string prefix) => new(prefix);

		public SubgraphBuilder GateAlwaysTrue()
		{
			var id = AddBool("always", true, 0, 0);
			return FinishWithActive(id, "bool");
		}

		public SubgraphBuilder FinishWithActive(string sourceNodeId, string sourceOutput)
		{
			var active = Add(Pref("active"), "Active", 1400, 0);
			Connect(sourceNodeId, sourceOutput, active, "isActive");
			return this;
		}

		public global::ExportedGraph Build() => new() { Nodes = _nodes, Connections = _connections };

		string Pref(string id) => $"{_prefix}-{id}";

		public string Add(string id, string label, float x, float y, GraphNodePayload? payload = null)
		{
			var fullId = Pref(id);
			_nodes.Add(new global::GraphNode
			{
				Id = fullId,
				Label = label,
				Data = payload?.ToJsonElement() ?? BehaviorGraphJson.EmptyObject,
				Position = new global::NodePosition { X = x, Y = y },
			});
			return fullId;
		}

		public string Add(string id, string label, float x, float y, string comment) =>
			Add(id, label, x, y, GraphNodePayload.FromComment(comment));

		public string AddBool(string id, bool value, float x, float y, string? comment = null) =>
			Add(id, "Boolean Input", x, y, GraphNodePayload.FromBool(value, comment));

		/// <summary>Boolean stub for guards that cannot be wired yet; comment is shown in the editor.</summary>
		public string AddBoolStub(string id, bool value, string comment) =>
			AddBool(id, value, 0, 0, comment);

		public string AddNum(string id, float value, float x, float y, string? comment = null) =>
			Add(id, "Number Input", x, y, GraphNodePayload.FromNumber(value, comment));

		public string AddConfig(string id, string configId, bool isBoolean, float x, float y, string? comment = null) =>
			Add(id, "Configuration Value Input", x, y, GraphNodePayload.FromConfig(configId, isBoolean, comment));

		public string AddConfigArray(string id, string configId, float x, float y, string? comment = null) =>
			Add(id, "Configuration Array Input", x, y, GraphNodePayload.FromConfigArray(configId, comment));

		/// <summary>dominanceLevel â†’ Configuration Array Input (DominanceFactors) â†’ out.</summary>
		public string WireDominanceLookup(string stateId, string configArrayId, float x, float y, string suffix = "dom")
		{
			var arr = AddConfigArray($"dominance-arr-{suffix}", configArrayId, x, y,
				"DominanceFactors[dominanceLevel]");
			Connect(stateId, "dominanceLevel", arr, "index");
			return arr;
		}

		public void Connect(string source, string sourceOutput, string target, string targetInput)
		{
			_connections.Add(new global::GraphConnection
			{
				Id = $"{_prefix}-c-{_connSeq++}",
				Source = source,
				SourceOutput = sourceOutput,
				Target = target,
				TargetInput = targetInput,
			});
		}

		/// <summary>lifeSupportPerHour = length*radius*(leaf ? leafThickness : radius*wood).</summary>
		public string WireLifeSupportPerHour(string organId, string stateId)
		{
			var leafThick = AddConfig("leaf-thick", DefaultSpeciesGraphBuilder.ConfigIds.LeafThickness, false, 240, 200,
				"AboveGroundAgent.LeafThickness");

			var lr = Add("lr", "Multiply", 480, 200);
			Connect(stateId, "length", lr, "a");
			Connect(stateId, "radius", lr, "b");

			var leafHour = Add("leaf-hour", "Multiply", 720, 180);
			Connect(lr, "out", leafHour, "a");
			Connect(leafThick, "num", leafHour, "b");

			var rw = Add("rw", "Multiply", 720, 220);
			Connect(stateId, "radius", rw, "a");
			Connect(stateId, "wood", rw, "b");

			var nonLeafHour = Add("nonleaf-hour", "Multiply", 960, 200);
			Connect(lr, "out", nonLeafHour, "a");
			Connect(rw, "out", nonLeafHour, "b");

			var perHour = Add("per-hour", "If / Else", 1200, 200);
			Connect(organId, "leaf", perHour, "condition");
			Connect(leafHour, "out", perHour, "trueValue");
			Connect(nonLeafHour, "out", perHour, "falseValue");

			return perHour;
		}

		/// <summary>Energy &gt; lifeSupportPerHour * EnoughEnergyFactor.</summary>
		public string WireEnoughEnergy(string organId, string stateId)
		{
			var perHour = WireLifeSupportPerHour(organId, stateId);
			var factor = AddConfig("enough-factor", DefaultSpeciesGraphBuilder.ConfigIds.EnoughEnergyFactor, false, 240, 280,
				"AboveGroundAgent.EnoughEnergy factor (320)");
			var threshold = Add("threshold", "Multiply", 1440, 280);
			Connect(perHour, "out", threshold, "a");
			Connect(factor, "num", threshold, "b");

			var enough = Add("enough", "Greater Than", 1680, 280);
			Connect(stateId, "energy", enough, "a");
			Connect(threshold, "out", enough, "b");
			return enough;
		}

		/// <summary>organ + Not(bud) + enoughEnergy.</summary>
		public string WireGrowthActiveGate(string organId, string stateId, string organSocket)
		{
			var notBud = Add("not-bud", "Not", 400, 40);
			Connect(organId, "bud", notBud, "a");

			var enough = WireEnoughEnergy(organId, stateId);
			var andOrgan = Add("and-organ", "And", 640, 0);
			Connect(organId, organSocket, andOrgan, "a");
			Connect(enough, "out", andOrgan, "b");

			var andGate = Add("and-gate", "And", 880, 20);
			Connect(notBud, "out", andGate, "a");
			Connect(andOrgan, "out", andGate, "b");
			return andGate;
		}

		/// <summary>Bergania.Tick growth gate: enough energy, not bud, not rhizome.</summary>
		public string WireBerganiaGrowthActiveGate(string organId, string stateId, string organSocket)
		{
			var gate = WireGrowthActiveGate(organId, stateId, organSocket);
			var notRizome = Add("not-riz", "Not", 1120, 40);
			Connect(stateId, "isRizome", notRizome, "a");
			var andGate = Add("and-berg-growth", "And", 1360, 20);
			Connect(gate, "out", andGate, "a");
			Connect(notRizome, "out", andGate, "b");
			return andGate;
		}

		/// <summary>Legacy Bergania stem radius growth: no production ratio; Min(water, energyReserve).</summary>
		public string WireBerganiaStemGrowthDelta(
			string stateId,
			string formId,
			string simId,
			string growthFactorConfigId,
			string maxRadiusConfigId)
		{
			var cfgRad = AddConfig("stem-rad", DefaultSpeciesGraphBuilder.ConfigIds.StemGrowthRadius, false, 240, 480);
			var dominance = WireDominanceLookup(stateId, DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactors, 240, 520, "stem");

			var energyReserve = WireEnergyReserve(stateId);
			var waterMinReserve = WireMinFloat(formId, "waterBalance", energyReserve, "out", "stem-water");

			var m1 = Add("m1", "Multiply", 720, 480);
			Connect(cfgRad, "num", m1, "a");
			Connect(dominance, "out", m1, "b");

			var m2 = Add("m2", "Multiply", 960, 480);
			Connect(m1, "out", m2, "a");
			Connect(energyReserve, "out", m2, "b");

			var m3 = Add("m3", "Multiply", 1200, 480);
			Connect(m2, "out", m3, "a");
			Connect(waterMinReserve, "out", m3, "b");

			var deltaRad = Add("delta-r", "Multiply", 1440, 480);
			Connect(m3, "out", deltaRad, "a");
			Connect(simId, "hoursPerTick", deltaRad, "b");
			var capped = WireCapRadiusDeltaToParent(stateId, formId, deltaRad, "stem");
			capped = WireMultiplyByConfig(capped, growthFactorConfigId, "stem-gf");
			capped = WireGateDeltaWhenRadiusBelowMax(stateId, maxRadiusConfigId, capped, "stem-max-r");
			return capped;
		}

		/// <summary>Math.Min(1f, value).</summary>
		public string WireMinOne(string valueNodeId, string valueOutput, string id)
		{
			var one = AddNum($"{id}-one", 1f, 480, 320);
			var gt = Add($"{id}-gt", "Greater Than", 720, 320);
			Connect(valueNodeId, valueOutput, gt, "a");
			Connect(one, "num", gt, "b");

			var min = Add($"{id}-min", "If / Else", 960, 320);
			Connect(gt, "out", min, "condition");
			Connect(one, "num", min, "trueValue");
			Connect(valueNodeId, valueOutput, min, "falseValue");
			return min;
		}

		/// <summary>Math.Min(a, b).</summary>
		public string WireMinFloat(string aNodeId, string aOutput, string bNodeId, string bOutput, string id)
		{
			var lt = Add($"{id}-lt", "Less Than", 720, 360);
			Connect(aNodeId, aOutput, lt, "a");
			Connect(bNodeId, bOutput, lt, "b");

			var min = Add($"{id}-min", "If / Else", 960, 360);
			Connect(lt, "out", min, "condition");
			Connect(aNodeId, aOutput, min, "trueValue");
			Connect(bNodeId, bOutput, min, "falseValue");
			return min;
		}

		public string WireProdRatio(string stateId, string formId)
		{
			var prodRatio = Add("prod-ratio", "Divide", 720, 400);
			Connect(stateId, "previousDayProductionInv", prodRatio, "a");
			Connect(formId, "dailyProductionMax", prodRatio, "b");
			return prodRatio;
		}

		/// <summary>Math.Clamp(energy / capacity, 0, 1).</summary>
		public string WireEnergyReserve(string stateId)
		{
			var ratio = Add("energy-ratio", "Divide", 480, 400);
			Connect(stateId, "energy", ratio, "a");
			Connect(stateId, "energyStorageCapacity", ratio, "b");

			var zero = AddNum("zero", 0f, 480, 360);
			var ltZero = Add("ratio-lt-zero", "Less Than", 600, 400);
			Connect(ratio, "out", ltZero, "a");
			Connect(zero, "num", ltZero, "b");

			var floored = Add("ratio-floored", "If / Else", 720, 380);
			Connect(ltZero, "out", floored, "condition");
			Connect(zero, "num", floored, "trueValue");
			Connect(ratio, "out", floored, "falseValue");

			var one = AddNum("one", 1f, 720, 440);
			var reserve = Add("energy-reserve", "Clamp Max", 920, 400);
			Connect(floored, "out", reserve, "value");
			Connect(one, "num", reserve, "max");
			return reserve;
		}

		public (string deltaLen, string deltaRad, string sizeLimitL, string sizeLimitR) WireLeafPetioleGrowthDeltas(
			string stateId, string formId,
			string configLengthId, string configRadiusId,
			bool capRadiusToParent)
		{
			var configLen = AddConfig("cfg-len", configLengthId, false, 240, 400,
				"Species size limit (length)");
			var configRad = AddConfig("cfg-rad", configRadiusId, false, 240, 440,
				"Species size limit (radius)");

			var sizeLimitL = Add("size-lim-l", "Add", 480, 400);
			Connect(configLen, "num", sizeLimitL, "a");
			Connect(stateId, "lengthVar", sizeLimitL, "b");

			var sizeLimitR = Add("size-lim-r", "Add", 480, 440);
			Connect(configRad, "num", sizeLimitR, "a");
			Connect(stateId, "radiusVar", sizeLimitR, "b");

			var waterClamped = WireMinOne(formId, "waterBalance", "water");
			var prodRatio = WireProdRatio(stateId, formId);

			var m1L = Add("m1-l", "Multiply", 960, 400);
			Connect(waterClamped, "out", m1L, "a");
			Connect(sizeLimitL, "out", m1L, "b");

			var m2L = Add("m2-l", "Multiply", 1200, 400);
			Connect(m1L, "out", m2L, "a");
			Connect(stateId, "growthTimeVar", m2L, "b");

			var deltaLen = Add("delta-l", "Multiply", 1440, 400);
			Connect(m2L, "out", deltaLen, "a");
			Connect(prodRatio, "out", deltaLen, "b");

			var m1R = Add("m1-r", "Multiply", 960, 440);
			Connect(waterClamped, "out", m1R, "a");
			Connect(sizeLimitR, "out", m1R, "b");

			var m2R = Add("m2-r", "Multiply", 1200, 440);
			Connect(m1R, "out", m2R, "a");
			Connect(stateId, "growthTimeVar", m2R, "b");

			var deltaRad = Add("delta-r", "Multiply", 1440, 440);
			Connect(m2R, "out", deltaRad, "a");
			Connect(prodRatio, "out", deltaRad, "b");

			if (capRadiusToParent)
			{
				var parentCap = Add("parent-cap", "Subtract", 1680, 440);
				Connect(formId, "parentBaseRadius", parentCap, "a");
				Connect(stateId, "radius", parentCap, "b");

				var capped = Add("delta-r-cap", "Clamp Max", 1920, 440);
				Connect(deltaRad, "out", capped, "value");
				Connect(parentCap, "out", capped, "max");
				deltaRad = capped;
			}

			deltaLen = WireClampDeltaToLimit(stateId, "length", sizeLimitL, deltaLen, "len");
			deltaRad = WireClampDeltaToLimit(stateId, "radius", sizeLimitR, deltaRad, "rad");

			return (deltaLen, deltaRad, sizeLimitL, sizeLimitR);
		}

		public string WireUnderSizeLimit(string stateId, string sizeLimitL, string sizeLimitR)
		{
			var lenLt = Add("len-under", "Less Than", 1680, 360);
			Connect(stateId, "length", lenLt, "a");
			Connect(sizeLimitL, "out", lenLt, "b");

			var radLt = Add("rad-under", "Less Than", 1680, 400);
			Connect(stateId, "radius", radLt, "a");
			Connect(sizeLimitR, "out", radLt, "b");

			var under = Add("under-limit", "And", 1920, 380);
			Connect(lenLt, "out", under, "a");
			Connect(radLt, "out", under, "b");
			return under;
		}

		public string WireClampDeltaToLimit(string stateId, string axis, string sizeLimitId, string deltaId, string suffix)
		{
			var remaining = Add($"rem-{suffix}", "Subtract", 1920, 420);
			Connect(sizeLimitId, "out", remaining, "a");
			Connect(stateId, axis, remaining, "b");
			return WireMinFloat(deltaId, "out", remaining, "out", $"clamp-{suffix}");
		}

		public (string deltaLen, string deltaRad) WireMeristemGrowthDeltas(
			string stateId,
			string formId,
			string simId,
			string? growthFactorConfigId = null,
			string? maxRadiusConfigId = null,
			bool gateLengthByLengthVar = false)
		{
			var cfgLen = AddConfig("mer-len", DefaultSpeciesGraphBuilder.ConfigIds.MeristemGrowthLength, false, 240, 480);
			var cfgRad = AddConfig("mer-rad", DefaultSpeciesGraphBuilder.ConfigIds.MeristemGrowthRadius, false, 240, 520);
			var dominance = WireDominanceLookup(stateId, DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactors, 240, 560, "mer");

			var energyReserve = WireEnergyReserve(stateId);
			var waterReserve = WireMinOne(formId, "waterBalance", "water-res");
			var prodRatio = WireProdRatio(stateId, formId);

			string WireAxisDelta(string cfgNodeId, float y, string suffix)
			{
				var m1 = Add($"m1-{suffix}", "Multiply", 720, y);
				Connect(cfgNodeId, "num", m1, "a");
				Connect(dominance, "out", m1, "b");

				var m2 = Add($"m2-{suffix}", "Multiply", 960, y);
				Connect(m1, "out", m2, "a");
				Connect(energyReserve, "out", m2, "b");

				var m3 = Add($"m3-{suffix}", "Multiply", 1200, y);
				Connect(m2, "out", m3, "a");
				Connect(waterReserve, "out", m3, "b");

				var m4 = Add($"m4-{suffix}", "Multiply", 1440, y);
				Connect(m3, "out", m4, "a");
				Connect(simId, "hoursPerTick", m4, "b");

				var delta = Add($"delta-{suffix}", "Multiply", 1680, y);
				Connect(m4, "out", delta, "a");
				Connect(prodRatio, "out", delta, "b");
				return delta;
			}

			var deltaLen = WireAxisDelta(cfgLen, 480, "len");
			var deltaRadRaw = WireAxisDelta(cfgRad, 520, "rad");
			var deltaRad = WireCapRadiusDeltaToParent(stateId, formId, deltaRadRaw, "mer");

			if (growthFactorConfigId is not null)
			{
				deltaLen = WireMultiplyByConfig(deltaLen, growthFactorConfigId, "mer-gf-len");
				deltaRad = WireMultiplyByConfig(deltaRad, growthFactorConfigId, "mer-gf-rad");
			}

			if (maxRadiusConfigId is not null)
				deltaRad = WireGateDeltaWhenRadiusBelowMax(stateId, maxRadiusConfigId, deltaRad, "mer-max-r");

			if (gateLengthByLengthVar)
				deltaLen = WireGateLengthDeltaWhenLengthLeLengthVar(stateId, deltaLen, "mer-len-gate");

			return (deltaLen, deltaRad);
		}

		public string WireStemGrowthDelta(
			string stateId,
			string formId,
			string simId,
			string? growthFactorConfigId = null,
			string? maxRadiusConfigId = null)
		{
			var cfgRad = AddConfig("stem-rad", DefaultSpeciesGraphBuilder.ConfigIds.StemGrowthRadius, false, 240, 480);
			var dominance = WireDominanceLookup(stateId, DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactors, 240, 520, "stem");

			var energyReserve = WireEnergyReserve(stateId);
			var waterMinReserve = WireMinFloat(formId, "waterBalance", energyReserve, "out", "stem-water");

			var m1 = Add("m1", "Multiply", 720, 480);
			Connect(cfgRad, "num", m1, "a");
			Connect(dominance, "out", m1, "b");

			var m2 = Add("m2", "Multiply", 960, 480);
			Connect(m1, "out", m2, "a");
			Connect(energyReserve, "out", m2, "b");

			var m3 = Add("m3", "Multiply", 1200, 480);
			Connect(m2, "out", m3, "a");
			Connect(waterMinReserve, "out", m3, "b");

			var deltaRad = Add("delta-r", "Multiply", 1440, 480);
			Connect(m3, "out", deltaRad, "a");
			Connect(simId, "hoursPerTick", deltaRad, "b");
			var capped = WireCapRadiusDeltaToParent(stateId, formId, deltaRad, "stem");

			if (growthFactorConfigId is not null)
				capped = WireMultiplyByConfig(capped, growthFactorConfigId, "stem-gf");

			if (maxRadiusConfigId is not null)
				capped = WireGateDeltaWhenRadiusBelowMax(stateId, maxRadiusConfigId, capped, "stem-max-r");

			return capped;
		}

		public string WireMultiplyByConfig(string deltaNodeId, string configId, string suffix)
		{
			var cfg = AddConfig($"cfg-{suffix}", configId, false, 1920, 560);
			var mul = Add($"mul-{suffix}", "Multiply", 2160, 560);
			Connect(deltaNodeId, "out", mul, "a");
			Connect(cfg, "num", mul, "b");
			return mul;
		}

		public string WireGateDeltaWhenRadiusBelowMax(string stateId, string maxRadiusConfigId, string deltaNodeId, string suffix)
		{
			var maxR = AddConfig($"max-r-{suffix}", maxRadiusConfigId, false, 1920, 600);
			var radiusLt = Add($"rad-lt-{suffix}", "Less Than", 2160, 600);
			Connect(stateId, "radius", radiusLt, "a");
			Connect(maxR, "num", radiusLt, "b");
			var c0 = AddNum($"c0-{suffix}", 0f, 2160, 640);
			var gated = Add($"gate-{suffix}", "If / Else", 2400, 600);
			Connect(radiusLt, "out", gated, "condition");
			Connect(deltaNodeId, "out", gated, "trueValue");
			Connect(c0, "num", gated, "falseValue");
			return gated;
		}

		public string WireGateLengthDeltaWhenLengthLeLengthVar(string stateId, string deltaNodeId, string suffix)
		{
			var lenGt = Add($"len-gt-{suffix}", "Greater Than", 1920, 480);
			Connect(stateId, "length", lenGt, "a");
			Connect(stateId, "lengthVar", lenGt, "b");
			var c0 = AddNum($"c0-len-{suffix}", 0f, 1920, 520);
			var gated = Add($"gate-len-{suffix}", "If / Else", 2160, 480);
			Connect(lenGt, "out", gated, "condition");
			Connect(c0, "num", gated, "trueValue");
			Connect(deltaNodeId, "out", gated, "falseValue");
			return gated;
		}

		/// <summary>TickDefault: growth.Y = min(growth.Y, parentRadius - radius).</summary>
		public string WireCapRadiusDeltaToParent(string stateId, string formId, string deltaRadId, string suffix)
		{
			var parentCap = Add($"parent-cap-{suffix}", "Subtract", 1680, 520);
			Connect(formId, "parentBaseRadius", parentCap, "a");
			Connect(stateId, "radius", parentCap, "b");

			var capped = Add($"delta-r-cap-{suffix}", "Clamp Max", 1920, 520);
			Connect(deltaRadId, "out", capped, "value");
			Connect(parentCap, "out", capped, "max");
			return capped;
		}

		/// <summary>NextFloatAccum(p, hoursPerTick) â€” returns RNG node id (out socket).</summary>
		public string WireRandomAccumChance(string pNodeId, string pOutput)
		{
			var rng = Add("rng-accum", "Random Accum Chance Input", 1680, 120);
			Connect(pNodeId, pOutput, rng, "p");
			return rng;
		}

		/// <summary>Stem dominance death probability p = base / (q*q).</summary>
		public string WireStemDeathProbability(string stateId, string formId)
		{
			var hCoeff = AddConfig("h-coeff", DefaultSpeciesGraphBuilder.ConfigIds.StemDeathHeightCoeff, false, 280, 200);
			var eCoeff = AddConfig("e-coeff", DefaultSpeciesGraphBuilder.ConfigIds.StemDeathEfficiencyCoeff, false, 280, 240);
			var rCoeff = AddConfig("r-coeff", DefaultSpeciesGraphBuilder.ConfigIds.StemDeathRadiusCoeff, false, 280, 280);
			var baseP = AddConfig("base-p", DefaultSpeciesGraphBuilder.ConfigIds.StemDeathProbabilityBase, false, 280, 320);

			var h = Add("h", "Multiply", 520, 200);
			Connect(hCoeff, "num", h, "a");
			Connect(formId, "agentHeightRatio", h, "b");

			var hSq = Add("h-sq", "Multiply", 760, 200);
			Connect(h, "out", hSq, "a");
			Connect(h, "out", hSq, "b");

			var eNum = Add("e-num", "Multiply", 520, 240);
			Connect(eCoeff, "num", eNum, "a");
			Connect(stateId, "previousDayEnvResources", eNum, "b");
			var eDiv = Add("e-div", "Divide", 760, 240);
			Connect(eNum, "out", eDiv, "a");
			Connect(formId, "dailyEfficiencyMax", eDiv, "b");

			var eSq = Add("e-sq", "Multiply", 760, 280);
			Connect(eDiv, "out", eSq, "a");
			Connect(eDiv, "out", eSq, "b");

			var one = AddNum("one", 1f, 520, 320);
			var rTerm = Add("r-term", "Multiply", 760, 320);
			Connect(rCoeff, "num", rTerm, "a");
			Connect(stateId, "radius", rTerm, "b");

			var q1 = Add("q1", "Add", 1000, 280);
			Connect(one, "num", q1, "a");
			Connect(hSq, "out", q1, "b");

			var q2 = Add("q2", "Add", 1240, 280);
			Connect(q1, "out", q2, "a");
			Connect(rTerm, "out", q2, "b");

			var q = Add("q", "Add", 1480, 280);
			Connect(q2, "out", q, "a");
			Connect(eSq, "out", q, "b");

			var qWood = Add("q-wood", "Add", 1720, 280);
			Connect(q, "out", qWood, "a");
			Connect(stateId, "wood", qWood, "b");

			var qSq = Add("q-sq", "Multiply", 1960, 280);
			Connect(qWood, "out", qSq, "a");
			Connect(qWood, "out", qSq, "b");

			var p = Add("death-p", "Divide", 2200, 280);
			Connect(baseP, "num", p, "a");
			Connect(qSq, "out", p, "b");
			return p;
		}

		/// <summary>Unproductive petiole death: p = (1 - 2*production)^2.</summary>
		public string WireUnproductiveDeathProbability(string prodRatioNodeId, string prodRatioOutput)
		{
			var two = AddNum("two", 2f, 1480, 160);
			var doubled = Add("doubled", "Multiply", 1720, 160);
			Connect(two, "num", doubled, "a");
			Connect(prodRatioNodeId, prodRatioOutput, doubled, "b");

			var one = AddNum("one", 1f, 1720, 200);
			var inverted = Add("inverted", "Subtract", 1960, 160);
			Connect(one, "num", inverted, "a");
			Connect(doubled, "out", inverted, "b");

			var p = Add("death-p", "Multiply", 2200, 160);
			Connect(inverted, "out", p, "a");
			Connect(inverted, "out", p, "b");
			return p;
		}

		/// <summary>Composable auxin-twig activation chain (phase 2).</summary>
		public void WireTwigEffectChain(string organId, string formId)
		{
			var notParentMer = Add("not-parent-mer", "Not", 1480, 360);
			Connect(formId, "parentMeristem", notParentMer, "a");

			var petiolePath = Add("petiole-path", "And", 1720, 380);
			Connect(organId, "petiole", petiolePath, "a");
			Connect(notParentMer, "out", petiolePath, "b");

			var death = Add("death-children", "Death Children", 1960, 360);
			Connect(petiolePath, "out", death, "trigger");

			var becomeTrig = Add("become-trig", "Or", 1960, 400);
			Connect(organId, "bud", becomeTrig, "a");
			Connect(death, "seq", becomeTrig, "b");

			var become = Add("become-mer", "Become Meristem", 2200, 400);
			Connect(becomeTrig, "out", become, "trigger");

			var latAngle = AddConfig("lat-angle", DefaultSpeciesGraphBuilder.ConfigIds.TwigLateralAngle, false, 2200, 440);
			var setLat = Add("set-lat", "Set Lateral Angle", 2440, 400);
			Connect(become, "seq", setLat, "trigger");
			Connect(latAngle, "num", setLat, "value");

			var one = AddNum("one", 1f, 2440, 480);
			var deltaDom = Add("delta-dom", "Delta Dominance", 2680, 400);
			Connect(setLat, "seq", deltaDom, "trigger");
			Connect(one, "num", deltaDom, "count");

			var turn = Add("turn-up", "Turn Upwards", 2920, 400);
			Connect(deltaDom, "seq", turn, "trigger");

			var nodeDist = AddConfig("node-dist", DefaultSpeciesGraphBuilder.ConfigIds.NodeDistance, false, 2920, 440);
			var nodeDistVar = AddConfig("node-dist-var", DefaultSpeciesGraphBuilder.ConfigIds.NodeDistanceVar, false, 2920, 480);
			var rngVar = Add("rng-var", "Random Float Var Input", 3160, 480);
			Connect(nodeDistVar, "num", rngVar, "variance");

			var lengthVar = Add("length-var", "Add", 3400, 440);
			Connect(nodeDist, "num", lengthVar, "a");
			Connect(rngVar, "out", lengthVar, "b");

			var setLen = Add("set-len", "Set Length Var", 3160, 400);
			Connect(turn, "seq", setLen, "trigger");
			Connect(lengthVar, "out", setLen, "value");

			var createLeaves = Add("create-leaves", "Create Leaves", 3400, 400);
			Connect(setLen, "seq", createLeaves, "trigger");
		}
	}