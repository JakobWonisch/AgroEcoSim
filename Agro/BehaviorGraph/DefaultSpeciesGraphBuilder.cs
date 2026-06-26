using System.Text.Json;

namespace Agro.BehaviorGraph;

/// <summary>
/// Bootstrap behavior graphs for the Default species — one subgraph per <see cref="AboveGroundAgent.TickDefault"/> topic.
/// TickDefault topics as separate graphs (Active gate + phase-2 effects where implemented).
/// </summary>
public static class DefaultSpeciesGraphBuilder
{
	public static IReadOnlyList<(string Name, global::ExportedGraph Graph)> BuildDefaultSpeciesSubgraphs() =>
	[
		("Life support", BuildLifeSupportSubgraph()),
		("Photosynthesis", BuildPhotosynthesisSubgraph()),
		// ("Petiole age bud", BuildPetioleAgeBudSubgraph()),
		// ("Stem dominance death", BuildStemDominanceDeathSubgraph()),
		// ("Meristem tick marker", BuildMeristemTickMarkerSubgraph()),
		// ("Auxin twig", BuildAuxinTwigSubgraph()),
		// ("Growth leaf", BuildGrowthLeafSubgraph()),
		// ("Growth petiole", BuildGrowthPetioleSubgraph()),
		// ("Growth meristem", BuildGrowthMeristemSubgraph()),
		// ("Growth stem", BuildGrowthStemSubgraph()),
		// ("Wood lignify", BuildWoodLignifySubgraph()),
		// ("Meristem chain", BuildMeristemChainSubgraph()),
		// ("Petiole cover bud", BuildPetioleCoverBudSubgraph()),
		// ("Petiole unproductive death", BuildPetioleUnproductiveDeathSubgraph()),
		// ("Energy depletion", BuildEnergyDepletionSubgraph()),
		// ("Auxins update", BuildAuxinsUpdateSubgraph()),
	];

	/// <summary>
	/// TickDefault lines 417–420: Energy -= LifeSupportPerTick.
	/// lifeSupportPerHour = Length*Radius*(leaf ? LeafThickness : Radius*WoodFactor); * HoursPerTick.
	/// </summary>
	public static global::ExportedGraph BuildLifeSupportSubgraph()
	{
		var b = SubgraphBuilder.Create("ls");
		var always = b.AddBool("always", true, 0, 0);

		var organ = b.Add("organ", "Agent Type Input", 0, 80);
		var state = b.Add("state", "Agent State Input", 0, 140);

		// AboveGroundAgent.LeafThickness
		var cLeafThick = b.AddNum("leaf-thick", 0.0001f, 240, 80);
		// MISSING: world.HoursPerTick — no simulation input node; AgroWorld default is 1
		var cHoursPerTick = b.AddNum("hours-per-tick", 1f, 240, 120,
			"Placeholder: AgroWorld.HoursPerTick (no simulation input node yet)");
		var c0 = b.AddNum("c0", 0f, 240, 160);

		var lr = b.Add("lr", "Multiply", 480, 100);
		b.Connect(state, "length", lr, "a");
		b.Connect(state, "radius", lr, "b");

		var leafHour = b.Add("leaf-hour", "Multiply", 720, 80);
		b.Connect(lr, "out", leafHour, "a");
		b.Connect(cLeafThick, "num", leafHour, "b");

		var rw = b.Add("rw", "Multiply", 720, 140);
		b.Connect(state, "radius", rw, "a");
		b.Connect(state, "wood", rw, "b");

		var nonLeafHour = b.Add("nonleaf-hour", "Multiply", 960, 120);
		b.Connect(lr, "out", nonLeafHour, "a");
		b.Connect(rw, "out", nonLeafHour, "b");

		var perHour = b.Add("per-hour", "If / Else", 1200, 100);
		b.Connect(organ, "leaf", perHour, "condition");
		b.Connect(leafHour, "out", perHour, "trueValue");
		b.Connect(nonLeafHour, "out", perHour, "falseValue");

		var perTick = b.Add("per-tick", "Multiply", 1440, 100);
		b.Connect(perHour, "out", perTick, "a");
		b.Connect(cHoursPerTick, "num", perTick, "b");

		var negTick = b.Add("neg-tick", "Subtract", 1680, 100);
		b.Connect(c0, "num", negTick, "a");
		b.Connect(perTick, "out", negTick, "b");

		var dEnergy = b.Add("d-energy", "Delta Energy", 1920, 100);
		b.Connect(negTick, "out", dEnergy, "amount");

		return b.FinishWithActive(always, "bool").Build();
	}

