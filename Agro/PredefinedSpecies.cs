using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agro;

/// <summary>Bootstrap graph row for GET /Simulation/species (camelCase JSON).</summary>
public sealed class PredefinedSpeciesGraphEntry
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("graph")]
	public required global::ExportedGraph Graph { get; init; }
}

/// <summary>Bootstrap entry for GET /Simulation/species: display name plus template behavior graphs.</summary>
public sealed class PredefinedSpeciesEntry
{
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("aka")]
	public string? Aka { get; init; }

	[JsonPropertyName("graphs")]
	public required List<PredefinedSpeciesGraphEntry> Graphs { get; init; }

	[JsonPropertyName("configuration")]
	public List<BehaviorConfigUploadEntry>? Configuration { get; init; }
}

public static class PredefinedSpeciesCatalog
{
	static readonly Lazy<List<PredefinedSpeciesEntry>> Lazy = new(Build);

	public static IReadOnlyList<PredefinedSpeciesEntry> All => Lazy.Value;

	static List<PredefinedSpeciesGraphEntry> BuildGraphEntries(
		IReadOnlyList<(string Name, global::ExportedGraph Graph)> subgraphs)
	{
		var entries = new List<PredefinedSpeciesGraphEntry>();
		foreach (var (name, graph) in subgraphs)
		{
			entries.Add(new PredefinedSpeciesGraphEntry
			{
				Id = Guid.NewGuid().ToString(),
				Name = name,
				Graph = graph,
			});
		}

		return entries;
	}

	static List<PredefinedSpeciesGraphEntry> BuildDefaultSpeciesGraphEntries() =>
		BuildGraphEntries(BehaviorGraph.DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs());

	static List<PredefinedSpeciesGraphEntry> BuildPerseaGraphEntries() =>
		BuildGraphEntries(BehaviorGraph.PerseaSpeciesGraphBuilder.BuildSpeciesSubgraphs());

	static List<PredefinedSpeciesGraphEntry> BuildBerganiaGraphEntries(
		BehaviorGraph.BerganiaTickGraphBuilder.BerganiaGraphOptions options) =>
		BuildGraphEntries(BehaviorGraph.BerganiaTickGraphBuilder.BuildSpeciesSubgraphs(options));

	static global::ExportedGraph BuildMinimalGatedGraph()
	{
		var boolId = Guid.NewGuid().ToString();
		var activeId = Guid.NewGuid().ToString();
		var connId = Guid.NewGuid().ToString();
		var boolData = JsonSerializer.SerializeToElement(new Dictionary<string, bool> { ["bool"] = true });
		var emptyData = JsonSerializer.SerializeToElement(new Dictionary<string, object>());
		return new global::ExportedGraph
		{
			Nodes =
			[
				new global::GraphNode
				{
					Id = boolId,
					Label = "Boolean Input",
					Data = boolData,
					Position = new global::NodePosition { X = 0, Y = 0 },
				},
				new global::GraphNode
				{
					Id = activeId,
					Label = "Active",
					Data = emptyData,
					Position = new global::NodePosition { X = 220, Y = 0 },
				},
			],
			Connections =
			[
				new global::GraphConnection
				{
					Id = connId,
					Source = boolId,
					SourceOutput = "bool",
					Target = activeId,
					TargetInput = "isActive",
				},
			],
		};
	}

	static List<PredefinedSpeciesEntry> Build()
	{
		var list = new List<PredefinedSpeciesEntry>();
		foreach (var s in SpeciesSettings.Predefined)
		{
			var (graphs, configuration) = s.Name switch
			{
				"Default" => (
					BuildDefaultSpeciesGraphEntries(),
					(List<BehaviorConfigUploadEntry>?)[.. BehaviorGraph.DefaultSpeciesGraphBuilder.BuildDefaultConfiguration()]),
				"Persea americana" => (
					BuildPerseaGraphEntries(),
					(List<BehaviorConfigUploadEntry>?)[.. BehaviorGraph.PerseaSpeciesGraphBuilder.BuildConfiguration()]),
				"Geranium Macrorrhizum" => (
					BuildBerganiaGraphEntries(BehaviorGraph.BerganiaTickGraphBuilder.BerganiaGraphOptions.GeraniumMacrorrhizum),
					(List<BehaviorConfigUploadEntry>?)[.. BehaviorGraph.BerganiaTickGraphBuilder.BuildConfiguration(
						BehaviorGraph.BerganiaTickGraphBuilder.BerganiaGraphOptions.GeraniumMacrorrhizum)]),
				"Geranium × Cantabrigiense" => (
					BuildBerganiaGraphEntries(BehaviorGraph.BerganiaTickGraphBuilder.BerganiaGraphOptions.GeraniumCantabrigiense),
					(List<BehaviorConfigUploadEntry>?)[.. BehaviorGraph.BerganiaTickGraphBuilder.BuildConfiguration(
						BehaviorGraph.BerganiaTickGraphBuilder.BerganiaGraphOptions.GeraniumCantabrigiense)]),
				"Bergenia Cordifolia" => (
					BuildBerganiaGraphEntries(BehaviorGraph.BerganiaTickGraphBuilder.BerganiaGraphOptions.BergeniaCordifolia),
					(List<BehaviorConfigUploadEntry>?)[.. BehaviorGraph.BerganiaTickGraphBuilder.BuildConfiguration(
						BehaviorGraph.BerganiaTickGraphBuilder.BerganiaGraphOptions.BergeniaCordifolia)]),
				_ => (
					[
						new PredefinedSpeciesGraphEntry
						{
							Id = Guid.NewGuid().ToString(),
							Name = "Main",
							Graph = BuildMinimalGatedGraph(),
						},
					],
					(List<BehaviorConfigUploadEntry>?)null),
			};

			list.Add(new PredefinedSpeciesEntry
			{
				Name = s.Name,
				Aka = s.Aka,
				Graphs = graphs,
				Configuration = configuration,
			});
		}

		return list;
	}
}
