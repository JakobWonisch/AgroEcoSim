using System.Text.Json.Serialization;

namespace Agro;

/// <summary>Bootstrap entry for GET /Simulation/species: display name plus template behavior graph.</summary>
public sealed class PredefinedSpeciesEntry
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("aka")]
    public string? Aka { get; init; }

    [JsonPropertyName("graph")]
    public required global::ExportedGraph Graph { get; init; }
}

public static class PredefinedSpeciesCatalog
{
    static readonly Lazy<List<PredefinedSpeciesEntry>> Lazy = new(Build);

    public static IReadOnlyList<PredefinedSpeciesEntry> All => Lazy.Value;

    static List<PredefinedSpeciesEntry> Build()
    {
        var list = new List<PredefinedSpeciesEntry>();
        foreach (var s in SpeciesSettings.Predefined)
        {
            list.Add(new PredefinedSpeciesEntry
            {
                Name = s.Name,
                Aka = s.Aka,
                Graph = new global::ExportedGraph
                {
                    Nodes = [],
                    Connections = []
                }
            });
        }

        return list;
    }
}