	/// <summary>
	/// TickDefault lines 427–456: leaf photosynthesis when Water_g &gt; 0 and irradiance &gt; 0.01.
	/// MISSING effect nodes: CurrentDayEnvResources += approxLight*surface; CurrentDayEnvResourcesInv += approxLight.
	/// </summary>
	public static global::ExportedGraph BuildPhotosynthesisSubgraph()
	{
		var b = SubgraphBuilder.Create("photo");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var ir = b.Add("ir", "Irradiance Input", 0, 120);

		var c0 = b.AddNum("c0", 0f, 280, 0);
		var c001 = b.AddNum("c001", 0.01f, 280, 40);
		// AboveGroundAgent.mPhotoEfficiency
		var cPhotoEff = b.AddNum("photo-eff", 0.005f, 280, 80);
		// surface = Length * Radius * 2f (leaf branch in TickDefault)
		var cSurface2 = b.AddNum("surface-2", 2f, 280, 120);

		// Active: Organ == Leaf && Water_g > 0 && irradiance > 0.01
		var hasWater = b.Add("has-water", "Greater Than (or Equal)", 520, 0);
		b.Connect(state, "water", hasWater, "a");
		b.Connect(c0, "num", hasWater, "b");

		var bright = b.Add("bright", "Greater Than (or Equal)", 520, 60);
		b.Connect(ir, "irradiance", bright, "a");
		b.Connect(c001, "num", bright, "b");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "leaf", and1, "a");
		b.Connect(hasWater, "out", and1, "b");

		var activeCond = b.Add("active-cond", "And", 1000, 40);
		b.Connect(and1, "out", activeCond, "a");
		b.Connect(bright, "out", activeCond, "b");

		// Phase 2 — photosynthesizedEnergy = min(surface * irradiance * mPhotoEfficiency, Water_g)
		var lr = b.Add("lr", "Multiply", 480, 200);
		b.Connect(state, "length", lr, "a");
		b.Connect(state, "radius", lr, "b");

		var surface = b.Add("surface", "Multiply", 720, 200);
		b.Connect(lr, "out", surface, "a");
		b.Connect(cSurface2, "num", surface, "b");

		var byLight = b.Add("by-light", "Multiply", 960, 200);
		b.Connect(surface, "out", byLight, "a");
		b.Connect(ir, "irradiance", byLight, "b");

		var lightEff = b.Add("light-eff", "Multiply", 1200, 200);
		b.Connect(byLight, "out", lightEff, "a");
		b.Connect(cPhotoEff, "num", lightEff, "b");

		var lightLeWater = b.Add("light-le-water", "Less Than (or Equal)", 1440, 240);
		b.Connect(lightEff, "out", lightLeWater, "a");
		b.Connect(state, "water", lightLeWater, "b");

		var photoAmt = b.Add("photo-amt", "If / Else", 1680, 200);
		b.Connect(lightLeWater, "out", photoAmt, "condition");
		b.Connect(lightEff, "out", photoAmt, "trueValue");
		b.Connect(state, "water", photoAmt, "falseValue");

		var dEnergy = b.Add("d-energy", "Delta Energy", 1920, 180);
		b.Connect(photoAmt, "out", dEnergy, "amount");

		var negPhoto = b.Add("neg-photo", "Subtract", 1920, 240);
		b.Connect(c0, "num", negPhoto, "a");
		b.Connect(photoAmt, "out", negPhoto, "b");

		var dWater = b.Add("d-water", "Delta Water", 2160, 240);
		b.Connect(negPhoto, "out", dWater, "amount");

		var prodInv = b.Add("prod-inv", "Divide", 1920, 300);
		b.Connect(photoAmt, "out", prodInv, "a");
		b.Connect(surface, "out", prodInv, "b");

		var accProd = b.Add("acc-prod", "Accumulate Production", 2160, 300,
			"MISSING: CurrentDayEnvResources += approxLight*surface (no effect node)");
		b.Connect(prodInv, "out", accProd, "amount");

