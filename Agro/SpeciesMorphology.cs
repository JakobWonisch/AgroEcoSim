using System.Text.Json;

namespace Agro;

/// <summary>
/// Resolves <see cref="SpeciesSettings"/> (morphology and static parameters) from <see cref="SimulationRequest"/> by name.
/// Does not use behavior graphs; see <see cref="PlantSpeciesProfile"/>.
/// </summary>
public static class SpeciesMorphology
{
	static readonly Dictionary<string, string> PredefinedMorphologyJson = BuildPredefinedSnapshots();

	static Dictionary<string, string> BuildPredefinedSnapshots()
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var template in SpeciesSettings.Predefined)
		{
			if (string.IsNullOrEmpty(template.Name))
				continue;
			map[template.Name] = JsonSerializer.Serialize(template, AgroJsonSerializerContext.Default.SpeciesSettings);
		}

		return map;
	}

	public static SpeciesSettings Resolve(string? speciesName, SimulationRequest? settings)
	{
		if (!string.IsNullOrEmpty(speciesName))
		{
			var legacy = settings?.Species?.FirstOrDefault(x => x.Name == speciesName);
			if (legacy != null)
				return legacy;

			if (PredefinedMorphologyJson.TryGetValue(speciesName, out var json))
				return DeserializeFresh(speciesName, json);
		}

		return SpeciesSettings.Default;
	}

	internal static SpeciesSettings DeserializeFresh(string speciesName, string json)
	{
		var fresh = JsonSerializer.Deserialize(json, AgroJsonSerializerContext.Default.SpeciesSettings)
			?? throw new InvalidOperationException("Failed to deserialize species morphology template.");

		// JSON may carry default DominanceFactor=0 and rebuild a table that does not match catalog templates.
		var template = SpeciesSettings.Predefined.FirstOrDefault(x => x.Name == speciesName);
		if (template is not null)
			fresh.DominanceFactors = (float[])template.DominanceFactors.Clone();

		return fresh;
	}
}
