namespace Agro.BehaviorGraph;

/// <summary>Bootstrap graphs and configuration for Persea americana (TickDefault topology).</summary>
public static class PerseaSpeciesGraphBuilder
{
	public static class PerseaTickConstants
	{
		public const float LeafLength = 0.2f;
		public const float LeafRadius = 0.04f;
		public const float PetioleLength = 0.05f;
		public const float PetioleRadius = 0.007f;
		public const float LeafGrowthTimeHours = 720f;
		public const float Height = 12f;
		public const float WoodGrowthTime = 2400f;
		public const float WoodGrowthTimeVar = 240f;
		public const float LateralsPerNode = 4f;
		/// <summary>45° leaf pitch (SpeciesSettings default); whorl offset uses LateralRoll=0 like TickDefault.</summary>
		public const float LateralPitch = DefaultSpeciesGraphBuilder.DefaultTickConstants.LateralPitch;
		public const float LateralPitchVar = DefaultSpeciesGraphBuilder.DefaultTickConstants.LateralPitchVar;
		public const float LateralRoll = 0f;
		/// <summary>Post-<see cref="SpeciesSettings.Init"/> gravitaxis (0.2×0.4); keeps apical meristems upright.</summary>
		public const float ShootsGravitaxis = DefaultSpeciesGraphBuilder.DefaultTickConstants.ShootsGravitaxis;
		/// <summary>Post-Init apical bend subtractor; same as default / historical <c>1 − 0.02</c> UI serialize.</summary>
		public const float TwigsBendingApical = DefaultSpeciesGraphBuilder.DefaultTickConstants.TwigsBendingApical;
	}

	public static List<BehaviorConfigUploadEntry> BuildConfiguration()
	{
		var entries = DefaultSpeciesGraphBuilder.BuildDefaultConfiguration().ToList();
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafLength, PerseaTickConstants.LeafLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius, PerseaTickConstants.LeafRadius);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength, PerseaTickConstants.PetioleLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius, PerseaTickConstants.PetioleRadius);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.Height, PerseaTickConstants.Height);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafGrowthTime, PerseaTickConstants.LeafGrowthTimeHours);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode, PerseaTickConstants.LateralsPerNode);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleCoverThreshold,
			DefaultSpeciesGraphBuilder.ComputePetioleCoverThreshold(
				DefaultSpeciesGraphBuilder.DefaultTickConstants.LateralPitch,
				PerseaTickConstants.PetioleLength));
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime, PerseaTickConstants.WoodGrowthTime);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTimeVar, PerseaTickConstants.WoodGrowthTimeVar);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LateralPitch, PerseaTickConstants.LateralPitch);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LateralPitchVar, PerseaTickConstants.LateralPitchVar);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LateralRoll, PerseaTickConstants.LateralRoll);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.ShootsGravitaxis, PerseaTickConstants.ShootsGravitaxis);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingApical, PerseaTickConstants.TwigsBendingApical);
		return entries;
	}

	public static IReadOnlyList<(string Name, global::ExportedGraph Graph)> BuildSpeciesSubgraphs() =>
		DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs();

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
}
