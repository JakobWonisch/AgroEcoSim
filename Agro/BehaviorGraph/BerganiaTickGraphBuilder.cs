using System.Text.Json;

namespace Agro.BehaviorGraph;

/// <summary>Shared Bergania-tick behavior graphs (Geranium ×2, Bergenia).</summary>
public static class BerganiaTickGraphBuilder
{
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
		float DominanceFactor = 0.7f)
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
			PChaining: [0.01f, 0.015f, 0.008f, 0f]);

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
			PChaining: [0.01f, 0.015f, 0.008f, 0f]);

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
			PChaining: [0.015f, 0.02f, 0.01f, 0f]);
	}

	public static class ConfigIds
	{
		public const string GrowthFactor = "default-config-growth-factor";
		public const string MaxRadius = "default-config-max-radius";
		public const string PNewCrown = "default-config-p-new-crown";
		public const string PExpandRizome = "default-config-p-expand-rizome";
		public const string RizomeMaxDepth = "default-config-rizome-max-depth";
		public const string PChaining = "default-config-p-chaining";
	}

	public static List<BehaviorConfigUploadEntry> BuildConfiguration(BerganiaGraphOptions opt)
	{
		var entries = DefaultSpeciesGraphBuilder.BuildDefaultConfiguration().ToList();
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafLength, opt.LeafLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius, opt.LeafRadius);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength, opt.PetioleLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius, opt.PetioleRadius);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleUnproductiveMinAgeHours, opt.MaxLeaveAge);
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
		return entries;
	}

	public static IReadOnlyList<(string Name, global::ExportedGraph Graph)> BuildSpeciesSubgraphs(BerganiaGraphOptions opt)
	{
		var core = DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs()
			.Where(g => g.Name != "Auxin twig")
			.ToList();

		core[0] = ("Life support", BuildLifeSupportSubgraph(skipRhizome: true));
		core[3] = ("Stem dominance death", BuildStemDominanceDeathSubgraph(skipRhizome: true));

		core.Add(("Spring crown recruitment", BuildSpringCrownSubgraph()));
		core.Add(("trySpawn reset", BuildTrySpawnResetSubgraph()));
		core.Add(("Rhizome expansion", BuildRhizomeExpansionSubgraph()));

		if (opt.SpeciesLabel == "Bergenia Cordifolia")
			core.Add(("Flower organs gap", BuildFlowerGapCommentSubgraph()));

		return core;
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

		var preFlower = b.Add("pre-flower", "Equal To", 400, 120);
		b.Connect(phase, "preFlower", preFlower, "a");
		var t = b.AddBool("t", true, 400, 160);
		b.Connect(t, "bool", preFlower, "b");

		var pNew = b.AddConfig("p-new", ConfigIds.PNewCrown, false, 640, 80);
		var rng = b.Add("rng", "Random Chance Input", 880, 80);
		b.Connect(pNew, "num", rng, "probability");

		var andGate = b.Add("and-gate", "And", 1120, 60);
		b.Connect(isBud, "out", andGate, "a");
		b.Connect(form, "parentIsRhizome", andGate, "b");
		var and2 = b.Add("and2", "And", 1360, 80);
		b.Connect(andGate, "out", and2, "a");
		b.Connect(preFlower, "out", and2, "b");
		var and3 = b.Add("and3", "And", 1600, 100);
		b.Connect(and2, "out", and3, "a");
		b.Connect(state, "trySpawn", and3, "b");
		var and4 = b.Add("and4", "And", 1840, 120);
		b.Connect(and3, "out", and4, "a");
		b.Connect(rng, "out", and4, "b");

		var become = b.Add("become", "Become Meristem", 2080, 120);
		b.Connect(and4, "out", become, "trigger");

		var domOne = b.AddNum("dom-one", 1f, 2080, 160);
		var deltaDom = b.Add("delta-dom", "Delta Dominance", 2320, 160);
		b.Connect(become, "seq", deltaDom, "trigger");
		b.Connect(domOne, "num", deltaDom, "count");

		var turn = b.Add("turn", "Turn Upwards", 2560, 160);
		b.Connect(deltaDom, "seq", turn, "trigger");

		var leaves = b.Add("leaves", "Create Leaves", 2800, 160);
		b.Connect(turn, "seq", leaves, "trigger");

		return b.FinishWithActive(and4, "out").Build();
	}

	static global::ExportedGraph BuildTrySpawnResetSubgraph()
	{
		var b = SubgraphBuilder.Create("try-spawn");
		var phase = b.Add("phase", "Phase Input", 0, 0);
		var reset = b.Add("reset", "Equal To", 400, 0);
		b.Connect(phase, "resetPending", reset, "a");
		var t = b.AddBool("t", true, 400, 40);
		b.Connect(t, "bool", reset, "b");

		var setTry = b.Add("set-try", "Set trySpawn", 640, 0);
		var tryTrue = b.AddBool("try-true", true, 640, 40);
		b.Connect(tryTrue, "bool", setTry, "value");
		b.Connect(reset, "out", setTry, "trigger");

		return b.FinishWithActive(reset, "out").Build();
	}

	static global::ExportedGraph BuildRhizomeExpansionSubgraph()
	{
		var b = SubgraphBuilder.Create("riz-exp");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var phase = b.Add("phase", "Phase Input", 0, 120);

		var isRizome = b.Add("is-riz", "Equal To", 400, 0);
		b.Connect(state, "isRizome", isRizome, "a");
		var t = b.AddBool("t", true, 400, 40);
		b.Connect(t, "bool", isRizome, "b");

		var maxDepth = b.AddConfig("max-depth", ConfigIds.RizomeMaxDepth, false, 400, 120);
		var depthLt = b.Add("depth-lt", "Less Than", 640, 120);
		b.Connect(state, "dominanceLevel", depthLt, "a");
		b.Connect(maxDepth, "num", depthLt, "b");

		var notReset = b.Add("not-reset", "Not", 640, 160);
		b.Connect(phase, "resetPending", notReset, "a");

		var pExpand = b.AddConfig("p-expand", ConfigIds.PExpandRizome, false, 640, 200);
		var rng = b.Add("rng", "Random Chance Input", 880, 200);
		b.Connect(pExpand, "num", rng, "probability");

		var and1 = b.Add("and1", "And", 1120, 40);
		b.Connect(isRizome, "out", and1, "a");
		b.Connect(notReset, "out", and1, "b");
		var and2 = b.Add("and2", "And", 1360, 80);
		b.Connect(and1, "out", and2, "a");
		b.Connect(depthLt, "out", and2, "b");
		var and3 = b.Add("and3", "And", 1600, 120);
		b.Connect(and2, "out", and3, "a");
		b.Connect(rng, "out", and3, "b");

		var spawn = b.Add("spawn", "Spawn Rhizome", 1840, 120);
		b.Connect(and3, "out", spawn, "trigger");

		return b.FinishWithActive(and3, "out").Build();
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
			Value = JsonSerializer.SerializeToElement(value),
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
			Value = JsonSerializer.SerializeToElement(values),
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
			Value = JsonSerializer.SerializeToElement(value),
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
			Value = JsonSerializer.SerializeToElement(values),
		};
		if (i < 0) entries.Add(entry);
		else entries[i] = entry;
	}
}
