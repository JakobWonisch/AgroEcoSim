namespace Agro;

/// <summary>
/// Resolves <see cref="SpeciesSettings"/> for a plant from legacy request fields and/or <see cref="ExportedGraph"/>.
/// Graph interpretation is a stub until a full node evaluator exists.
/// </summary>
public static class SpeciesFromGraph
{
    public static SpeciesSettings Resolve(string? speciesName, SimulationRequest settings)
    {
        if (!string.IsNullOrEmpty(speciesName))
        {
            var legacy = settings.Species?.FirstOrDefault(x => x.Name == speciesName);
            if (legacy != null)
                return legacy;

            if (settings.SpeciesGraphs != null
                && settings.SpeciesGraphs.TryGetValue(speciesName, out var graph)
                && graph?.Nodes is { Count: > 0 })
                return FromExportedGraph(speciesName, graph);

            var predefined = SpeciesSettings.Predefined.FirstOrDefault(x => x.Name == speciesName);
            if (predefined != null)
                return predefined;
        }

        return SpeciesSettings.Default;
    }

    static SpeciesSettings FromExportedGraph(string speciesName, global::ExportedGraph graph)
    {
        _ = graph;
        var predefined = SpeciesSettings.Predefined.FirstOrDefault(x => x.Name == speciesName);
        return predefined ?? SpeciesSettings.Default;
    }
}
