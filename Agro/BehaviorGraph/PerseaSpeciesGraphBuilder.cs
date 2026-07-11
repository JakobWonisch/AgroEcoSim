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
	}

	public static List<BehaviorConfigUploadEntry> BuildConfiguration()
	{
		var entries = DefaultSpeciesGraphBuilder.BuildDefaultConfiguration().ToList();
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafLength, PerseaTickConstants.LeafLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius, PerseaTickConstants.LeafRadius);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength, PerseaTickConstants.PetioleLength);
		SetNumber(entries, DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius, PerseaTickConstants.PetioleRadius);
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
