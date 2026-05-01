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
}

public static class PredefinedSpeciesCatalog
{
	static readonly Lazy<List<PredefinedSpeciesEntry>> Lazy = new(Build);

	public static IReadOnlyList<PredefinedSpeciesEntry> All => Lazy.Value;

	static global::ExportedGraph BuildDefaultGatedGraph()
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
			list.Add(new PredefinedSpeciesEntry
			{
				Name = s.Name,
				Aka = s.Aka,
				Graphs =
				[
					new PredefinedSpeciesGraphEntry
					{
						Id = Guid.NewGuid().ToString(),
						Name = "Main",
						Graph = BuildDefaultGatedGraph(),
					},
				],
			});
		}

		return list;
	}
}
