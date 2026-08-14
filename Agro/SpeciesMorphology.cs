using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agro;

/// <summary>
/// Resolves <see cref="SpeciesSettings"/> (morphology and static parameters) from <see cref="SimulationRequest"/> by name.
/// Does not use behavior graphs; see <see cref="PlantSpeciesProfile"/>.
/// </summary>
public static class SpeciesMorphology
{
	static readonly Dictionary<string, string> PredefinedMorphologyJson = BuildPredefinedSnapshots();

	/// <summary>
	/// Fields the frontend <c>Species.serialize()</c> actually sends. Everything else
	/// (rhizome, crown pitch, seasonal chaining, …) must stay on the predefined template.
	/// </summary>
	static readonly string[] UiMorphologyProperties =
	[
		"Aka", "Behavior",
		"Height",
		"NodeDistance", "NodeDistanceVar",
		"MonopodialFactor", "DominanceFactor",
		"AuxinsProduction",
		"LateralsPerNode",
		"LateralRoll", "LateralRollVar", "LateralPitch", "LateralPitchVar",
		"TwigsBending", "TwigsBendingLevel", "TwigsBendingApical", "ShootsGravitaxis",
		"WoodGrowthTime", "WoodGrowthTimeVar",
		"LeafLength", "LeafLengthVar", "LeafRadius", "LeafRadiusVar",
		"LeafGrowthTime", "LeafGrowthTimeVar", "LeafPitch", "LeafPitchVar",
		"PetioleLength", "PetioleLengthVar", "PetioleRadius", "PetioleRadiusVar",
		"RootsGravitaxis",
	];

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
			var ui = settings?.Species?.FirstOrDefault(x => x.Name == speciesName);
			if (PredefinedMorphologyJson.TryGetValue(speciesName, out var json))
			{
				var template = DeserializeFresh(speciesName, json);
				return ui is null ? template : OverlayUiMorphology(template, ui);
			}

			if (ui != null)
				return ui;
		}

		return SpeciesSettings.Default;
	}

	/// <summary>
	/// Overlay HUD fields the frontend serialize() always sends. Rhizome / crown /
	/// seasonal chaining stay on the predefined Init() template so legacy ticks
	/// keep the original catalog values.
	/// </summary>
	static SpeciesSettings OverlayUiMorphology(SpeciesSettings template, SpeciesSettings ui)
	{
		var templateNode = JsonSerializer.SerializeToNode(template, AgroJsonSerializerContext.Default.SpeciesSettings)!.AsObject();
		var uiNode = JsonSerializer.SerializeToNode(ui, AgroJsonSerializerContext.Default.SpeciesSettings)!.AsObject();
		foreach (var prop in UiMorphologyProperties)
		{
			if (uiNode[prop] is { } value)
				templateNode[prop] = value.DeepClone();
		}

		var merged = JsonSerializer.Deserialize(templateNode, AgroJsonSerializerContext.Default.SpeciesSettings)
			?? throw new InvalidOperationException("Failed to merge UI morphology onto predefined template.");
		merged.DominanceFactors = (float[])template.DominanceFactors.Clone();
		return merged;
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
