namespace Agro.BehaviorGraph;

/// <summary>Shared Bergania-tick behavior graphs (Geranium ×2, Bergenia).</summary>
public static class BerganiaTickGraphBuilder
{
	/// <summary>Legacy <see cref="SpeciesSettings.pChaningSeaonns"/> default when species Init does not override.</summary>
	public static readonly float[] LegacyDefaultPChaining = [0.0015f, 0.0005f, 0.001f, 0f];

	/// <summary>Legacy <see cref="SpeciesSettings.pFloweringSeaonns"/> default when species Init does not override.</summary>
	public static readonly float[] LegacyDefaultPFlowering = [0.0005f, 0.005f, 0.0003f, 0f];

	public sealed record BerganiaGraphOptions(
		string SpeciesLabel,
		float LeafLength,
		float LeafRadius,
		float PetioleLength,
		float PetioleRadius,
		float MaxLeaveAge,
		float GrowthFactor,
		float MaxRadius,
		float PNewCrown,
		float PExpandRizome,
		float RizomeMaxDepth,
		float RizomeLength,
		float RizomeRadius,
		float[] PChaining,
		float[] PFlowering,
		float DominanceFactor = 0.7f,
		float NodeDistance = 0f,
		float NodeDistanceVar = 0f)
	{
		public static BerganiaGraphOptions GeraniumMacrorrhizum => new(
			SpeciesLabel: "Geranium Macrorrhizum",
			LeafLength: 0.06f,
			LeafRadius: 0.03f,
			PetioleLength: 0.15f,
			PetioleRadius: 0.0018f,
			MaxLeaveAge: 160f,
			GrowthFactor: 0.25f,
			MaxRadius: 0.0035f,
			PNewCrown: 0.70f,
			PExpandRizome: 0.0012f,
			RizomeMaxDepth: 3f,
			RizomeLength: 0.045f,
			RizomeRadius: 0.0035f,
			PChaining: LegacyDefaultPChaining,
			PFlowering: LegacyDefaultPFlowering);

		public static BerganiaGraphOptions GeraniumCantabrigiense => new(
			SpeciesLabel: "Geranium × Cantabrigiense",
			LeafLength: 0.04f,
			LeafRadius: 0.02f,
			PetioleLength: 0.09f,
			PetioleRadius: 0.0017f,
			MaxLeaveAge: 140f,
			GrowthFactor: 0.20f,
			MaxRadius: 0.0030f,
			PNewCrown: 0.45f,
			PExpandRizome: 0.0008f,
			RizomeMaxDepth: 3f,
			RizomeLength: 0.05f,
			RizomeRadius: 0.0030f,
			PChaining: LegacyDefaultPChaining,
			PFlowering: LegacyDefaultPFlowering);

		public static BerganiaGraphOptions BergeniaCordifolia => new(
			SpeciesLabel: "Bergenia Cordifolia",
			LeafLength: 0.24f,
			LeafRadius: 0.09f,
			PetioleLength: 0.005f,
			PetioleRadius: 0.004f,
			MaxLeaveAge: 100f,
			GrowthFactor: 0.20f,
			MaxRadius: 0.005f,
			PNewCrown: 0.50f,
			PExpandRizome: 0.0005f,
			RizomeMaxDepth: 3f,
			RizomeLength: 0.04f,
			RizomeRadius: 0.0025f,
			PChaining: [0.015f, 0.02f, 0.01f, 0f],
			PFlowering: [0.0005f, 0.005f, 0.0003f, 0f]);
	}

	public static class ConfigIds
	{
		public const string GrowthFactor = "default-config-growth-factor";
		public const string MaxRadius = "default-config-max-radius";
		public const string PNewCrown = "default-config-p-new-crown";
		public const string PExpandRizome = "default-config-p-expand-rizome";
		public const string RizomeMaxDepth = "default-config-rizome-max-depth";
		public const string PChaining = "default-config-p-chaining";
		public const string PFlowering = "default-config-p-flowering";
	}

