using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agro;

/// <summary>
/// Resolves <see cref="SpeciesSettings"/> (morphology and static parameters) from <see cref="SimulationRequest"/> by name.
/// Does not use behavior graphs; see <see cref="PlantSpeciesProfile"/>.
/// </summary>
public static class SpeciesMorphology
{
	static Dictionary<string, string>? PredefinedMorphologyJson;

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

	/// <summary>
	/// Frontend <c>Species.ts</c> signal defaults that differ from <see cref="SpeciesSettings"/> field defaults.
	/// Treated as "unset HUD" so they do not overwrite catalog Init().
	/// </summary>
	static readonly Dictionary<string, float[]> UnsetHudNumericValues = new(StringComparer.Ordinal)
	{
		["Height"] = [10f, 12f],
		["NodeDistance"] = [0.04f],
		["NodeDistanceVar"] = [0.01f],
		["MonopodialFactor"] = [1f],
		["DominanceFactor"] = [0f, 0.7f],
		["AuxinsProduction"] = [40f],
		["LateralsPerNode"] = [2f],
		["LateralRoll"] = [0f],
		["LateralRollVar"] = [5f * MathF.PI / 180f],
		["LateralPitch"] = [45f * MathF.PI / 180f],
		["LateralPitchVar"] = [5f * MathF.PI / 180f],
		["TwigsBending"] = [0.5f],
		["TwigsBendingLevel"] = [1f],
		["TwigsBendingApical"] = [0.02f],
		["ShootsGravitaxis"] = [0.2f],
		["WoodGrowthTime"] = [100f],
		["WoodGrowthTimeVar"] = [10f],
		["LeafLength"] = [0.12f],
		["LeafLengthVar"] = [0.02f],
		["LeafRadius"] = [0.04f],
		["LeafRadiusVar"] = [0.01f],
		["LeafGrowthTime"] = [480f],
		["LeafGrowthTimeVar"] = [120f],
		["LeafPitch"] = [20f * MathF.PI / 180f],
		["LeafPitchVar"] = [5f * MathF.PI / 180f],
		["PetioleLength"] = [0.04f, 0.05f],
		["PetioleLengthVar"] = [0.01f],
		["PetioleRadius"] = [0.0025f, 0.0015f],
		["PetioleRadiusVar"] = [0.0005f],
		["RootsGravitaxis"] = [0.2f],
		["Behavior"] = [0f],
	};

	/// <summary>
	/// Capture catalog morphology before any <see cref="SpeciesSettings.Init"/> can run.
	/// Called from <see cref="SpeciesSettings"/> static construction.
	/// </summary>
	internal static void CapturePredefinedSnapshots(IReadOnlyList<SpeciesSettings> predefined)
	{
		if (PredefinedMorphologyJson != null)
			return;
		PredefinedMorphologyJson = BuildPredefinedSnapshots(predefined);
	}

	static Dictionary<string, string> Snapshots =>
		PredefinedMorphologyJson ??= BuildPredefinedSnapshots(SpeciesSettings.Predefined);

	static Dictionary<string, string> BuildPredefinedSnapshots(IReadOnlyList<SpeciesSettings> predefined)
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var template in predefined)
		{
			if (string.IsNullOrEmpty(template.Name))
				continue;
			map[template.Name] = JsonSerializer.Serialize(template, AgroJsonSerializerContext.Default.SpeciesSettings);
		}

		return map;
	}

	/// <summary>
	/// Legacy morphology resolve: request species object wins as-is, else shared predefined, else Default.
	/// Do not change this path — it is part of the legacy simulation contract.
	/// </summary>
	public static SpeciesSettings Resolve(string? speciesName, SimulationRequest? settings)
	{
		if (!string.IsNullOrEmpty(speciesName))
		{
			var legacy = settings?.Species?.FirstOrDefault(x => x.Name == speciesName);
			if (legacy != null)
				return legacy;

			var predefined = SpeciesSettings.Predefined.FirstOrDefault(x => x.Name == speciesName);
			if (predefined != null)
				return predefined;
		}

		return SpeciesSettings.Default;
	}

	/// <summary>
	/// Node-graph morphology: fresh Init() template with HUD fields overlaid (rhizome / crown /
	/// seasonal chaining stay on the catalog). Used by <see cref="PlantSpeciesProfile"/>.
	/// </summary>
	public static SpeciesSettings ResolveForNodeGraphs(string? speciesName, SimulationRequest? settings)
	{
		if (!string.IsNullOrEmpty(speciesName))
		{
			var ui = settings?.Species?.FirstOrDefault(x => x.Name == speciesName);
			if (Snapshots.TryGetValue(speciesName, out var json))
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
	/// seasonal chaining stay on the predefined Init() template.
	/// Tree-default HUD values (C# field default or Species.ts signal default) are
	/// skipped when Init() customized that field — otherwise geranium internodes,
	/// petioles, and leaf angles get replaced by the Default-species sliders.
	/// </summary>
	static SpeciesSettings OverlayUiMorphology(SpeciesSettings template, SpeciesSettings ui)
	{
		var templateNode = JsonSerializer.SerializeToNode(template, AgroJsonSerializerContext.Default.SpeciesSettings)!.AsObject();
		var uiNode = JsonSerializer.SerializeToNode(ui, AgroJsonSerializerContext.Default.SpeciesSettings)!.AsObject();
		foreach (var prop in UiMorphologyProperties)
		{
			if (uiNode[prop] is not { } value)
				continue;
			if (IsUnsetHudValue(prop, value, templateNode[prop]))
				continue;
			templateNode[prop] = value.DeepClone();
		}

		var merged = JsonSerializer.Deserialize(templateNode, AgroJsonSerializerContext.Default.SpeciesSettings)
			?? throw new InvalidOperationException("Failed to merge UI morphology onto predefined template.");
		merged.DominanceFactors = (float[])template.DominanceFactors.Clone();
		return merged;
	}

	static bool IsUnsetHudValue(string prop, JsonNode uiVal, JsonNode? templateVal)
	{
		if (JsonNumericEquals(uiVal, templateVal))
			return false;

		// Source-gen System.Text.Json zeros omitted properties (skips C# field initializers).
		if (uiVal.GetValueKind() == JsonValueKind.Number
			&& NearlyEqual(uiVal.GetValue<float>(), 0f)
			&& templateVal is not null
			&& templateVal.GetValueKind() == JsonValueKind.Number
			&& !NearlyEqual(templateVal.GetValue<float>(), 0f))
			return true;

		if (uiVal.GetValueKind() == JsonValueKind.Number
			&& UnsetHudNumericValues.TryGetValue(prop, out var unsetValues))
		{
			var uiNum = uiVal.GetValue<float>();
			foreach (var unset in unsetValues)
			{
				if (NearlyEqual(uiNum, unset))
					return true;
			}
		}

		return false;
	}

	static bool JsonNumericEquals(JsonNode? a, JsonNode? b)
	{
		if (a is null || b is null)
			return a is null && b is null;
		if (a.GetValueKind() == JsonValueKind.Number && b.GetValueKind() == JsonValueKind.Number)
			return NearlyEqual(a.GetValue<float>(), b.GetValue<float>());
		return a.ToJsonString() == b.ToJsonString();
	}

	static bool NearlyEqual(float a, float b) => MathF.Abs(a - b) <= 1e-5f;

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
