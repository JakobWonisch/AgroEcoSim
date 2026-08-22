using System.Text.Json;

namespace Agro.BehaviorGraph;

/// <summary>
/// Resolves behavior configuration values from <see cref="SpeciesSettings"/> and bootstrap defaults
/// when an entry is missing from the uploaded configuration dictionary.
/// </summary>
static class BehaviorConfigParameterFallback
{
	static readonly Lazy<Dictionary<string, float>> DefaultBootstrapNumbers = new(BuildDefaultBootstrapNumbers);
	static readonly Lazy<Dictionary<string, float[]>> DefaultBootstrapArrays = new(BuildDefaultBootstrapArrays);

	public static bool TryNumber(SpeciesSettings species, string? configId, out float value)
	{
		value = 0f;
		if (string.IsNullOrWhiteSpace(configId))
			return false;

		switch (configId)
		{
			case DefaultSpeciesGraphBuilder.ConfigIds.Height:
				value = species.Height; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafLength:
				value = species.LeafLength; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafLengthVar:
				value = species.LeafLengthVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius:
				value = species.LeafRadius; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafRadiusVar:
				value = species.LeafRadiusVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafGrowthTime:
				value = species.LeafGrowthTime; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafGrowthTimeVar:
				value = species.LeafGrowthTimeVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength:
				value = species.PetioleLength; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.PetioleLengthVar:
				value = species.PetioleLengthVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius:
				value = species.PetioleRadius; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadiusVar:
				value = species.PetioleRadiusVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.PetioleCoverThreshold:
				value = species.PetioleCoverThreshold; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.NodeDistance:
				value = species.NodeDistance; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.NodeDistanceVar:
				value = species.NodeDistanceVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.MonopodialFactor:
				value = species.MonopodialFactor; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactor:
				value = species.DominanceFactor; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.AuxinsProduction:
				value = species.AuxinsProduction; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode:
				value = species.LateralsPerNode; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LateralRoll:
				value = species.LateralRoll; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LateralRollVar:
				value = species.LateralRollVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LateralPitch:
				value = species.LateralPitch; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LateralPitchVar:
				value = species.LateralPitchVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafPitch:
				value = species.LeafPitch; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.LeafPitchVar:
				value = species.LeafPitchVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.TwigsBending:
				value = species.TwigsBending; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingLevel:
				value = species.TwigsBendingLevel; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingApical:
				value = species.TwigsBendingApical; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.ShootsGravitaxis:
				// Config stores post-Init effective gravitaxis; Parameters match after Init().
				value = species.ShootsGravitaxis;
				return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime:
				value = species.WoodGrowthTime; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTimeVar:
				value = species.WoodGrowthTimeVar; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.RizomeLength:
				value = species.RizomeLength; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.RizomeRadius:
				value = species.RizomeRadius; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.FloweringStartAgeHours:
				value = species.FloweringStartAgeHours; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.FloweringEndAgeHours:
				value = species.FloweringEndAgeHours; return true;
			case DefaultSpeciesGraphBuilder.ConfigIds.PetioleAgeBudMinHours:
				value = species.MaxLeaveAge; return true;
			case BerganiaTickGraphBuilder.ConfigIds.GrowthFactor:
				value = species.growthFactor; return true;
			case BerganiaTickGraphBuilder.ConfigIds.MaxRadius:
				value = species.MaxRadius; return true;
			case BerganiaTickGraphBuilder.ConfigIds.PNewCrown:
				value = species.pNewCrown; return true;
			case BerganiaTickGraphBuilder.ConfigIds.PExpandRizome:
				value = species.pExpandRizome; return true;
			case BerganiaTickGraphBuilder.ConfigIds.RizomeMaxDepth:
				value = species.RizomeMaxDepth; return true;
			case BerganiaTickGraphBuilder.ConfigIds.CrownPitch:
				value = species.crownPitch; return true;
		}

		return DefaultBootstrapNumbers.Value.TryGetValue(configId, out value);
	}

	public static bool TryArrayElement(SpeciesSettings species, string? configId, float index, out float value)
	{
		value = 0f;
		if (string.IsNullOrWhiteSpace(configId))
			return false;

		float[]? arr = configId switch
		{
			DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactors => species.DominanceFactors,
			BerganiaTickGraphBuilder.ConfigIds.PChaining => species.pChaningSeaonns,
			BerganiaTickGraphBuilder.ConfigIds.PFlowering => species.pFloweringSeaonns,
			_ => null,
		};

		if (arr is { Length: > 0 })
		{
			var i = (int)MathF.Floor(index);
			if (i < 0) i = 0;
			if (i >= arr.Length) i = arr.Length - 1;
			value = arr[i];
			return true;
		}

		if (DefaultBootstrapArrays.Value.TryGetValue(configId, out var bootstrap) && bootstrap.Length > 0)
		{
			var i = (int)MathF.Floor(index);
			if (i < 0) i = 0;
			if (i >= bootstrap.Length) i = bootstrap.Length - 1;
			value = bootstrap[i];
			return true;
		}

		return false;
	}

	static Dictionary<string, float> BuildDefaultBootstrapNumbers()
	{
		var dict = new Dictionary<string, float>(StringComparer.Ordinal);
		foreach (var entry in DefaultSpeciesGraphBuilder.BuildDefaultConfiguration())
		{
			if (!string.Equals(entry.Type, "number", StringComparison.OrdinalIgnoreCase))
				continue;
			dict[entry.Id] = ReadNumberValue(entry.Value);
		}
		return dict;
	}

	static Dictionary<string, float[]> BuildDefaultBootstrapArrays()
	{
		var dict = new Dictionary<string, float[]>(StringComparer.Ordinal);
		foreach (var entry in DefaultSpeciesGraphBuilder.BuildDefaultConfiguration())
		{
			if (!string.Equals(entry.Type, "number[]", StringComparison.OrdinalIgnoreCase))
				continue;
			dict[entry.Id] = ReadFloatArrayValue(entry.Value);
		}
		return dict;
	}

	static float ReadNumberValue(JsonElement value) =>
		value.ValueKind switch
		{
			JsonValueKind.Number => value.TryGetSingle(out var f) ? f : 0f,
			JsonValueKind.True => 1f,
			JsonValueKind.False => 0f,
			_ => 0f,
		};

	static float[] ReadFloatArrayValue(JsonElement value)
	{
		if (value.ValueKind != JsonValueKind.Array)
			return [];
		var list = new List<float>();
		foreach (var el in value.EnumerateArray())
		{
			list.Add(el.ValueKind switch
			{
				JsonValueKind.Number => el.TryGetSingle(out var f) ? f : 0f,
				JsonValueKind.True => 1f,
				JsonValueKind.False => 0f,
				_ => 0f,
			});
		}
		return list.ToArray();
	}
}
