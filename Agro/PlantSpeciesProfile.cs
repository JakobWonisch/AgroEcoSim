using Agro.BehaviorGraph;

namespace Agro;

/// <summary>
/// Morphology from named templates plus optional compiled above-ground behavior graph.
/// </summary>
public sealed class PlantSpeciesProfile
{
	public required SpeciesSettings Morphology { get; init; }
	public CompiledBehaviorGraph? BehaviorGraph { get; init; }

	public static PlantSpeciesProfile Resolve(string? speciesName, SimulationRequest? settings)
	{
		var morph = SpeciesMorphology.Resolve(speciesName, settings);
		CompiledBehaviorGraph? graph = null;
		if (!string.IsNullOrEmpty(speciesName) && settings?.SpeciesGraphs != null)
		{
			global::ExportedGraph? exported = null;
			var graphKey = speciesName;
			if (!settings.SpeciesGraphs.TryGetValue(speciesName, out exported))
			{
				var matches = settings.SpeciesGraphs.Keys
					.Where(k => string.Equals(k, speciesName, StringComparison.OrdinalIgnoreCase))
					.ToArray();
				if (matches.Length == 1)
				{
					graphKey = matches[0];
					exported = settings.SpeciesGraphs[graphKey];
					Console.Error.WriteLine($"[BehaviorGraph] Using case-insensitive graph key match for species '{speciesName}': '{graphKey}'.");
				}
				else if (matches.Length > 1)
				{
					Console.Error.WriteLine($"[BehaviorGraph] Multiple case-insensitive graph keys match species '{speciesName}': [{string.Join(", ", matches)}]. Using '{matches[0]}'.");
					graphKey = matches[0];
					exported = settings.SpeciesGraphs[graphKey];
				}
				else
				{
					var keys = string.Join(", ", settings.SpeciesGraphs.Keys);
					Console.Error.WriteLine($"[BehaviorGraph] No graph key found for species '{speciesName}'. Available SpeciesGraphs keys: [{keys}]");
				}
			}

			if (exported?.Nodes is not { Count: > 0 })
			{
				if (exported != null)
					Console.Error.WriteLine($"[BehaviorGraph] Species graph for '{graphKey}' (requested '{speciesName}') is empty.");
			}
			else if (exported != null)
			{
				if (BehaviorGraphCompiler.TryCompile(exported, out var compiled, out var error))
					graph = compiled;
				else
					Console.Error.WriteLine($"[BehaviorGraph] Failed to compile species graph for '{graphKey}' (requested '{speciesName}'): {error}");
			}
		}

		return new PlantSpeciesProfile { Morphology = morph, BehaviorGraph = graph };
	}
}