	public static List<BehaviorConfigUploadEntry> BuildConfiguration(BerganiaGraphOptions opt)
	{
		var entries = DefaultSpeciesGraphBuilder.BuildDefaultConfiguration().ToList();
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafLength, opt.LeafLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius, opt.LeafRadius);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength, opt.PetioleLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius, opt.PetioleRadius);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.NodeDistance, opt.NodeDistance);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.NodeDistanceVar, opt.NodeDistanceVar);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleAgeBudMinHours, opt.MaxLeaveAge);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleAgeBudReferenceHours, 8760f * 2f);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactor, opt.DominanceFactor);
		SetArray(entries, DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactors,
			DefaultSpeciesGraphBuilder.BuildDominanceFactorsTable(opt.DominanceFactor));
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.RizomeLength, opt.RizomeLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.RizomeRadius, opt.RizomeRadius);

		AddOrReplace(entries, ConfigIds.GrowthFactor, "Growth factor", opt.GrowthFactor);
		AddOrReplace(entries, ConfigIds.MaxRadius, "Max radius", opt.MaxRadius);
		AddOrReplace(entries, ConfigIds.PNewCrown, "P new crown", opt.PNewCrown);
		AddOrReplace(entries, ConfigIds.PExpandRizome, "P expand rhizome", opt.PExpandRizome);
		AddOrReplace(entries, ConfigIds.RizomeMaxDepth, "Rhizome max depth", opt.RizomeMaxDepth);
		AddOrReplaceArray(entries, ConfigIds.PChaining, "P chaining by phase", opt.PChaining);
		AddOrReplaceArray(entries, ConfigIds.PFlowering, "P flowering by phase", opt.PFlowering);
		return entries;
	}

	public static IReadOnlyList<(string Name, global::ExportedGraph Graph)> BuildSpeciesSubgraphs(BerganiaGraphOptions opt)
	{
		var graphs = new List<(string, global::ExportedGraph)>
		{
			("Life support", BuildLifeSupportSubgraph(skipRhizome: true)),
			("Photosynthesis", DefaultSpeciesGraphBuilder.BuildPhotosynthesisSubgraph()),
			("Petiole age bud", DefaultSpeciesGraphBuilder.BuildPetioleAgeBudSubgraph()),
			("Stem dominance death", BuildStemDominanceDeathSubgraph(skipRhizome: true)),
			("Meristem tick marker", DefaultSpeciesGraphBuilder.BuildMeristemTickMarkerSubgraph()),
			("Spring crown recruitment", BuildSpringCrownSubgraph()),
			("trySpawn reset", BuildTrySpawnResetSubgraph()),
			("Growth leaf", BuildBerganiaGrowthLeafSubgraph()),
			("Growth petiole", BuildBerganiaGrowthPetioleSubgraph()),
			("Growth meristem", BuildBerganiaGrowthMeristemSubgraph()),
			("Growth stem", BuildBerganiaGrowthStemSubgraph()),
			("Wood lignify", BuildWoodLignifyNoOpSubgraph()),
			("Meristem chain", BuildBerganiaMeristemChainSubgraph(opt)),
			("Petiole cover bud", DefaultSpeciesGraphBuilder.BuildPetioleCoverBudSubgraph()),
			("Petiole unproductive death", DefaultSpeciesGraphBuilder.BuildPetioleUnproductiveDeathSubgraph()),
			("Energy depletion", BuildBerganiaEnergyDepletionSubgraph()),
			("Rhizome test3 arm", BuildRhizomeTest3ArmSubgraph()),
			("Rhizome expansion", BuildRhizomeExpansionSubgraph()),
			("Auxins update", DefaultSpeciesGraphBuilder.BuildAuxinsUpdateSubgraph()),
		};

		if (opt.SpeciesLabel == "Bergenia Cordifolia")
			graphs.Add(("Flower organs gap", BuildFlowerGapCommentSubgraph()));

		return graphs;
	}

	static global::ExportedGraph BuildBerganiaGrowthLeafSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-leaf-berg");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);

		var activeGate = b.WireBerganiaGrowthActiveGate(organ, state, "leaf");
		var (deltaLen, deltaRad, sizeLimitL, sizeLimitR) = b.WireLeafPetioleGrowthDeltas(
			state, form,
			DefaultSpeciesGraphBuilder.ConfigIds.LeafLength,
			DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius,
			capRadiusToParent: false);
		var underLimit = b.WireUnderSizeLimit(state, sizeLimitL, sizeLimitR);
		var growthGate = b.Add("growth-gate", "And", 2200, 40);
		b.Connect(activeGate, "out", growthGate, "a");
		b.Connect(underLimit, "out", growthGate, "b");

		var growth = b.Add("growth", "Growth", 2400, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(growthGate, "out").Build();
	}

	static global::ExportedGraph BuildBerganiaGrowthPetioleSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-pet-berg");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);

		var activeGate = b.WireBerganiaGrowthActiveGate(organ, state, "petiole");
		var (deltaLen, deltaRad, sizeLimitL, sizeLimitR) = b.WireLeafPetioleGrowthDeltas(
			state, form,
			DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength,
			DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius,
			capRadiusToParent: true);
		var underLimit = b.WireUnderSizeLimit(state, sizeLimitL, sizeLimitR);
		var growthGate = b.Add("growth-gate", "And", 2200, 40);
		b.Connect(activeGate, "out", growthGate, "a");
		b.Connect(underLimit, "out", growthGate, "b");

		var growth = b.Add("growth", "Growth", 2400, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(growthGate, "out").Build();
	}

	static global::ExportedGraph BuildBerganiaGrowthMeristemSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-mer-berg");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var sim = b.Add("sim", "Simulation Settings Input", 0, 180);

		var activeGate = b.WireBerganiaGrowthActiveGate(organ, state, "meristem");
		var (deltaLen, deltaRad) = b.WireMeristemGrowthDeltas(
			state, form, sim,
			growthFactorConfigId: ConfigIds.GrowthFactor,
			maxRadiusConfigId: ConfigIds.MaxRadius,
			gateLengthByLengthVar: true);

		var growth = b.Add("growth", "Growth", 2600, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(activeGate, "out").Build();
	}

	static global::ExportedGraph BuildBerganiaGrowthStemSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-stem-berg");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var sim = b.Add("sim", "Simulation Settings Input", 0, 180);
		var phase = b.Add("phase", "Phase Input", 0, 240);

		var activeGate = b.WireBerganiaGrowthActiveGate(organ, state, "stem");
		var notReset = b.Add("not-reset", "Not", 1600, 240);
		b.Connect(phase, "resetPending", notReset, "a");
		var andStemReset = b.Add("and-stem-reset", "And", 1840, 120);
		b.Connect(activeGate, "out", andStemReset, "a");
		b.Connect(notReset, "out", andStemReset, "b");

		var deltaRad = b.WireBerganiaStemGrowthDelta(
			state, form, sim,
			ConfigIds.GrowthFactor,
			ConfigIds.MaxRadius);
		var c0 = b.AddNum("c0", 0f, 2600, 160);

		var growth = b.Add("growth", "Growth", 2600, 200);
		b.Connect(c0, "num", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(andStemReset, "out").Build();
	}

	static global::ExportedGraph BuildBerganiaMeristemChainSubgraph(BerganiaGraphOptions opt)
	{
		var b = SubgraphBuilder.Create("mc-berg");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var phase = b.Add("phase", "Phase Input", 0, 120);

		var lengthGt = b.Add("len-gt", "Greater Than", 520, 60);
		b.Connect(state, "length", lengthGt, "a");
		b.Connect(state, "lengthVar", lengthGt, "b");

		var notBud = b.Add("not-bud", "Not", 520, 20);
		b.Connect(organ, "bud", notBud, "a");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "meristem", and1, "a");
		b.Connect(lengthGt, "out", and1, "b");

		var enough = b.WireEnoughEnergy(organ, state);
		var notRizome = b.Add("not-riz", "Not", 1000, 80);
		b.Connect(state, "isRizome", notRizome, "a");

		var and2 = b.Add("and2", "And", 1240, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(notBud, "out", and2, "b");

		var and3 = b.Add("and3", "And", 1480, 60);
		b.Connect(and2, "out", and3, "a");
		b.Connect(enough, "out", and3, "b");

		var and4 = b.Add("and4", "And", 1720, 80);
		b.Connect(and3, "out", and4, "a");
		b.Connect(notRizome, "out", and4, "b");

		var pChainArr = b.AddConfigArray("p-chain-arr", ConfigIds.PChaining, 1960, 80, "pChaningSeaonns[phase]");
		b.Connect(phase, "phaseIndex", pChainArr, "index");
		var chainRng = b.Add("chain-rng", "Random Chance Input", 2200, 80);
		b.Connect(pChainArr, "out", chainRng, "p");

		var chainTrig = b.Add("chain-trig", "And", 2440, 60);
		b.Connect(and4, "out", chainTrig, "a");
		b.Connect(chainRng, "out", chainTrig, "b");

		var becomeStem = b.Add("become-stem", "Become Stem", 2200, 120);
		b.Connect(chainTrig, "out", becomeStem, "trigger");

		var wasTrue = b.AddBool("was-val", true, 2200, 160);
		var setWas = b.Add("set-was", "Set Was Meristem", 2440, 160);
		b.Connect(wasTrue, "bool", setWas, "value");
		b.Connect(chainTrig, "out", setWas, "trigger");

		var preFlower = b.Add("pre-flower", "Equal To", 1960, 120);
		b.Connect(phase, "preFlower", preFlower, "a");
		var t = b.AddBool("t", true, 1960, 160);
		b.Connect(t, "bool", preFlower, "b");

		var preFlowerTrig = b.Add("pre-flower-trig", "And", 2200, 200);
		b.Connect(chainTrig, "out", preFlowerTrig, "a");
		b.Connect(preFlower, "out", preFlowerTrig, "b");

		var spawnFlower = b.Add("spawn-flower", "Spawn Flower Meristem", 2440, 200);
		b.Connect(preFlowerTrig, "out", spawnFlower, "trigger");

		var spawnMeristem = b.Add("spawn-mer", "Spawn Meristem", 2440, 120);
		b.Connect(chainTrig, "out", spawnMeristem, "trigger");

		var createLeaves = b.Add("create-leaves", "Create Leaves", 2680, 120);
		b.Connect(spawnMeristem, "seq", createLeaves, "trigger");
		b.Connect(spawnMeristem, "childId", createLeaves, "meristemId");

		b.Add("bend-gap", "Number Input", 2680, 200,
			GraphNodePayload.FromComment("GAP: bendPetiol multi-agent orientation mutation not implemented in graph mode."));

		return b.FinishWithActive(and4, "out").Build();
	}

	static global::ExportedGraph BuildWoodLignifyNoOpSubgraph()
	{
		var b = SubgraphBuilder.Create("wood-noop");
		var never = b.AddBool("never", false, 0, 0);
		b.Add("wood-gap", "Number Input", 240, 0,
			GraphNodePayload.FromComment("Bergania.Tick: stem wood-factor update is commented out in legacy."));
		return b.FinishWithActive(never, "bool").Build();
	}

	static global::ExportedGraph BuildBerganiaEnergyDepletionSubgraph()
	{
		var b = SubgraphBuilder.Create("ed-berg");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var c0 = b.AddNum("c0", 0f, 280, 0);

		var starved = b.Add("starved", "Less Than or Equal", 520, 0);
		b.Connect(state, "energy", starved, "a");
		b.Connect(c0, "num", starved, "b");

		var notRizome = b.Add("not-riz", "Not", 520, 40);
		b.Connect(state, "isRizome", notRizome, "a");

		var active = b.Add("active", "And", 760, 0);
		b.Connect(starved, "out", active, "a");
		b.Connect(notRizome, "out", active, "b");

		var makeBudPetiole = b.Add("make-bud-pet", "Make Bud", 1000, 160);
		var andPetiole = b.Add("and-pet", "And", 760, 160);
		b.Connect(active, "out", andPetiole, "a");
		b.Connect(organ, "petiole", andPetiole, "b");
		b.Connect(andPetiole, "out", makeBudPetiole, "trigger");

		var deathLeaf = b.Add("death-leaf", "Death", 1000, 200);
		var andLeaf = b.Add("and-leaf", "And", 760, 200);
		b.Connect(active, "out", andLeaf, "a");
		b.Connect(organ, "leaf", andLeaf, "b");
		b.Connect(andLeaf, "out", deathLeaf, "trigger");

		var deathParent = b.Add("death-parent", "Death Parent", 1240, 200);
		b.Connect(andLeaf, "out", deathParent, "trigger");

		var makeBudStemRhizome = b.Add("make-bud-stem", "Make Bud", 1240, 280);
		var andStem = b.Add("and-stem", "And", 1000, 280);
		b.Connect(active, "out", andStem, "a");
		b.Connect(organ, "stem", andStem, "b");
		var andStemRiz = b.Add("and-stem-riz", "And", 1240, 280);
		b.Connect(andStem, "out", andStemRiz, "a");
		b.Connect(form, "parentIsRhizome", andStemRiz, "b");
		b.Connect(andStemRiz, "out", makeBudStemRhizome, "trigger");

		var deathStem = b.Add("death-stem", "Death", 1240, 320);
		var notParentRiz = b.Add("not-parent-riz", "Not", 1000, 320);
		b.Connect(form, "parentIsRhizome", notParentRiz, "a");
		var andStemDeath = b.Add("and-stem-death", "And", 1240, 320);
		b.Connect(andStem, "out", andStemDeath, "a");
		b.Connect(notParentRiz, "out", andStemDeath, "b");
		b.Connect(andStemDeath, "out", deathStem, "trigger");

		var makeBudOther = b.Add("make-bud-other", "Make Bud", 1240, 400);
		var notPetiole = b.Add("not-pet", "Not", 760, 360);
		b.Connect(organ, "petiole", notPetiole, "a");
		var notLeaf = b.Add("not-leaf", "Not", 760, 400);
		b.Connect(organ, "leaf", notLeaf, "a");
		var notStem = b.Add("not-stem", "Not", 760, 440);
		b.Connect(organ, "stem", notStem, "a");
		var notBud = b.Add("not-bud", "Not", 760, 480);
		b.Connect(organ, "bud", notBud, "a");

		var andOther = b.Add("and-other", "And", 1000, 400);
		b.Connect(active, "out", andOther, "a");
		b.Connect(notPetiole, "out", andOther, "b");
		var andOther2 = b.Add("and-other2", "And", 1240, 400);
		b.Connect(andOther, "out", andOther2, "a");
		b.Connect(notLeaf, "out", andOther2, "b");
		var andOther3 = b.Add("and-other3", "And", 1480, 400);
		b.Connect(andOther2, "out", andOther3, "a");
		b.Connect(notStem, "out", andOther3, "b");
		var andOther4 = b.Add("and-other4", "And", 1720, 400);
		b.Connect(andOther3, "out", andOther4, "a");
		b.Connect(notBud, "out", andOther4, "b");
		var andOtherRiz = b.Add("and-other-riz", "And", 1960, 400);
		b.Connect(andOther4, "out", andOtherRiz, "a");
		b.Connect(form, "parentIsRhizome", andOtherRiz, "b");
		b.Connect(andOtherRiz, "out", makeBudOther, "trigger");

		b.Add("orient-gap", "Number Input", 2200, 400,
			GraphNodePayload.FromComment("GAP: parent orientation on MakeBud not implemented in graph mode."));

		return b.FinishWithActive(active, "out").Build();
	}

	static global::ExportedGraph BuildLifeSupportSubgraph(bool skipRhizome)
	{
		if (!skipRhizome)
			return DefaultSpeciesGraphBuilder.BuildLifeSupportSubgraph();

		var b = SubgraphBuilder.Create("ls-berg");
		var always = b.AddBool("always", true, 0, 0);
		var organ = b.Add("organ", "Agent Type Input", 0, 80);
		var state = b.Add("state", "Agent State Input", 0, 140);
		var sim = b.Add("sim", "Simulation Settings Input", 0, 200);
		var c0 = b.AddNum("c0", 0f, 240, 160);

		var notRizome = b.Add("not-riz", "Not", 400, 40);
		b.Connect(state, "isRizome", notRizome, "a");
		var gate = b.Add("gate", "And", 640, 20);
		b.Connect(always, "bool", gate, "a");
		b.Connect(notRizome, "out", gate, "b");

		var perHour = b.WireLifeSupportPerHour(organ, state);
		var perTick = b.Add("per-tick", "Multiply", 1440, 100);
		b.Connect(perHour, "out", perTick, "a");
		b.Connect(sim, "hoursPerTick", perTick, "b");

		var negTick = b.Add("neg-tick", "Subtract", 1680, 100);
		b.Connect(c0, "num", negTick, "a");
		b.Connect(perTick, "out", negTick, "b");

		var dEnergy = b.Add("d-energy", "Delta Energy", 1920, 100);
		b.Connect(negTick, "out", dEnergy, "amount");

		return b.FinishWithActive(gate, "out").Build();
	}

	static global::ExportedGraph BuildStemDominanceDeathSubgraph(bool skipRhizome)
	{
		if (!skipRhizome)
			return DefaultSpeciesGraphBuilder.BuildStemDominanceDeathSubgraph();

		var b = SubgraphBuilder.Create("sdd-berg");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var minDom = b.AddConfig("min-dom", DefaultSpeciesGraphBuilder.ConfigIds.MinDominanceForStemDeath, false, 280, 0,
			"DominanceLevel must exceed this for height-based stem death");

		var domGt1 = b.Add("dom-gt1", "Greater Than", 520, 0);
		b.Connect(state, "dominanceLevel", domGt1, "a");
		b.Connect(minDom, "num", domGt1, "b");

		var parentDomLt = b.Add("parent-dom-lt", "Less Than", 520, 60);
		b.Connect(form, "parentDominance", parentDomLt, "a");
		b.Connect(state, "dominanceLevel", parentDomLt, "b");

		var notRizome = b.Add("not-riz", "Not", 520, 100);
		b.Connect(state, "isRizome", notRizome, "a");

		var andDom = b.Add("and-dom", "And", 760, 20);
		b.Connect(domGt1, "out", andDom, "a");
		b.Connect(parentDomLt, "out", andDom, "b");

		var and1 = b.Add("and1", "And", 760, 60);
		b.Connect(organ, "stem", and1, "a");
		b.Connect(andDom, "out", and1, "b");

		var and2 = b.Add("and2", "And", 1000, 80);
		b.Connect(and1, "out", and2, "a");
		b.Connect(notRizome, "out", and2, "b");

		var deathP = b.WireStemDeathProbability(state, form);
		var rng = b.WireRandomAccumChance(deathP, "out");
		var c0 = b.AddNum("c0", 0f, 1920, 200);
		var setEnergy = b.Add("set-energy", "Set Energy", 2160, 200);
		b.Connect(c0, "num", setEnergy, "value");
		b.Connect(rng, "out", setEnergy, "trigger");

		return b.FinishWithActive(and2, "out").Build();
	}

	static global::ExportedGraph BuildSpringCrownSubgraph()
	{
		var b = SubgraphBuilder.Create("spring");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var phase = b.Add("phase", "Phase Input", 0, 180);

		var isBud = b.Add("is-bud", "Equal To", 400, 0);
		b.Connect(organ, "bud", isBud, "a");
		var budTrue = b.AddBool("bud-true", true, 400, 40);
		b.Connect(budTrue, "bool", isBud, "b");

		var notRizome = b.Add("not-riz", "Not", 400, 80);
		b.Connect(state, "isRizome", notRizome, "a");

		var preFlower = b.Add("pre-flower", "Equal To", 400, 120);
		b.Connect(phase, "preFlower", preFlower, "a");
		var t = b.AddBool("t", true, 400, 160);
		b.Connect(t, "bool", preFlower, "b");

		var pNew = b.AddConfig("p-new", ConfigIds.PNewCrown, false, 640, 80);
		var rng = b.Add("rng", "Random Chance Input", 880, 80);
		b.Connect(pNew, "num", rng, "p");

		var andGate = b.Add("and-gate", "And", 1120, 60);
		b.Connect(isBud, "out", andGate, "a");
		b.Connect(form, "parentIsRhizome", andGate, "b");
		var andRiz = b.Add("and-riz", "And", 1360, 80);
		b.Connect(andGate, "out", andRiz, "a");
		b.Connect(notRizome, "out", andRiz, "b");
		var and2 = b.Add("and2", "And", 1600, 100);
		b.Connect(andRiz, "out", and2, "a");
		b.Connect(preFlower, "out", and2, "b");
		var and3 = b.Add("and3", "And", 1840, 120);
		b.Connect(and2, "out", and3, "a");
		b.Connect(state, "trySpawn", and3, "b");
		var and4 = b.Add("and4", "And", 2080, 140);
		b.Connect(and3, "out", and4, "a");
		b.Connect(rng, "out", and4, "b");

		var become = b.Add("become", "Become Meristem", 2320, 140);
		b.Connect(and4, "out", become, "trigger");

		var initRadius = b.AddNum("init-radius", AboveGroundAgent.InitialRadius, 2320, 220);
		var setRadius = b.Add("set-radius", "Set Radius", 2560, 220);
		b.Connect(become, "seq", setRadius, "trigger");
		b.Connect(initRadius, "num", setRadius, "value");

		var setEnergy = b.Add("set-energy", "Set Energy", 2800, 220);
		b.Connect(setRadius, "seq", setEnergy, "trigger");
		b.Connect(state, "energyStorageCapacity", setEnergy, "value");

		var nodeDist = b.AddConfig("node-dist", DefaultSpeciesGraphBuilder.ConfigIds.NodeDistance, false, 2800, 260);
		var nodeDistVar = b.AddConfig("node-dist-var", DefaultSpeciesGraphBuilder.ConfigIds.NodeDistanceVar, false, 2800, 300);
		var rngVar = b.Add("rng-var", "Random Float Var Input", 3040, 300);
		b.Connect(nodeDistVar, "num", rngVar, "variance");
		var lengthVar = b.Add("length-var", "Add", 3280, 260);
		b.Connect(nodeDist, "num", lengthVar, "a");
		b.Connect(rngVar, "out", lengthVar, "b");
		var setLenVar = b.Add("set-len-var", "Set Length Var", 3040, 220);
		b.Connect(setEnergy, "seq", setLenVar, "trigger");
		b.Connect(lengthVar, "out", setLenVar, "value");

		var domOne = b.AddNum("dom-one", 1f, 3280, 220);
		var deltaDom = b.Add("delta-dom", "Delta Dominance", 3520, 220);
		b.Connect(setLenVar, "seq", deltaDom, "trigger");
		b.Connect(domOne, "num", deltaDom, "count");

		var turn = b.Add("turn", "Turn Upwards", 3760, 220);
		b.Connect(deltaDom, "seq", turn, "trigger");

		b.Add("yaw-gap", "Number Input", 3760, 260,
			GraphNodePayload.FromComment("GAP: spring crown yaw orientation from RNG not implemented (Turn Upwards only)."));

		var leaves = b.Add("leaves", "Create Leaves", 4000, 220);
		b.Connect(turn, "seq", leaves, "trigger");

		var notRng = b.Add("not-rng", "Not", 2080, 180);
		b.Connect(rng, "out", notRng, "a");
		var failGate = b.Add("fail-gate", "And", 2320, 180);
		b.Connect(and3, "out", failGate, "a");
		b.Connect(notRng, "out", failGate, "b");
		var tryFalse = b.AddBool("try-false", false, 2560, 220);
		var setTryFalse = b.Add("set-try-false", "Set trySpawn", 2560, 180);
		b.Connect(tryFalse, "bool", setTryFalse, "value");
		b.Connect(failGate, "out", setTryFalse, "trigger");

		return b.FinishWithActive(andRiz, "out").Build();
	}

	static global::ExportedGraph BuildTrySpawnResetSubgraph()
	{
		var b = SubgraphBuilder.Create("try-spawn");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var phase = b.Add("phase", "Phase Input", 0, 180);

		var reset = b.Add("reset", "Equal To", 400, 0);
		b.Connect(phase, "resetPending", reset, "a");
		var t = b.AddBool("t", true, 400, 40);
		b.Connect(t, "bool", reset, "b");

		var isBud = b.Add("is-bud", "Equal To", 400, 80);
		b.Connect(organ, "bud", isBud, "a");
		b.Connect(t, "bool", isBud, "b");

		var notRizome = b.Add("not-riz", "Not", 400, 120);
		b.Connect(state, "isRizome", notRizome, "a");

		var and1 = b.Add("and1", "And", 640, 40);
		b.Connect(reset, "out", and1, "a");
		b.Connect(isBud, "out", and1, "b");
		var and2 = b.Add("and2", "And", 880, 60);
		b.Connect(and1, "out", and2, "a");
		b.Connect(form, "parentIsRhizome", and2, "b");
		var and3 = b.Add("and3", "And", 1120, 80);
		b.Connect(and2, "out", and3, "a");
		b.Connect(notRizome, "out", and3, "b");

		var setTry = b.Add("set-try", "Set trySpawn", 1360, 80);
		var tryTrue = b.AddBool("try-true", true, 1360, 120);
		b.Connect(tryTrue, "bool", setTry, "value");
		b.Connect(and3, "out", setTry, "trigger");

		return b.FinishWithActive(and3, "out").Build();
	}

	static global::ExportedGraph BuildRhizomeTest3ArmSubgraph()
	{
		var b = SubgraphBuilder.Create("riz-t3");
		var state = b.Add("state", "Agent State Input", 0, 0);
		var phase = b.Add("phase", "Phase Input", 0, 60);

		var isRizome = b.Add("is-riz", "Equal To", 400, 0);
		b.Connect(state, "isRizome", isRizome, "a");
		var t = b.AddBool("t", true, 400, 40);
		b.Connect(t, "bool", isRizome, "b");

		var preFlower = b.Add("pre-flower", "Equal To", 400, 80);
		b.Connect(phase, "preFlower", preFlower, "a");
		b.Connect(t, "bool", preFlower, "b");

		var notTest3 = b.Add("not-t3", "Not", 400, 120);
		b.Connect(state, "rizomeTest3", notTest3, "a");

		var and1 = b.Add("and1", "And", 640, 40);
		b.Connect(isRizome, "out", and1, "a");
		b.Connect(preFlower, "out", and1, "b");
		var and2 = b.Add("and2", "And", 880, 60);
		b.Connect(and1, "out", and2, "a");
		b.Connect(notTest3, "out", and2, "b");

		var setTrue = b.AddBool("set-true", true, 1120, 100);
		var setTest3 = b.Add("set-t3", "Set Rizome Test3", 1120, 60);
		b.Connect(setTrue, "bool", setTest3, "value");
		b.Connect(and2, "out", setTest3, "trigger");

		return b.FinishWithActive(and2, "out").Build();
	}

	static global::ExportedGraph BuildRhizomeExpansionSubgraph()
	{
		var b = SubgraphBuilder.Create("riz-exp");
		var state = b.Add("state", "Agent State Input", 0, 0);
		var phase = b.Add("phase", "Phase Input", 0, 60);
		var agentId = b.Add("agent-id", "Agent Id Input", 0, 120);

		var isRizome = b.Add("is-riz", "Equal To", 400, 0);
		b.Connect(state, "isRizome", isRizome, "a");
		var t = b.AddBool("t", true, 400, 40);
		b.Connect(t, "bool", isRizome, "b");

		var maxDepth = b.AddConfig("max-depth", ConfigIds.RizomeMaxDepth, false, 400, 120);
		var depthLt = b.Add("depth-lt", "Less Than", 640, 120);
		b.Connect(state, "rizomeDepth", depthLt, "a");
		b.Connect(maxDepth, "num", depthLt, "b");

		var notReset = b.Add("not-reset", "Not", 640, 160);
		b.Connect(phase, "resetPending", notReset, "a");

		var hasTest3 = b.Add("has-t3", "Equal To", 640, 200);
		b.Connect(state, "rizomeTest3", hasTest3, "a");
		b.Connect(t, "bool", hasTest3, "b");

		var pExpand = b.AddConfig("p-expand", ConfigIds.PExpandRizome, false, 640, 240);
		var baseGate = b.Add("base-gate", "And", 880, 80);
		b.Connect(isRizome, "out", baseGate, "a");
		b.Connect(notReset, "out", baseGate, "b");
		var baseGate2 = b.Add("base-gate2", "And", 1120, 100);
		b.Connect(baseGate, "out", baseGate2, "a");
		b.Connect(depthLt, "out", baseGate2, "b");
		var baseGate3 = b.Add("base-gate3", "And", 1360, 120);
		b.Connect(baseGate2, "out", baseGate3, "a");
		b.Connect(hasTest3, "out", baseGate3, "b");

		WireRhizomeSpawnBranch(b, baseGate3, state, pExpand, "test", "rizomeTest", "Set Rizome Test", 1600, 0);
		WireRhizomeSpawnBranch(b, baseGate3, state, pExpand, "test2", "rizomeTest2", "Set Rizome Test2", 1600, 160);
		WireRhizomeSpawnBranch(b, baseGate3, state, pExpand, "test4", "rizomeTest4", "Set Rizome Test4", 1600, 320,
			agentId, requireRoot: true);

		b.Add("orient-gap", "Number Input", 2800, 320,
			GraphNodePayload.FromComment("GAP: per-branch rhizome spawn orientations + collision/soil checks not implemented."));

		return b.FinishWithActive(baseGate3, "out").Build();
	}

	static void WireRhizomeSpawnBranch(
		SubgraphBuilder b,
		string baseGateId,
		string stateId,
		string pExpandId,
		string rngSuffix,
		string testSocket,
		string clearNodeType,
		int x,
		int y,
		string? agentIdId = null,
		bool requireRoot = false)
	{
		var hasTest = b.Add($"has-{rngSuffix}", "Equal To", x, y);
		b.Connect(stateId, testSocket, hasTest, "a");
		var t = b.AddBool($"t-{rngSuffix}", true, x, y + 40);
		b.Connect(t, "bool", hasTest, "b");

		var rng = b.Add($"rng-{rngSuffix}", "Random Chance Input", x + 240, y + 20);
		b.Connect(pExpandId, "num", rng, "p");

		var gate = b.Add($"gate-{rngSuffix}", "And", x + 480, y + 20);
		b.Connect(baseGateId, "out", gate, "a");
		b.Connect(hasTest, "out", gate, "b");
		var gate2 = b.Add($"gate2-{rngSuffix}", "And", x + 720, y + 20);
		b.Connect(gate, "out", gate2, "a");
		b.Connect(rng, "out", gate2, "b");

		if (requireRoot && agentIdId is not null)
		{
			var isRoot = b.Add($"root-{rngSuffix}", "Equal To", x + 480, y + 80);
			b.Connect(agentIdId, "agentId", isRoot, "a");
			var c0 = b.AddNum($"c0-{rngSuffix}", 0f, x + 480, y + 120);
			b.Connect(c0, "num", isRoot, "b");
			var gate3 = b.Add($"gate3-{rngSuffix}", "And", x + 960, y + 20);
			b.Connect(gate2, "out", gate3, "a");
			b.Connect(isRoot, "out", gate3, "b");
			gate2 = gate3;
		}

		var spawn = b.Add($"spawn-{rngSuffix}", "Spawn Rhizome", x + 1200, y + 20);
		b.Connect(gate2, "out", spawn, "trigger");

		var clearFalse = b.AddBool($"clear-false-{rngSuffix}", false, x + 1440, y + 60);
		var clearTest = b.Add($"clear-{rngSuffix}", clearNodeType, x + 1440, y + 20);
		b.Connect(clearFalse, "bool", clearTest, "value");
		b.Connect(gate2, "out", clearTest, "trigger");
	}

	/// <summary>Documents FlowerHelper gap — graph compiles; no flower-organ behavior in node mode.</summary>
	static global::ExportedGraph BuildFlowerGapCommentSubgraph()
	{
		var b = SubgraphBuilder.Create("flower-gap");
		var never = b.AddBool("never", false, 400, 0);
		b.Add("flower-gap-note", "Number Input", 640, 0,
			GraphNodePayload.FromComment("GAP: FlowerHelper not implemented in behavior-graph mode."));
		return b.FinishWithActive(never, "bool").Build();
	}

	static void SetNumber(List<BehaviorConfigUploadEntry> entries, string id, float value)
	{
		var i = entries.FindIndex(e => e.Id == id);
		if (i < 0) return;
		var e = entries[i];
		entries[i] = new BehaviorConfigUploadEntry
		{
			Id = e.Id,
			Key = e.Key,
			Label = e.Label,
			Usage = e.Usage,
			Type = e.Type,
			Value = BehaviorGraphJson.Number(value),
		};
	}

	static void SetArray(List<BehaviorConfigUploadEntry> entries, string id, float[] values)
	{
		var i = entries.FindIndex(e => e.Id == id);
		if (i < 0) return;
		var e = entries[i];
		entries[i] = new BehaviorConfigUploadEntry
		{
			Id = e.Id,
			Key = e.Key,
			Label = e.Label,
			Usage = e.Usage,
			Type = "number[]",
			Value = BehaviorGraphJson.NumberArray(values),
		};
	}

	static void AddOrReplace(List<BehaviorConfigUploadEntry> entries, string id, string label, float value)
	{
		var i = entries.FindIndex(e => e.Id == id);
		var entry = new BehaviorConfigUploadEntry
		{
			Id = id,
			Key = label,
			Label = label,
			Type = "number",
			Value = BehaviorGraphJson.Number(value),
		};
		if (i < 0) entries.Add(entry);
		else entries[i] = entry;
	}

	static void AddOrReplaceArray(List<BehaviorConfigUploadEntry> entries, string id, string label, float[] values)
	{
		var i = entries.FindIndex(e => e.Id == id);
		var entry = new BehaviorConfigUploadEntry
		{
			Id = id,
			Key = label,
			Label = label,
			Type = "number[]",
			Value = BehaviorGraphJson.NumberArray(values),
		};
		if (i < 0) entries.Add(entry);
		else entries[i] = entry;
	}
}
