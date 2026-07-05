using System.Text.Json;

namespace Agro.BehaviorGraph;

/// <summary>
/// Bootstrap behavior graphs for the Default species — one subgraph per <see cref="AboveGroundAgent.TickDefault"/> topic.
/// TickDefault topics as separate graphs (Active gate + phase-2 effects where implemented).
/// </summary>
public static class DefaultSpeciesGraphBuilder
{
	/// <summary>Stable configuration ids for Default species bootstrap graphs.</summary>
	public static class ConfigIds
	{
		public const string LeafThickness = "default-config-leaf-thickness";
		public const string PhotoEfficiency = "default-config-photo-efficiency";
		public const string MinIrradiance = "default-config-min-irradiance";
		public const string LeafSurfaceFactor = "default-config-leaf-surface-factor";
		public const string PetioleAgeBudMinHours = "default-config-petiole-age-bud-min-hours";
		public const string PetioleAgeBudReferenceHours = "default-config-petiole-age-bud-reference-hours";
		public const string PetioleUnproductiveMinAgeHours = "default-config-petiole-unproductive-min-age-hours";
		public const string UnproductiveProductionThreshold = "default-config-unproductive-production-threshold";
		public const string PetioleCoverThreshold = "default-config-petiole-cover-threshold";
		public const string MinDominanceForStemDeath = "default-config-min-dominance-for-stem-death";
		public const string EnoughEnergyFactor = "default-config-enough-energy-factor";
		public const string StemDeathProbabilityBase = "default-config-stem-death-probability-base";
		public const string StemDeathHeightCoeff = "default-config-stem-death-height-coeff";
		public const string StemDeathEfficiencyCoeff = "default-config-stem-death-efficiency-coeff";
		public const string StemDeathRadiusCoeff = "default-config-stem-death-radius-coeff";
		public const string LeafLength = "default-config-leaf-length";
		public const string LeafRadius = "default-config-leaf-radius";
		public const string PetioleLength = "default-config-petiole-length";
		public const string PetioleRadius = "default-config-petiole-radius";
		public const string MeristemGrowthLength = "default-config-meristem-growth-length";
		public const string MeristemGrowthRadius = "default-config-meristem-growth-radius";
		public const string StemGrowthRadius = "default-config-stem-growth-radius";
		public const string DominanceFactor = "default-config-dominance-factor";
		public const string AuxinsProduction = "default-config-auxins-production";
		public const string NodeDistance = "default-config-node-distance";
		public const string NodeDistanceVar = "default-config-node-distance-var";
		public const string TwigLateralAngle = "default-config-twig-lateral-angle";
	}

	static float DefaultPetioleCoverThreshold()
	{
		var s = SpeciesSettings.Default;
		return MathF.Cos(MathF.PI * 0.5f - s.LateralPitch) * s.PetioleLength * 0.25f;
	}

