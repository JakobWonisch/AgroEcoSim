using Agro.BehaviorGraph;

namespace Agro;

/// <summary>
/// Morphology from named templates plus optional compiled above-ground behavior graphs (ordered).
/// </summary>
public sealed class PlantSpeciesProfile
{
	public required SpeciesSettings Morphology { get; init; }

	public required IReadOnlyList<CompiledBehaviorGraph> BehaviorGraphs { get; init; }

	public static PlantSpeciesProfile Resolve(string? speciesName, SimulationRequest? settings)
	{
		var morph = SpeciesMorphology.Resolve(speciesName, settings);
		var graphs = new List<CompiledBehaviorGraph>();
		if (!string.IsNullOrEmpty(speciesName) && settings?.SpeciesGraphs != null)
		{
			List<SpeciesGraphUploadEntry>? entries = null;
			var graphKey = speciesName;
			if (!settings.SpeciesGraphs.TryGetValue(speciesName, out entries))
			{
				var matches = settings.SpeciesGraphs.Keys
					.Where(k => string.Equals(k, speciesName, StringComparison.OrdinalIgnoreCase))
					.ToArray();
				if (matches.Length == 1)
				{
					graphKey = matches[0];
					entries = settings.SpeciesGraphs[graphKey];
					Console.Error.WriteLine($"[BehaviorGraph] Using case-insensitive graph key match for species '{speciesName}': '{graphKey}'.");
				}
				else if (matches.Length > 1)
				{
					Console.Error.WriteLine($"[BehaviorGraph] Multiple case-insensitive graph keys match species '{speciesName}': [{string.Join(", ", matches)}]. Using '{matches[0]}'.");
					graphKey = matches[0];
					entries = settings.SpeciesGraphs[graphKey];
				}
				else
				{
					var keys = string.Join(", ", settings.SpeciesGraphs.Keys);
					Console.Error.WriteLine($"[BehaviorGraph] No graph key found for species '{speciesName}'. Available SpeciesGraphs keys: [{keys}]");
				}
			}

			if (entries != null)
			{
				foreach (var entry in entries)
				{
					var exported = entry.Graph;
					if (exported.Nodes is not { Count: > 0 })
					{
						Console.Error.WriteLine($"[BehaviorGraph] Skipping empty graph '{entry.Name}' (id '{entry.Id}') for species '{graphKey}'.");
						continue;
					}

					if (BehaviorGraphCompiler.TryCompile(exported, out var compiled, out var error))
						graphs.Add(compiled);
					else
						Console.Error.WriteLine($"[BehaviorGraph] Failed to compile graph '{entry.Name}' (id '{entry.Id}') for '{graphKey}' (requested '{speciesName}'): {error}");
				}
			}
		}

		return new PlantSpeciesProfile { Morphology = morph, BehaviorGraphs = graphs };
	}
}