		return b.FinishWithActive(activeCond, "out").Build();
	}

	/// <summary>TickDefault lines 461–467 — partial gate; RNG and parent organ type missing.</summary>
	public static global::ExportedGraph BuildPetioleAgeBudSubgraph()
	{
		var b = SubgraphBuilder.Create("pab");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var c36 = b.AddNum("c36", 36f, 280, 0);

		var ageOk = b.Add("age-ok", "Greater Than (or Equal)", 520, 60);
		b.Connect(state, "ageHours", ageOk, "a");
		b.Connect(c36, "num", ageOk, "b");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "petiole", and1, "a");
		b.Connect(ageOk, "out", and1, "b");

		// MISSING: formation.GetOrgan(Parent) != Meristem
		var missingParent = b.AddBoolStub("missing-parent", true,
			"MISSING: parent organ type != Meristem (no formation.GetOrgan input)");
		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(missingParent, "bool", and2, "b");

		// MISSING: plant.RNG.NextFloatAccum(p*p, HoursPerTick)
		var missingRng = b.AddBoolStub("missing-rng", true,
			"MISSING: NextFloatAccum (Random Chance uses different semantics)");
		var and3 = b.Add("and3", "And", 1240, 60);
		b.Connect(and2, "out", and3, "a");
		b.Connect(missingRng, "bool", and3, "b");

		return b.FinishWithActive(and3, "out").Build();
	}

	/// <summary>TickDefault lines 473–481 — partial gate; dominance and RNG missing.</summary>
	public static global::ExportedGraph BuildStemDominanceDeathSubgraph()
	{
		var b = SubgraphBuilder.Create("sdd");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);

		// MISSING: DominanceLevel > 1 && GetDominance(Parent) < DominanceLevel
		var missingDom = b.AddBoolStub("missing-dom", true,
			"MISSING: DominanceLevel and formation.GetDominance(Parent)");
		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "stem", and1, "a");
		b.Connect(missingDom, "bool", and1, "b");

		var missingRng = b.AddBoolStub("missing-rng", true,
			"MISSING: NextFloatAccum for stem death probability");
		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(missingRng, "bool", and2, "b");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault line 490 — switch case Meristem (wasMeristem flag).</summary>
	public static global::ExportedGraph BuildMeristemTickMarkerSubgraph()
	{
		var b = SubgraphBuilder.Create("mtm");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		return b.FinishWithActive(organ, "meristem").Build();
	}

	/// <summary>TickDefault lines 494–519 — partial; auxin graph and energy gate missing.</summary>
	public static global::ExportedGraph BuildAuxinTwigSubgraph()
	{
		var b = SubgraphBuilder.Create("atw");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);

		var petioleOrBud = b.Add("pet-or-bud", "Or", 400, 20);
		b.Connect(organ, "petiole", petioleOrBud, "a");
		b.Connect(organ, "bud", petioleOrBud, "b");

		// MISSING: Energy > EnoughEnergy(lifeSupportPerHour * 320)
		var missingEnergy = b.AddBoolStub("missing-energy", true,
			"MISSING: Energy > EnoughEnergy (needs life support per hour * 320)");
		var and1 = b.Add("and1", "And", 880, 20);
		b.Connect(petioleOrBud, "out", and1, "a");
		b.Connect(missingEnergy, "bool", and1, "b");

		var missingAuxin = b.AddBoolStub("missing-auxin", true,
			"MISSING: parentAuxins < threshold and local minimum traversal");
		var and2 = b.Add("and2", "And", 1120, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(missingAuxin, "bool", and2, "b");

		return b.FinishWithActive(and2, "out").Build();
	}

	static global::ExportedGraph BuildGrowthOrganSubgraph(string prefix, string organSocket)
	{
		var b = SubgraphBuilder.Create(prefix);
		var organ = b.Add("organ", "Agent Type Input", 0, 0);

		var notBud = b.Add("not-bud", "Not", 400, 40);
		b.Connect(organ, "bud", notBud, "a");

		var missingEnergy = b.AddBoolStub("missing-energy", true,
			"MISSING: Energy > EnoughEnergy; Organ != Bud (partial: Not bud only)");
		var and1 = b.Add("and1", "And", 640, 0);
		b.Connect(organ, organSocket, and1, "a");
		b.Connect(missingEnergy, "bool", and1, "b");

		var and2 = b.Add("and2", "And", 880, 20);
		b.Connect(notBud, "out", and2, "a");
		b.Connect(and1, "out", and2, "b");

		return b.FinishWithActive(and2, "out").Build();
	}

	public static global::ExportedGraph BuildGrowthLeafSubgraph() => BuildGrowthOrganSubgraph("gr-leaf", "leaf");

	public static global::ExportedGraph BuildGrowthPetioleSubgraph() => BuildGrowthOrganSubgraph("gr-pet", "petiole");

	public static global::ExportedGraph BuildGrowthMeristemSubgraph() => BuildGrowthOrganSubgraph("gr-mer", "meristem");

	public static global::ExportedGraph BuildGrowthStemSubgraph() => BuildGrowthOrganSubgraph("gr-stem", "stem");

	/// <summary>TickDefault lines 623–628 — partial; energy and branch context missing.</summary>
	public static global::ExportedGraph BuildWoodLignifySubgraph()
	{
		var b = SubgraphBuilder.Create("wl");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var c1 = b.AddNum("c1", 1f, 280, 0);

		var woodLt1 = b.Add("wood-lt", "Less Than (or Equal)", 520, 60);
		b.Connect(state, "wood", woodLt1, "a");
		b.Connect(c1, "num", woodLt1, "b");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "stem", and1, "a");
		b.Connect(woodLt1, "out", and1, "b");

		var missingEnergy = b.AddBoolStub("missing-energy", true,
			"MISSING: Energy > EnoughEnergy and Stem|Meristem growth block context");
		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(missingEnergy, "bool", and2, "b");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault lines 632+ — partial; LengthVar and energy gate missing.</summary>
	public static global::ExportedGraph BuildMeristemChainSubgraph()
	{
		var b = SubgraphBuilder.Create("mc");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);

		// MISSING: Length > LengthVar (no LengthVar agent input; placeholder 0 => length >= 0)
		var cLengthVar = b.AddNum("c-length-var", 0f, 280, 60);
		var lengthGt = b.Add("len-gt", "Greater Than (or Equal)", 520, 60);
		b.Connect(state, "length", lengthGt, "a");
		b.Connect(cLengthVar, "num", lengthGt, "b");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "meristem", and1, "a");
		b.Connect(lengthGt, "out", and1, "b");

		var missingEnergy = b.AddBoolStub("missing-energy", true,
			"MISSING: Energy > EnoughEnergy and Organ != Bud");
		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(missingEnergy, "bool", and2, "b");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault lines 686–687 — partial; formation reads missing.</summary>
	public static global::ExportedGraph BuildPetioleCoverBudSubgraph()
	{
		var b = SubgraphBuilder.Create("pcb");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);

		var missingEnergy = b.AddBoolStub("missing-energy", true,
			"MISSING: Energy > EnoughEnergy and else-branch of Stem|Meristem block");
		var missingCover = b.AddBoolStub("missing-cover", true,
			"MISSING: ParentRadiusAtBirth + PetioleCoverThreshold < parent base radius");
		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "petiole", and1, "a");
		b.Connect(missingEnergy, "bool", and1, "b");

		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(missingCover, "bool", and2, "b");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault lines 690–706 — partial; production sum and RNG missing.</summary>
	public static global::ExportedGraph BuildPetioleUnproductiveDeathSubgraph()
	{
		var b = SubgraphBuilder.Create("pud");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var c48 = b.AddNum("c48", 48f, 280, 0);

		var ageOk = b.Add("age-ok", "Greater Than (or Equal)", 520, 60);
		b.Connect(state, "ageHours", ageOk, "a");
		b.Connect(c48, "num", ageOk, "b");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "petiole", and1, "a");
		b.Connect(ageOk, "out", and1, "b");

		var missingParent = b.AddBoolStub("missing-parent", true,
			"MISSING: parent organ != Meristem");
		var missingChildren = b.AddBoolStub("missing-children", true,
			"MISSING: children != null and production sum < 0.5");
		var missingRng = b.AddBoolStub("missing-rng", true,
			"MISSING: NextFloatAccum for unproductive petiole death");

		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(missingParent, "bool", and2, "b");

		var and3 = b.Add("and3", "And", 1240, 60);
		b.Connect(and2, "out", and3, "a");
		b.Connect(missingChildren, "bool", and3, "b");

		var and4 = b.Add("and4", "And", 1480, 80);
		b.Connect(and3, "out", and4, "a");
		b.Connect(missingRng, "bool", and4, "b");

		return b.FinishWithActive(and4, "out").Build();
	}

	/// <summary>TickDefault lines 712–726 — Energy &lt;= 0.</summary>
	public static global::ExportedGraph BuildEnergyDepletionSubgraph()
	{
		var b = SubgraphBuilder.Create("ed");
		var state = b.Add("state", "Agent State Input", 0, 0);
		var c0 = b.AddNum("c0", 0f, 280, 0);

		var starved = b.Add("starved", "Less Than (or Equal)", 520, 0);
		b.Connect(state, "energy", starved, "a");
		b.Connect(c0, "num", starved, "b");

		return b.FinishWithActive(starved, "out").Build();
	}

	/// <summary>TickDefault line 730 — unconditional auxins update.</summary>
	public static global::ExportedGraph BuildAuxinsUpdateSubgraph() =>
		SubgraphBuilder.Create("aux")
			.GateAlwaysTrue()
			.Build();

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
				Data = payload?.ToJsonElement() ?? JsonSerializer.SerializeToElement(new { }),
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
	}
}