	public static IReadOnlyList<BehaviorConfigUploadEntry> BuildDefaultConfiguration() =>
	[
		new()
		{
			Id = ConfigIds.LeafThickness,
			Key = "Leaf thickness",
			Label = "Leaf thickness",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(AboveGroundAgent.LeafThickness),
		},
		new()
		{
			Id = ConfigIds.PhotoEfficiency,
			Key = "Photo efficiency",
			Label = "Photo efficiency",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(AboveGroundAgent.mPhotoEfficiency),
		},
		new()
		{
			Id = ConfigIds.MinIrradiance,
			Key = "Min irradiance",
			Label = "Min irradiance",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(0.01f),
		},
		new()
		{
			Id = ConfigIds.LeafSurfaceFactor,
			Key = "Leaf surface factor",
			Label = "Leaf surface factor",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(2f),
		},
		new()
		{
			Id = ConfigIds.PetioleAgeBudMinHours,
			Key = "Petiole age bud min hours",
			Label = "Petiole age bud min hours",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(36f),
		},
		new()
		{
			Id = ConfigIds.PetioleAgeBudReferenceHours,
			Key = "Petiole age bud reference hours",
			Label = "Petiole age bud reference hours",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(4032f),
		},
		new()
		{
			Id = ConfigIds.PetioleUnproductiveMinAgeHours,
			Key = "Petiole unproductive min age hours",
			Label = "Petiole unproductive min age hours",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(48f),
		},
		new()
		{
			Id = ConfigIds.UnproductiveProductionThreshold,
			Key = "Unproductive production threshold",
			Label = "Unproductive production threshold",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(0.5f),
		},
		new()
		{
			Id = ConfigIds.PetioleCoverThreshold,
			Key = "Petiole cover threshold",
			Label = "Petiole cover threshold",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(DefaultPetioleCoverThreshold()),
		},
		new()
		{
			Id = ConfigIds.MinDominanceForStemDeath,
			Key = "Min dominance for stem death",
			Label = "Min dominance for stem death",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(1f),
		},
		new()
		{
			Id = ConfigIds.EnoughEnergyFactor,
			Key = "Enough energy factor",
			Label = "Enough energy factor",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(320f),
		},
		new()
		{
			Id = ConfigIds.StemDeathProbabilityBase,
			Key = "Stem death probability base",
			Label = "Stem death probability base",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(0.004f),
		},
		new()
		{
			Id = ConfigIds.StemDeathHeightCoeff,
			Key = "Stem death height coeff",
			Label = "Stem death height coeff",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(5f),
		},
		new()
		{
			Id = ConfigIds.StemDeathEfficiencyCoeff,
			Key = "Stem death efficiency coeff",
			Label = "Stem death efficiency coeff",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(4f),
		},
		new()
		{
			Id = ConfigIds.StemDeathRadiusCoeff,
			Key = "Stem death radius coeff",
			Label = "Stem death radius coeff",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(20f),
		},
		new()
		{
			Id = ConfigIds.LeafLength,
			Key = "Leaf length",
			Label = "Leaf length",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.LeafLength),
		},
		new()
		{
			Id = ConfigIds.LeafRadius,
			Key = "Leaf radius",
			Label = "Leaf radius",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.LeafRadius),
		},
		new()
		{
			Id = ConfigIds.PetioleLength,
			Key = "Petiole length",
			Label = "Petiole length",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.PetioleLength),
		},
		new()
		{
			Id = ConfigIds.PetioleRadius,
			Key = "Petiole radius",
			Label = "Petiole radius",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.PetioleRadius),
		},
		new()
		{
			Id = ConfigIds.MeristemGrowthLength,
			Key = "Meristem growth length",
			Label = "Meristem growth length",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(1e-3f),
		},
		new()
		{
			Id = ConfigIds.MeristemGrowthRadius,
			Key = "Meristem growth radius",
			Label = "Meristem growth radius",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(2e-5f),
		},
		new()
		{
			Id = ConfigIds.StemGrowthRadius,
			Key = "Stem growth radius",
			Label = "Stem growth radius",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(2e-5f),
		},
		new()
		{
			Id = ConfigIds.DominanceFactor,
			Key = "Dominance factor",
			Label = "Dominance factor",
			Type = "number",
			// Default species never sets DominanceFactor init; DominanceFactors stays [0.7f] and
			// TickDefault falls back to index 0 for DominanceLevel >= 1.
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.DominanceFactors[0]),
		},
		new()
		{
			Id = ConfigIds.AuxinsProduction,
			Key = "Auxins production",
			Label = "Auxins production",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.AuxinsProduction),
		},
		new()
		{
			Id = ConfigIds.NodeDistance,
			Key = "Node distance",
			Label = "Node distance",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.NodeDistance),
		},
		new()
		{
			Id = ConfigIds.NodeDistanceVar,
			Key = "Node distance var",
			Label = "Node distance var",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(SpeciesSettings.Default.NodeDistanceVar),
		},
		new()
		{
			Id = ConfigIds.TwigLateralAngle,
			Key = "Twig lateral angle",
			Label = "Twig lateral angle",
			Type = "number",
			Value = JsonSerializer.SerializeToElement(MathF.PI * 0.5f),
		},
	];

	public static IReadOnlyList<(string Name, global::ExportedGraph Graph)> BuildDefaultSpeciesSubgraphs() =>
	[
		("Life support", BuildLifeSupportSubgraph()),
		("Photosynthesis", BuildPhotosynthesisSubgraph()),
		("Petiole age bud", BuildPetioleAgeBudSubgraph()),
		("Stem dominance death", BuildStemDominanceDeathSubgraph()),
		("Meristem tick marker", BuildMeristemTickMarkerSubgraph()),
		("Auxin twig", BuildAuxinTwigSubgraph()),
		("Growth leaf", BuildGrowthLeafSubgraph()),
		("Growth petiole", BuildGrowthPetioleSubgraph()),
		("Growth meristem", BuildGrowthMeristemSubgraph()),
		("Growth stem", BuildGrowthStemSubgraph()),
		("Wood lignify", BuildWoodLignifySubgraph()),
		("Meristem chain", BuildMeristemChainSubgraph()),
		("Petiole cover bud", BuildPetioleCoverBudSubgraph()),
		("Petiole unproductive death", BuildPetioleUnproductiveDeathSubgraph()),
		("Energy depletion", BuildEnergyDepletionSubgraph()),
		("Auxins update", BuildAuxinsUpdateSubgraph()),
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
		var sim = b.Add("sim", "Simulation Settings Input", 0, 200);

		var leafThick = b.AddConfig("leaf-thick", ConfigIds.LeafThickness, false, 240, 80,
			"AboveGroundAgent.LeafThickness");
		var c0 = b.AddNum("c0", 0f, 240, 160);

		var lr = b.Add("lr", "Multiply", 480, 100);
		b.Connect(state, "length", lr, "a");
		b.Connect(state, "radius", lr, "b");

		var leafHour = b.Add("leaf-hour", "Multiply", 720, 80);
		b.Connect(lr, "out", leafHour, "a");
		b.Connect(leafThick, "num", leafHour, "b");

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
		b.Connect(sim, "hoursPerTick", perTick, "b");

		var negTick = b.Add("neg-tick", "Subtract", 1680, 100);
		b.Connect(c0, "num", negTick, "a");
		b.Connect(perTick, "out", negTick, "b");

		var dEnergy = b.Add("d-energy", "Delta Energy", 1920, 100);
		b.Connect(negTick, "out", dEnergy, "amount");

		return b.FinishWithActive(always, "bool").Build();
	}

	/// <summary>
	/// TickDefault lines 427–456: leaf photosynthesis when Water_g &gt; 0 and irradiance &gt; 0.01.
	/// </summary>
	public static global::ExportedGraph BuildPhotosynthesisSubgraph()
	{
		var b = SubgraphBuilder.Create("photo");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var ir = b.Add("ir", "Irradiance Input", 0, 120);

		var c0 = b.AddNum("c0", 0f, 280, 0);
		var minIr = b.AddConfig("min-ir", ConfigIds.MinIrradiance, false, 280, 40,
			"Minimum irradiance (W/m²) to run photosynthesis");
		var photoEff = b.AddConfig("photo-eff", ConfigIds.PhotoEfficiency, false, 280, 80,
			"AboveGroundAgent.mPhotoEfficiency");
		var surfaceFactor = b.AddConfig("surface-factor", ConfigIds.LeafSurfaceFactor, false, 280, 120,
			"Leaf surface multiplier: Length * Radius * factor");

		// Active: Organ == Leaf && Water_g > 0 && irradiance > 0.01
		var hasWater = b.Add("has-water", "Greater Than", 520, 0);
		b.Connect(state, "water", hasWater, "a");
		b.Connect(c0, "num", hasWater, "b");

		var bright = b.Add("bright", "Greater Than", 520, 60);
		b.Connect(ir, "irradiance", bright, "a");
		b.Connect(minIr, "num", bright, "b");

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
		b.Connect(surfaceFactor, "num", surface, "b");

		var byLight = b.Add("by-light", "Multiply", 960, 200);
		b.Connect(surface, "out", byLight, "a");
		b.Connect(ir, "irradiance", byLight, "b");

		var lightEff = b.Add("light-eff", "Multiply", 1200, 200);
		b.Connect(byLight, "out", lightEff, "a");
		b.Connect(photoEff, "num", lightEff, "b");

		var lightLeWater = b.Add("light-le-water", "Less Than", 1440, 240);
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

		var accProd = b.Add("acc-prod", "Accumulate Production", 2160, 300);
		b.Connect(prodInv, "out", accProd, "amount");

		var accEnv = b.Add("acc-env", "Accumulate Env Resources", 2160, 360);
		b.Connect(byLight, "out", accEnv, "amount");

		var accEnvInv = b.Add("acc-env-inv", "Accumulate Env Resources Inv", 2160, 420);
		b.Connect(ir, "irradiance", accEnvInv, "amount");

		return b.FinishWithActive(activeCond, "out").Build();
	}

	/// <summary>TickDefault lines 461–467.</summary>
	public static global::ExportedGraph BuildPetioleAgeBudSubgraph()
	{
		var b = SubgraphBuilder.Create("pab");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var minAge = b.AddConfig("min-age", ConfigIds.PetioleAgeBudMinHours, false, 280, 0,
			"Minimum petiole age (hours) before age-based budding");
		var refHours = b.AddConfig("ref-hours", ConfigIds.PetioleAgeBudReferenceHours, false, 280, 40,
			"Reference hours for age-based budding probability");

		var ageOk = b.Add("age-ok", "Greater Than", 520, 60);
		b.Connect(state, "ageHours", ageOk, "a");
		b.Connect(minAge, "num", ageOk, "b");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "petiole", and1, "a");
		b.Connect(ageOk, "out", and1, "b");

		var notParentMeristem = b.Add("not-parent-mer", "Not", 1000, 20);
		b.Connect(form, "parentMeristem", notParentMeristem, "a");

		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(notParentMeristem, "out", and2, "b");

		var ageRatio = b.Add("age-ratio", "Integer Divide", 1240, 80);
		b.Connect(state, "ageHours", ageRatio, "a");
		b.Connect(refHours, "num", ageRatio, "b");

		var pSquared = b.Add("p-sq", "Multiply", 1480, 80);
		b.Connect(ageRatio, "out", pSquared, "a");
		b.Connect(ageRatio, "out", pSquared, "b");

		var rng = b.WireRandomAccumChance(pSquared, "out");

		var makeBud = b.Add("make-bud", "Make Bud", 2160, 200);
		b.Connect(rng, "out", makeBud, "trigger");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault lines 473–486.</summary>
	public static global::ExportedGraph BuildStemDominanceDeathSubgraph()
	{
		var b = SubgraphBuilder.Create("sdd");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var minDom = b.AddConfig("min-dom", ConfigIds.MinDominanceForStemDeath, false, 280, 0,
			"DominanceLevel must exceed this for height-based stem death");

		var domGt1 = b.Add("dom-gt1", "Greater Than", 520, 0);
		b.Connect(state, "dominanceLevel", domGt1, "a");
		b.Connect(minDom, "num", domGt1, "b");

		var parentDomLt = b.Add("parent-dom-lt", "Less Than", 520, 60);
		b.Connect(form, "parentDominance", parentDomLt, "a");
		b.Connect(state, "dominanceLevel", parentDomLt, "b");

		var andDom = b.Add("and-dom", "And", 760, 20);
		b.Connect(domGt1, "out", andDom, "a");
		b.Connect(parentDomLt, "out", andDom, "b");

		var and1 = b.Add("and1", "And", 760, 60);
		b.Connect(organ, "stem", and1, "a");
		b.Connect(andDom, "out", and1, "b");

		var deathP = b.WireStemDeathProbability(state, form);
		var rng = b.WireRandomAccumChance(deathP, "out");

		var c0 = b.AddNum("c0", 0f, 1920, 200);
		var setEnergy = b.Add("set-energy", "Set Energy", 2160, 200);
		b.Connect(c0, "num", setEnergy, "value");
		b.Connect(rng, "out", setEnergy, "trigger");

		return b.FinishWithActive(and1, "out").Build();
	}

	/// <summary>TickDefault line 490 — switch case Meristem (wasMeristem flag).</summary>
	public static global::ExportedGraph BuildMeristemTickMarkerSubgraph()
	{
		var b = SubgraphBuilder.Create("mtm");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var wasTrue = b.AddBool("was-true", true, 1920, 200);
		var setWas = b.Add("set-was", "Set Was Meristem", 2160, 200);
		b.Connect(wasTrue, "bool", setWas, "value");
		return b.FinishWithActive(organ, "meristem").Build();
	}

	/// <summary>TickDefault lines 494–548.</summary>
	public static global::ExportedGraph BuildAuxinTwigSubgraph()
	{
		var b = SubgraphBuilder.Create("atw");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var form = b.Add("form", "Formation Input", 0, 60);
		var state = b.Add("state", "Agent State Input", 0, 120);

		// TickDefault: petiole twig only when parent is not meristem; buds may activate regardless.
		var notParentMeristem = b.Add("gate-not-parent-mer", "Not", 400, 60);
		b.Connect(form, "parentMeristem", notParentMeristem, "a");

		var petioleNotUnderMeristem = b.Add("pet-ok", "And", 640, 20);
		b.Connect(organ, "petiole", petioleNotUnderMeristem, "a");
		b.Connect(notParentMeristem, "out", petioleNotUnderMeristem, "b");

		var twigOrgan = b.Add("twig-organ", "Or", 640, 0);
		b.Connect(organ, "bud", twigOrgan, "a");
		b.Connect(petioleNotUnderMeristem, "out", twigOrgan, "b");

		var enough = b.WireEnoughEnergy(organ, state);
		var and1 = b.Add("and1", "And", 880, 20);
		b.Connect(twigOrgan, "out", and1, "a");
		b.Connect(enough, "out", and1, "b");

		var and2 = b.Add("and2", "And", 1120, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(form, "auxinLocalMinimum", and2, "b");

		b.WireTwigEffectChain(organ, form);

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>
	/// TickDefault lines 558–573 — Partial: production-based growth; size-limit guards deferred.
	/// </summary>
	public static global::ExportedGraph BuildGrowthLeafSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-leaf");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);

		var activeGate = b.WireGrowthActiveGate(organ, state, "leaf");
		var (deltaLen, deltaRad) = b.WireLeafPetioleGrowthDeltas(
			state, form,
			ConfigIds.LeafLength, ConfigIds.LeafRadius,
			capRadiusToParent: false);

		var growth = b.Add("growth", "Growth", 1200, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(activeGate, "out").Build();
	}

	/// <summary>
	/// TickDefault lines 575–592 — production-based growth with parent-radius cap on radius.
	/// </summary>
	public static global::ExportedGraph BuildGrowthPetioleSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-pet");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);

		var activeGate = b.WireGrowthActiveGate(organ, state, "petiole");
		var (deltaLen, deltaRad) = b.WireLeafPetioleGrowthDeltas(
			state, form,
			ConfigIds.PetioleLength, ConfigIds.PetioleRadius,
			capRadiusToParent: true);

		var growth = b.Add("growth", "Growth", 1200, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(activeGate, "out").Build();
	}

	/// <summary>
	/// TickDefault lines 594–606 — Partial: tier-1 meristem growth; dominance index deferred.
	/// </summary>
	public static global::ExportedGraph BuildGrowthMeristemSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-mer");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var sim = b.Add("sim", "Simulation Settings Input", 0, 180);

		var activeGate = b.WireGrowthActiveGate(organ, state, "meristem");
		var (deltaLen, deltaRad) = b.WireMeristemGrowthDeltas(state, form, sim);

		var growth = b.Add("growth", "Growth", 1200, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(activeGate, "out").Build();
	}

	/// <summary>
	/// TickDefault lines 608–618 — Partial: tier-1 stem radius growth.
	/// </summary>
	public static global::ExportedGraph BuildGrowthStemSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-stem");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var sim = b.Add("sim", "Simulation Settings Input", 0, 180);

		var activeGate = b.WireGrowthActiveGate(organ, state, "stem");
		var deltaRad = b.WireStemGrowthDelta(state, form, sim);
		var c0 = b.AddNum("c0", 0f, 1200, 160);

		var growth = b.Add("growth", "Growth", 1200, 200);
		b.Connect(c0, "num", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(activeGate, "out").Build();
	}

	/// <summary>TickDefault lines 623–628: min(wood, parentWood) + GrowthTimeVar, capped at 1.</summary>
	public static global::ExportedGraph BuildWoodLignifySubgraph()
	{
		var b = SubgraphBuilder.Create("wl");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var c1 = b.AddNum("c1", 1f, 280, 0);

		var woodLt1 = b.Add("wood-lt", "Less Than", 520, 60);
		b.Connect(state, "wood", woodLt1, "a");
		b.Connect(c1, "num", woodLt1, "b");

		var enough = b.WireEnoughEnergy(organ, state);
		var and1 = b.Add("and1", "And", 1000, 20);
		b.Connect(organ, "stem", and1, "a");
		b.Connect(woodLt1, "out", and1, "b");

		var and2 = b.Add("and2", "And", 1240, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(enough, "out", and2, "b");

		var baseWood = b.WireMinFloat(state, "wood", form, "parentWood", "base");

		var newWood = b.Add("new-wood", "Add", 1480, 200);
		b.Connect(baseWood, "out", newWood, "a");
		b.Connect(state, "growthTimeVar", newWood, "b");

		var one = b.AddNum("one", 1f, 1720, 240);
		var clamped = b.Add("clamped", "Clamp Max", 1960, 200);
		b.Connect(newWood, "out", clamped, "value");
		b.Connect(one, "num", clamped, "max");

		var setWood = b.Add("set-wood", "Set Wood", 2200, 200);
		b.Connect(clamped, "out", setWood, "value");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault lines 632+ — monopodial meristem chain (dichotomous deferred).</summary>
	public static global::ExportedGraph BuildMeristemChainSubgraph()
	{
		var b = SubgraphBuilder.Create("mc");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);

		var lengthGt = b.Add("len-gt", "Greater Than", 520, 60);
		b.Connect(state, "length", lengthGt, "a");
		b.Connect(state, "lengthVar", lengthGt, "b");

		var notBud = b.Add("not-bud", "Not", 520, 20);
		b.Connect(organ, "bud", notBud, "a");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "meristem", and1, "a");
		b.Connect(lengthGt, "out", and1, "b");

		var enough = b.WireEnoughEnergy(organ, state);
		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(notBud, "out", and2, "b");

		var and3 = b.Add("and3", "And", 1240, 60);
		b.Connect(and2, "out", and3, "a");
		b.Connect(enough, "out", and3, "b");

		var chainTrig = b.AddBool("chain-trig", true, 1480, 200);
		var becomeStem = b.Add("become-stem", "Become Stem", 1720, 160);
		b.Connect(chainTrig, "bool", becomeStem, "trigger");

		var wasTrue = b.AddBool("was-true", true, 1720, 200);
		var setWas = b.Add("set-was", "Set Was Meristem", 1960, 200);
		b.Connect(wasTrue, "bool", setWas, "value");

		var spawn = b.Add("spawn", "Spawn Meristem", 1720, 240);
		b.Connect(chainTrig, "bool", spawn, "trigger");

		var createLeaves = b.Add("create-leaves", "Create Leaves", 1960, 240);
		b.Connect(spawn, "seq", createLeaves, "trigger");
		b.Connect(spawn, "childId", createLeaves, "meristemId");

		return b.FinishWithActive(and3, "out").Build();
	}

	/// <summary>TickDefault lines 686–687.</summary>
	public static global::ExportedGraph BuildPetioleCoverBudSubgraph()
	{
		var b = SubgraphBuilder.Create("pcb");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);

		var enough = b.WireEnoughEnergy(organ, state);
		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "petiole", and1, "a");
		b.Connect(enough, "out", and1, "b");

		var coverSum = b.Add("cover-sum", "Add", 1000, 80);
		b.Connect(state, "parentRadiusAtBirth", coverSum, "a");
		var coverThreshold = b.AddConfig("cover-threshold", ConfigIds.PetioleCoverThreshold, false, 760, 80,
			"species.PetioleCoverThreshold");
		b.Connect(coverThreshold, "num", coverSum, "b");
		var coverLt = b.Add("cover-lt", "Less Than", 1000, 120);
		b.Connect(coverSum, "out", coverLt, "a");
		b.Connect(form, "parentBaseRadius", coverLt, "b");

		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(coverLt, "out", and2, "b");

		var makeBudTrig = b.AddBool("make-bud-trig", true, 1240, 200);
		var makeBud = b.Add("make-bud", "Make Bud", 1480, 200);
		b.Connect(makeBudTrig, "bool", makeBud, "trigger");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault lines 690–706.</summary>
	public static global::ExportedGraph BuildPetioleUnproductiveDeathSubgraph()
	{
		var b = SubgraphBuilder.Create("pud");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);
		var minAge = b.AddConfig("min-age", ConfigIds.PetioleUnproductiveMinAgeHours, false, 280, 0,
			"Minimum petiole age (hours) before unproductive-branch death");
		var prodThreshold = b.AddConfig("prod-threshold", ConfigIds.UnproductiveProductionThreshold, false, 1240, 120,
			"Children production / energyProductionMax below this triggers death roll");

		var ageOk = b.Add("age-ok", "Greater Than", 520, 60);
		b.Connect(state, "ageHours", ageOk, "a");
		b.Connect(minAge, "num", ageOk, "b");

		var and1 = b.Add("and1", "And", 760, 20);
		b.Connect(organ, "petiole", and1, "a");
		b.Connect(ageOk, "out", and1, "b");

		var notParentMeristem = b.Add("not-parent-mer", "Not", 1000, 0);
		b.Connect(form, "parentMeristem", notParentMeristem, "a");

		var prodRatio = b.Add("prod-ratio", "Divide", 1000, 120);
		b.Connect(form, "childrenProductionSum", prodRatio, "a");
		b.Connect(form, "energyProductionMax", prodRatio, "b");

		var prodLow = b.Add("prod-low", "Less Than", 1240, 120);
		b.Connect(prodRatio, "out", prodLow, "a");
		b.Connect(prodThreshold, "num", prodLow, "b");

		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(notParentMeristem, "out", and2, "b");

		var and3 = b.Add("and3", "And", 1240, 60);
		b.Connect(and2, "out", and3, "a");
		b.Connect(form, "hasChildren", and3, "b");

		var and4 = b.Add("and4", "And", 1480, 80);
		b.Connect(and3, "out", and4, "a");
		b.Connect(prodLow, "out", and4, "b");

		var deathP = b.WireUnproductiveDeathProbability(prodRatio, "out");
		var rng = b.WireRandomAccumChance(deathP, "out");

		var death = b.Add("death", "Death", 2440, 200);
		b.Connect(rng, "out", death, "trigger");

		return b.FinishWithActive(and4, "out").Build();
	}

	/// <summary>TickDefault lines 712–726 — Energy &lt;= 0.</summary>
	public static global::ExportedGraph BuildEnergyDepletionSubgraph()
	{
		var b = SubgraphBuilder.Create("ed");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var c0 = b.AddNum("c0", 0f, 280, 0);

		var starved = b.Add("starved", "Less Than or Equal", 520, 0);
		b.Connect(state, "energy", starved, "a");
		b.Connect(c0, "num", starved, "b");

		var makeBud = b.Add("make-bud", "Make Bud", 1000, 160);
		var andPetiole = b.Add("and-pet", "And", 760, 160);
		b.Connect(starved, "out", andPetiole, "a");
		b.Connect(organ, "petiole", andPetiole, "b");
		b.Connect(andPetiole, "out", makeBud, "trigger");

		var deathLeaf = b.Add("death-leaf", "Death", 1000, 200);
		var andLeaf = b.Add("and-leaf", "And", 760, 200);
		b.Connect(starved, "out", andLeaf, "a");
		b.Connect(organ, "leaf", andLeaf, "b");
		b.Connect(andLeaf, "out", deathLeaf, "trigger");

		var deathParent = b.Add("death-parent", "Death Parent", 1240, 200);
		b.Connect(andLeaf, "out", deathParent, "trigger");

		var deathDefault = b.Add("death-default", "Death", 1000, 280);
		var notPetiole = b.Add("not-pet", "Not", 760, 280);
		b.Connect(organ, "petiole", notPetiole, "a");
		var notLeaf = b.Add("not-leaf", "Not", 760, 320);
		b.Connect(organ, "leaf", notLeaf, "a");
		var andOther = b.Add("and-other", "And", 1000, 300);
		b.Connect(starved, "out", andOther, "a");
		b.Connect(notPetiole, "out", andOther, "b");
		var andOther2 = b.Add("and-other2", "And", 1240, 300);
		b.Connect(andOther, "out", andOther2, "a");
		b.Connect(notLeaf, "out", andOther2, "b");
		b.Connect(andOther2, "out", deathDefault, "trigger");

		return b.FinishWithActive(starved, "out").Build();
	}

	/// <summary>TickDefault line 730 — unconditional auxins update.</summary>
	public static global::ExportedGraph BuildAuxinsUpdateSubgraph()
	{
		var b = SubgraphBuilder.Create("aux");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var auxinsProd = b.AddConfig("auxins", ConfigIds.AuxinsProduction, false, 280, 0,
			"SpeciesSettings.Default.AuxinsProduction");
		var c0 = b.AddNum("c0", 0f, 280, 40);

		var meristemOrWas = b.Add("mer-or-was", "Or", 520, 20);
		b.Connect(organ, "meristem", meristemOrWas, "a");
		b.Connect(state, "wasMeristemThisTick", meristemOrWas, "b");

		var auxinsVal = b.Add("aux-val", "If / Else", 760, 40);
		b.Connect(meristemOrWas, "out", auxinsVal, "condition");
		b.Connect(auxinsProd, "num", auxinsVal, "trueValue");
		b.Connect(c0, "num", auxinsVal, "falseValue");

		var setAuxins = b.Add("set-auxins", "Set Auxins", 1000, 40);
		b.Connect(auxinsVal, "out", setAuxins, "value");

		return b.GateAlwaysTrue().Build();
	}

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

		public string AddConfig(string id, string configId, bool isBoolean, float x, float y, string? comment = null) =>
			Add(id, "Configuration Value Input", x, y, GraphNodePayload.FromConfig(configId, isBoolean, comment));

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
			var leafThick = AddConfig("leaf-thick", ConfigIds.LeafThickness, false, 240, 200,
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
			var factor = AddConfig("enough-factor", ConfigIds.EnoughEnergyFactor, false, 240, 280,
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

		/// <summary>Math.Clamp(energy / capacity, 0, 1) — upper clamp only (tier 1).</summary>
		public string WireEnergyReserve(string stateId)
		{
			var ratio = Add("energy-ratio", "Divide", 480, 400);
			Connect(stateId, "energy", ratio, "a");
			Connect(stateId, "energyStorageCapacity", ratio, "b");

			var one = AddNum("one", 1f, 480, 440);
			var reserve = Add("energy-reserve", "Clamp Max", 720, 400);
			Connect(ratio, "out", reserve, "value");
			Connect(one, "num", reserve, "max");
			return reserve;
		}

		public (string deltaLen, string deltaRad) WireLeafPetioleGrowthDeltas(
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

			return (deltaLen, deltaRad);
		}

		public (string deltaLen, string deltaRad) WireMeristemGrowthDeltas(string stateId, string formId, string simId)
		{
			var cfgLen = AddConfig("mer-len", ConfigIds.MeristemGrowthLength, false, 240, 480);
			var cfgRad = AddConfig("mer-rad", ConfigIds.MeristemGrowthRadius, false, 240, 520);
			var dominance = AddConfig("dominance", ConfigIds.DominanceFactor, false, 240, 560);

			var energyReserve = WireEnergyReserve(stateId);
			var waterReserve = WireMinOne(formId, "waterBalance", "water-res");
			var prodRatio = WireProdRatio(stateId, formId);

			string WireAxisDelta(string cfgNodeId, float y, string suffix)
			{
				var m1 = Add($"m1-{suffix}", "Multiply", 720, y);
				Connect(cfgNodeId, "num", m1, "a");
				Connect(dominance, "num", m1, "b");

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
			var deltaRad = WireCapRadiusDeltaToParent(stateId, formId, WireAxisDelta(cfgRad, 520, "rad"), "mer");
			return (deltaLen, deltaRad);
		}

		public string WireStemGrowthDelta(string stateId, string formId, string simId)
		{
			var cfgRad = AddConfig("stem-rad", ConfigIds.StemGrowthRadius, false, 240, 480);
			var dominance = AddConfig("dominance", ConfigIds.DominanceFactor, false, 240, 520);

			var energyReserve = WireEnergyReserve(stateId);
			var waterMinReserve = WireMinFloat(formId, "waterBalance", energyReserve, "out", "stem-water");

			var m1 = Add("m1", "Multiply", 720, 480);
			Connect(cfgRad, "num", m1, "a");
			Connect(dominance, "num", m1, "b");

			var m2 = Add("m2", "Multiply", 960, 480);
			Connect(m1, "out", m2, "a");
			Connect(energyReserve, "out", m2, "b");

			var m3 = Add("m3", "Multiply", 1200, 480);
			Connect(m2, "out", m3, "a");
			Connect(waterMinReserve, "out", m3, "b");

			var deltaRad = Add("delta-r", "Multiply", 1440, 480);
			Connect(m3, "out", deltaRad, "a");
			Connect(simId, "hoursPerTick", deltaRad, "b");
			return WireCapRadiusDeltaToParent(stateId, formId, deltaRad, "stem");
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

		/// <summary>NextFloatAccum(p, hoursPerTick) — returns RNG node id (out socket).</summary>
		public string WireRandomAccumChance(string pNodeId, string pOutput)
		{
			var rng = Add("rng-accum", "Random Accum Chance Input", 1680, 120);
			Connect(pNodeId, pOutput, rng, "p");
			return rng;
		}

		/// <summary>Stem dominance death probability p = base / (q*q).</summary>
		public string WireStemDeathProbability(string stateId, string formId)
		{
			var hCoeff = AddConfig("h-coeff", ConfigIds.StemDeathHeightCoeff, false, 280, 200);
			var eCoeff = AddConfig("e-coeff", ConfigIds.StemDeathEfficiencyCoeff, false, 280, 240);
			var rCoeff = AddConfig("r-coeff", ConfigIds.StemDeathRadiusCoeff, false, 280, 280);
			var baseP = AddConfig("base-p", ConfigIds.StemDeathProbabilityBase, false, 280, 320);

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

			var latAngle = AddConfig("lat-angle", ConfigIds.TwigLateralAngle, false, 2200, 440);
			var setLat = Add("set-lat", "Set Lateral Angle", 2440, 400);
			Connect(become, "seq", setLat, "trigger");
			Connect(latAngle, "num", setLat, "value");

			var one = AddNum("one", 1f, 2440, 480);
			var deltaDom = Add("delta-dom", "Delta Dominance", 2680, 400);
			Connect(setLat, "seq", deltaDom, "trigger");
			Connect(one, "num", deltaDom, "count");

			var turn = Add("turn-up", "Turn Upwards", 2920, 400);
			Connect(deltaDom, "seq", turn, "trigger");

			var nodeDist = AddConfig("node-dist", ConfigIds.NodeDistance, false, 2920, 440);
			var nodeDistVar = AddConfig("node-dist-var", ConfigIds.NodeDistanceVar, false, 2920, 480);
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
}
