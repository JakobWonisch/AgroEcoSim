namespace Agro.BehaviorGraph;

/// <summary>
/// Bootstrap behavior graphs for the Default species â€” one subgraph per <see cref="AboveGroundAgent.TickDefault"/> topic.
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
		public const string DominanceFactors = "default-config-dominance-factors";
		public const string AuxinsProduction = "default-config-auxins-production";
		public const string NodeDistance = "default-config-node-distance";
		public const string NodeDistanceVar = "default-config-node-distance-var";
		public const string TwigLateralAngle = "default-config-twig-lateral-angle";
		public const string MonopodialFactor = "default-config-monopodial-factor";
		public const string LateralRoll = "default-config-lateral-roll";
		public const string LateralRollVar = "default-config-lateral-roll-var";
		public const string LateralPitch = "default-config-lateral-pitch";
		public const string LateralPitchVar = "default-config-lateral-pitch-var";
		public const string LeafPitch = "default-config-leaf-pitch";
		public const string AuxinsThreshold = "default-config-auxins-threshold";
		public const string WoodGrowthTime = "default-config-wood-growth-time";
		public const string WoodGrowthTimeVar = "default-config-wood-growth-time-var";
		public const string LateralsPerNode = "default-config-laterals-per-node";
		public const string TwigsBending = "default-config-twig-bending";
		public const string TwigsBendingLevel = "default-config-twig-bending-level";
		public const string TwigsBendingApical = "default-config-twig-bending-apical";
		public const string ShootsGravitaxis = "default-config-shoots-gravitaxis";
		public const string RizomeLength = "default-config-rizome-length";
		public const string RizomeRadius = "default-config-rizome-radius";
		public const string FloweringStartAgeHours = "default-config-flowering-start-age-hours";
		public const string FloweringEndAgeHours = "default-config-flowering-end-age-hours";

		// TODO(config): Add AuxinsReach â€” Usage: "Auxines propagate this far within the plant with a linear falloff."
		// TODO(config): Add MaxLeafLevel â€” Usage: "Limits the level of branches that support petioles. Technically it corresponds to the maximum possible level of descendants."
	}

	/// <summary>TickDefault reference literals for bootstrap configuration (not <see cref="SpeciesSettings"/>).</summary>
	public static class DefaultTickConstants
	{
		public const float LeafLength = 0.12f;
		public const float LeafRadius = 0.04f;
		public const float PetioleLength = 0.04f;
		public const float PetioleRadius = 0.0025f;
		public const float DominanceFactor = 0.7f;
		public const float AuxinsProduction = 40f;
		public const float NodeDistance = 0.04f;
		public const float NodeDistanceVar = 0.01f;
		public const float MonopodialFactor = 1f;
		public const float LateralRoll = 0f;
		public const float LateralRollVar = 5f * MathF.PI / 180f;
		public const float LateralPitch = 45f * MathF.PI / 180f;
		public const float LateralPitchVar = 5f * MathF.PI / 180f;
		public const float LeafPitch = 20f * MathF.PI / 180f;
		public const float AuxinsThreshold = 1f;
		public const float WoodGrowthTime = 100f;
		public const float WoodGrowthTimeVar = 10f;
		public const int LateralsPerNode = 2;
		public const float TwigsBending = 0.5f;
		public const float TwigsBendingLevel = 1f;
		public const float TwigsBendingApical = 0.02f;
		/// <summary>Effective value after <see cref="SpeciesSettings.Init"/> (0.2 Ã— 0.4).</summary>
		public const float ShootsGravitaxis = 0.08f;
		public const float RizomeLength = 0.01f;
		public const float RizomeRadius = 0.0025f;
		public const float FloweringStartAgeHours = 24f * 45f;
		public const float FloweringEndAgeHours = 24f * 90f;

		public static float PetioleCoverThreshold =>
			MathF.Cos(MathF.PI * 0.5f - LateralPitch) * PetioleLength * 0.25f;
	}

	static float DefaultPetioleCoverThreshold() => DefaultTickConstants.PetioleCoverThreshold;

	/// <summary>Matches legacy <see cref="SpeciesSettings.DominanceFactor"/> setter table.</summary>
	public static float[] BuildDominanceFactorsTable(float baseFactor, int length = 17)
	{
		var arr = new float[length];
		arr[0] = 1f;
		arr[1] = 1f;
		arr[2] = baseFactor;
		const int factors = 16;
		for (var i = 3; i < factors && i < length; ++i)
			arr[i] = MathF.Pow(baseFactor, i);
		return arr;
	}

	public static IReadOnlyList<BehaviorConfigUploadEntry> BuildDefaultConfiguration() =>
	[
		new()
		{
			Id = ConfigIds.LeafThickness,
			Key = "Leaf thickness",
			Label = "Leaf thickness",
			Type = "number",
			Value = BehaviorGraphJson.Number(AboveGroundAgent.LeafThickness),
		},
		new()
		{
			Id = ConfigIds.PhotoEfficiency,
			Key = "Photo efficiency",
			Label = "Photo efficiency",
			Type = "number",
			Value = BehaviorGraphJson.Number(AboveGroundAgent.mPhotoEfficiency),
		},
		new()
		{
			Id = ConfigIds.MinIrradiance,
			Key = "Min irradiance",
			Label = "Min irradiance",
			Type = "number",
			Value = BehaviorGraphJson.Number(0.01f),
		},
		new()
		{
			Id = ConfigIds.LeafSurfaceFactor,
			Key = "Leaf surface factor",
			Label = "Leaf surface factor",
			Type = "number",
			Value = BehaviorGraphJson.Number(2f),
		},
		new()
		{
			Id = ConfigIds.PetioleAgeBudMinHours,
			Key = "Petiole age bud min hours",
			Label = "Petiole age bud min hours",
			Type = "number",
			Value = BehaviorGraphJson.Number(36f),
		},
		new()
		{
			Id = ConfigIds.PetioleAgeBudReferenceHours,
			Key = "Petiole age bud reference hours",
			Label = "Petiole age bud reference hours",
			Type = "number",
			Value = BehaviorGraphJson.Number(4032f),
		},
		new()
		{
			Id = ConfigIds.PetioleUnproductiveMinAgeHours,
			Key = "Petiole unproductive min age hours",
			Label = "Petiole unproductive min age hours",
			Type = "number",
			Value = BehaviorGraphJson.Number(48f),
		},
		new()
		{
			Id = ConfigIds.UnproductiveProductionThreshold,
			Key = "Unproductive production threshold",
			Label = "Unproductive production threshold",
			Type = "number",
			Value = BehaviorGraphJson.Number(0.5f),
		},
		new()
		{
			Id = ConfigIds.PetioleCoverThreshold,
			Key = "Petiole cover threshold",
			Label = "Petiole cover threshold",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultPetioleCoverThreshold()),
		},
		new()
		{
			Id = ConfigIds.MinDominanceForStemDeath,
			Key = "Min dominance for stem death",
			Label = "Min dominance for stem death",
			Type = "number",
			Value = BehaviorGraphJson.Number(1f),
		},
		new()
		{
			Id = ConfigIds.EnoughEnergyFactor,
			Key = "Enough energy factor",
			Label = "Enough energy factor",
			Type = "number",
			Value = BehaviorGraphJson.Number(320f),
		},
		new()
		{
			Id = ConfigIds.StemDeathProbabilityBase,
			Key = "Stem death probability base",
			Label = "Stem death probability base",
			Type = "number",
			Value = BehaviorGraphJson.Number(0.004f),
		},
		new()
		{
			Id = ConfigIds.StemDeathHeightCoeff,
			Key = "Stem death height coeff",
			Label = "Stem death height coeff",
			Type = "number",
			Value = BehaviorGraphJson.Number(5f),
		},
		new()
		{
			Id = ConfigIds.StemDeathEfficiencyCoeff,
			Key = "Stem death efficiency coeff",
			Label = "Stem death efficiency coeff",
			Type = "number",
			Value = BehaviorGraphJson.Number(4f),
		},
		new()
		{
			Id = ConfigIds.StemDeathRadiusCoeff,
			Key = "Stem death radius coeff",
			Label = "Stem death radius coeff",
			Type = "number",
			Value = BehaviorGraphJson.Number(20f),
		},
		new()
		{
			Id = ConfigIds.LeafLength,
			Key = "Leaf length",
			Label = "Leaf length",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.LeafLength),
		},
		new()
		{
			Id = ConfigIds.LeafRadius,
			Key = "Leaf radius",
			Label = "Leaf radius",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.LeafRadius),
		},
		new()
		{
			Id = ConfigIds.PetioleLength,
			Key = "Petiole length",
			Label = "Petiole length",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.PetioleLength),
		},
		new()
		{
			Id = ConfigIds.PetioleRadius,
			Key = "Petiole radius",
			Label = "Petiole radius",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.PetioleRadius),
		},
		new()
		{
			Id = ConfigIds.MeristemGrowthLength,
			Key = "Meristem growth length",
			Label = "Meristem growth length",
			Type = "number",
			Value = BehaviorGraphJson.Number(1e-3f),
		},
		new()
		{
			Id = ConfigIds.MeristemGrowthRadius,
			Key = "Meristem growth radius",
			Label = "Meristem growth radius",
			Type = "number",
			Value = BehaviorGraphJson.Number(2e-5f),
		},
		new()
		{
			Id = ConfigIds.StemGrowthRadius,
			Key = "Stem growth radius",
			Label = "Stem growth radius",
			Type = "number",
			Value = BehaviorGraphJson.Number(2e-5f),
		},
		new()
		{
			Id = ConfigIds.DominanceFactor,
			Key = "Dominance factor",
			Label = "Dominance factor",
			Usage = "Reduces the growth of lateral branches. Multiplies with each recursion level.",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.DominanceFactor),
		},
		new()
		{
			Id = ConfigIds.DominanceFactors,
			Key = "Dominance factors",
			Label = "Dominance factors",
			Usage = "Per-level growth multiplier indexed by dominance level (legacy DominanceFactors table).",
			Type = "number[]",
			Value = BehaviorGraphJson.NumberArray(BuildDominanceFactorsTable(DefaultTickConstants.DominanceFactor)),
		},
		new()
		{
			Id = ConfigIds.AuxinsProduction,
			Key = "Auxins production",
			Label = "Auxins production",
			Usage = "Each meristem node generates this amount of auxins (given in unspecified units).",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.AuxinsProduction),
		},
		new()
		{
			Id = ConfigIds.NodeDistance,
			Key = "Node distance",
			Label = "Node distance",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.NodeDistance),
		},
		new()
		{
			Id = ConfigIds.NodeDistanceVar,
			Key = "Node distance var",
			Label = "Node distance var",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.NodeDistanceVar),
		},
		new()
		{
			Id = ConfigIds.TwigLateralAngle,
			Key = "Twig lateral angle",
			Label = "Twig lateral angle",
			Type = "number",
			Value = BehaviorGraphJson.Number(MathF.PI * 0.5f),
		},
		new()
		{
			Id = ConfigIds.MonopodialFactor,
			Key = "Monopodial factor",
			Label = "Monopodial factor",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.MonopodialFactor),
		},
		new()
		{
			Id = ConfigIds.LateralRoll,
			Key = "Lateral roll",
			Label = "Lateral roll",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.LateralRoll),
		},
		new()
		{
			Id = ConfigIds.LateralRollVar,
			Key = "Lateral roll var",
			Label = "Lateral roll var",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.LateralRollVar),
		},
		new()
		{
			Id = ConfigIds.LateralPitch,
			Key = "Lateral pitch",
			Label = "Lateral pitch",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.LateralPitch),
		},
		new()
		{
			Id = ConfigIds.LateralPitchVar,
			Key = "Lateral pitch var",
			Label = "Lateral pitch var",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.LateralPitchVar),
		},
		new()
		{
			Id = ConfigIds.LeafPitch,
			Key = "Leaf pitch",
			Label = "Leaf pitch",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.LeafPitch),
		},
		new()
		{
			Id = ConfigIds.AuxinsThreshold,
			Key = "Auxins threshold",
			Label = "Auxins threshold",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.AuxinsThreshold),
		},
		new()
		{
			Id = ConfigIds.WoodGrowthTime,
			Key = "Wood growth time",
			Label = "Wood growth time (hours)",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.WoodGrowthTime),
		},
		new()
		{
			Id = ConfigIds.WoodGrowthTimeVar,
			Key = "Wood growth time var",
			Label = "Wood growth time var (hours)",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.WoodGrowthTimeVar),
		},
		new()
		{
			Id = ConfigIds.LateralsPerNode,
			Key = "Laterals per node",
			Label = "Laterals per node",
			Type = "number",
			Value = BehaviorGraphJson.Number((float)DefaultTickConstants.LateralsPerNode),
		},
		new()
		{
			Id = ConfigIds.TwigsBending,
			Key = "Twig bending",
			Label = "Twig bending",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.TwigsBending),
		},
		new()
		{
			Id = ConfigIds.TwigsBendingLevel,
			Key = "Twig bending level",
			Label = "Twig bending level",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.TwigsBendingLevel),
		},
		new()
		{
			Id = ConfigIds.TwigsBendingApical,
			Key = "Twig bending apical",
			Label = "Twig bending apical",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.TwigsBendingApical),
		},
		new()
		{
			Id = ConfigIds.ShootsGravitaxis,
			Key = "Shoot gravitaxis",
			Label = "Shoot gravitaxis",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.ShootsGravitaxis),
		},
		new()
		{
			Id = ConfigIds.RizomeLength,
			Key = "Rhizome length",
			Label = "Rhizome length",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.RizomeLength),
		},
		new()
		{
			Id = ConfigIds.RizomeRadius,
			Key = "Rhizome radius",
			Label = "Rhizome radius",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.RizomeRadius),
		},
		new()
		{
			Id = ConfigIds.FloweringStartAgeHours,
			Key = "Flowering start age (hours)",
			Label = "Flowering start age (hours)",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.FloweringStartAgeHours),
		},
		new()
		{
			Id = ConfigIds.FloweringEndAgeHours,
			Key = "Flowering end age (hours)",
			Label = "Flowering end age (hours)",
			Type = "number",
			Value = BehaviorGraphJson.Number(DefaultTickConstants.FloweringEndAgeHours),
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
	/// TickDefault lines 417â€“420: Energy -= LifeSupportPerTick.
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
	/// TickDefault lines 427â€“456: leaf photosynthesis when Water_g &gt; 0 and irradiance &gt; 0.01.
	/// </summary>
	public static global::ExportedGraph BuildPhotosynthesisSubgraph()
	{
		var b = SubgraphBuilder.Create("photo");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var ir = b.Add("ir", "Irradiance Input", 0, 120);

		var c0 = b.AddNum("c0", 0f, 280, 0);
		var minIr = b.AddConfig("min-ir", ConfigIds.MinIrradiance, false, 280, 40,
			"Minimum irradiance (W/mÂ²) to run photosynthesis");
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

		// Phase 2 â€” photosynthesizedEnergy = min(surface * irradiance * mPhotoEfficiency, Water_g)
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

	/// <summary>TickDefault lines 461â€“467.</summary>
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

	/// <summary>TickDefault lines 473â€“486.</summary>
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

	/// <summary>TickDefault line 490 â€” switch case Meristem (wasMeristem flag).</summary>
	public static global::ExportedGraph BuildMeristemTickMarkerSubgraph()
	{
		var b = SubgraphBuilder.Create("mtm");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var wasTrue = b.AddBool("was-true", true, 1920, 200);
		var setWas = b.Add("set-was", "Set Was Meristem", 2160, 200);
		b.Connect(wasTrue, "bool", setWas, "value");
		return b.FinishWithActive(organ, "meristem").Build();
	}

	/// <summary>TickDefault lines 494â€“548.</summary>
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
	/// TickDefault lines 558â€“573 â€” Partial: production-based growth; size-limit guards deferred.
	/// </summary>
	public static global::ExportedGraph BuildGrowthLeafSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-leaf");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);

		var activeGate = b.WireGrowthActiveGate(organ, state, "leaf");
		var (deltaLen, deltaRad, sizeLimitL, sizeLimitR) = b.WireLeafPetioleGrowthDeltas(
			state, form,
			ConfigIds.LeafLength, ConfigIds.LeafRadius,
			capRadiusToParent: false);
		var underLimit = b.WireUnderSizeLimit(state, sizeLimitL, sizeLimitR);
		var growthGate = b.Add("growth-gate", "And", 1000, 40);
		b.Connect(activeGate, "out", growthGate, "a");
		b.Connect(underLimit, "out", growthGate, "b");

		var growth = b.Add("growth", "Growth", 1200, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(growthGate, "out").Build();
	}

	/// <summary>
	/// TickDefault lines 575â€“592 â€” production-based growth with parent-radius cap on radius.
	/// </summary>
	public static global::ExportedGraph BuildGrowthPetioleSubgraph()
	{
		var b = SubgraphBuilder.Create("gr-pet");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var form = b.Add("form", "Formation Input", 0, 120);

		var activeGate = b.WireGrowthActiveGate(organ, state, "petiole");
		var (deltaLen, deltaRad, sizeLimitL, sizeLimitR) = b.WireLeafPetioleGrowthDeltas(
			state, form,
			ConfigIds.PetioleLength, ConfigIds.PetioleRadius,
			capRadiusToParent: true);
		var underLimit = b.WireUnderSizeLimit(state, sizeLimitL, sizeLimitR);
		var growthGate = b.Add("growth-gate", "And", 1000, 40);
		b.Connect(activeGate, "out", growthGate, "a");
		b.Connect(underLimit, "out", growthGate, "b");

		var growth = b.Add("growth", "Growth", 1200, 200);
		b.Connect(deltaLen, "out", growth, "Length");
		b.Connect(deltaRad, "out", growth, "Radius");

		return b.FinishWithActive(growthGate, "out").Build();
	}

	/// <summary>
	/// TickDefault lines 594â€“606 â€” meristem growth with indexed dominance lookup.
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

	/// <summary>TickDefault lines 608â€“618 â€” stem radius growth with indexed dominance lookup.</summary>
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

	/// <summary>TickDefault lines 623â€“628: min(wood, parentWood) + GrowthTimeVar, capped at 1.</summary>
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

	/// <summary>TickDefault lines 632â€“681 â€” monopodial and dichotomous meristem chain.</summary>
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

		var chainTrig = and3;

		var becomeStem = b.Add("become-stem", "Become Stem", 1720, 120);
		b.Connect(chainTrig, "out", becomeStem, "trigger");

		var wasTrue = b.AddBool("was-val", true, 1720, 160);
		var setWas = b.Add("set-was", "Set Was Meristem", 1960, 160);
		b.Connect(wasTrue, "bool", setWas, "value");

		var monoFactor = b.AddConfig("mono-factor", ConfigIds.MonopodialFactor, false, 1480, 200,
			"1 = monopodial meristem chain, &lt;1 = dichotomous");
		var one = b.AddNum("one", 1f, 1480, 240);
		var isMono = b.Add("is-mono", "Greater Than or Equal", 1720, 220);
		b.Connect(monoFactor, "num", isMono, "a");
		b.Connect(one, "num", isMono, "b");

		var domMax = b.AddNum("dom-max", 255f, 1480, 280);
		var domLt = b.Add("dom-lt", "Less Than", 1720, 280);
		b.Connect(state, "dominanceLevel", domLt, "a");
		b.Connect(domMax, "num", domLt, "b");

		var notMono = b.Add("not-mono", "Not", 1960, 220);
		b.Connect(isMono, "out", notMono, "a");

		var dichoGate = b.Add("dicho-gate", "And", 2200, 260);
		b.Connect(notMono, "out", dichoGate, "a");
		b.Connect(domLt, "out", dichoGate, "b");

		var monoTrig = b.Add("mono-trig", "And", 2200, 200);
		b.Connect(isMono, "out", monoTrig, "a");
		b.Connect(chainTrig, "out", monoTrig, "b");

		var dichoTrig = b.Add("dicho-trig", "And", 2440, 260);
		b.Connect(dichoGate, "out", dichoTrig, "a");
		b.Connect(chainTrig, "out", dichoTrig, "b");

		var spawnMono = b.Add("spawn-mono", "Spawn Meristem", 2440, 200);
		b.Connect(monoTrig, "out", spawnMono, "trigger");

		var leavesMono = b.Add("leaves-mono", "Create Leaves", 2680, 200);
		b.Connect(spawnMono, "seq", leavesMono, "trigger");
		b.Connect(spawnMono, "childId", leavesMono, "meristemId");

		var spawnDicho = b.Add("spawn-dicho", "Spawn Dichotomous Meristems", 2680, 280);
		b.Connect(dichoTrig, "out", spawnDicho, "trigger");

		var leavesDicho1 = b.Add("leaves-d1", "Create Leaves", 2920, 280);
		b.Connect(spawnDicho, "seq", leavesDicho1, "trigger");
		b.Connect(spawnDicho, "childId1", leavesDicho1, "meristemId");
		b.Connect(spawnDicho, "lateralPitch", leavesDicho1, "lateralAngle");

		var leavesDicho2 = b.Add("leaves-d2", "Create Leaves", 3160, 280);
		b.Connect(leavesDicho1, "seq", leavesDicho2, "trigger");
		b.Connect(spawnDicho, "childId2", leavesDicho2, "meristemId");
		b.Connect(spawnDicho, "lateralPitch", leavesDicho2, "lateralAngle");

		return b.FinishWithActive(and3, "out").Build();
	}

	/// <summary>TickDefault lines 686â€“687.</summary>
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
			"Petiole cover threshold (TickDefault derived)");
		b.Connect(coverThreshold, "num", coverSum, "b");
		var coverLt = b.Add("cover-lt", "Less Than", 1000, 120);
		b.Connect(coverSum, "out", coverLt, "a");
		b.Connect(form, "parentBaseRadius", coverLt, "b");

		var and2 = b.Add("and2", "And", 1000, 40);
		b.Connect(and1, "out", and2, "a");
		b.Connect(coverLt, "out", and2, "b");

		var makeBud = b.Add("make-bud", "Make Bud", 1480, 200);
		b.Connect(and2, "out", makeBud, "trigger");

		return b.FinishWithActive(and2, "out").Build();
	}

	/// <summary>TickDefault lines 690â€“706.</summary>
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

	/// <summary>TickDefault lines 712â€“726 â€” Energy &lt;= 0.</summary>
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

	/// <summary>TickDefault line 730 â€” unconditional auxins update.</summary>
	public static global::ExportedGraph BuildAuxinsUpdateSubgraph()
	{
		var b = SubgraphBuilder.Create("aux");
		var organ = b.Add("organ", "Agent Type Input", 0, 0);
		var state = b.Add("state", "Agent State Input", 0, 60);
		var auxinsProd = b.AddConfig("auxins", ConfigIds.AuxinsProduction, false, 280, 0,
			"Auxins production for meristem/stem agents");
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
}
