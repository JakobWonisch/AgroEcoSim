using System.Text.Json;

namespace Agro.BehaviorGraph;

/// <summary>
/// Builds <see cref="SpeciesSettings"/> for node-graph plants from behavior configuration
/// overlaid on the named predefined template (pre-<see cref="SpeciesSettings.Init"/> state).
/// </summary>
public static class SpeciesSettingsFromConfiguration
{
	public static SpeciesSettings Build(
		string? speciesName,
		IReadOnlyDictionary<string, BehaviorConfigEntry> config)
	{
		var template = FindPredefined(speciesName);
		var json = JsonSerializer.SerializeToNode(template, AgroJsonSerializerContext.Default.SpeciesSettings)!.AsObject();
		ApplyConfiguration(json, config);
		var settings = JsonSerializer.Deserialize(json, AgroJsonSerializerContext.Default.SpeciesSettings)
			?? throw new InvalidOperationException("Failed to build species settings from configuration.");

		if (BehaviorGraphConfig.TryArray(config, DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactors, out var dominanceFactors)
			&& dominanceFactors.Length > 0)
			settings.DominanceFactors = (float[])dominanceFactors.Clone();

		return settings;
	}

	static SpeciesSettings FindPredefined(string? speciesName)
	{
		if (!string.IsNullOrEmpty(speciesName))
		{
			var predefined = SpeciesSettings.Predefined.FirstOrDefault(x => x.Name == speciesName);
			if (predefined != null)
				return predefined;
		}

		return SpeciesSettings.Default;
	}

	static void ApplyConfiguration(
		System.Text.Json.Nodes.JsonObject json,
		IReadOnlyDictionary<string, BehaviorConfigEntry> config)
	{
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.Height, "Height");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafLength, "LeafLength");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafLengthVar, "LeafLengthVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafRadius, "LeafRadius");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafRadiusVar, "LeafRadiusVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafGrowthTime, "LeafGrowthTime");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafGrowthTimeVar, "LeafGrowthTimeVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.PetioleLength, "PetioleLength");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.PetioleLengthVar, "PetioleLengthVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadius, "PetioleRadius");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.PetioleRadiusVar, "PetioleRadiusVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.NodeDistance, "NodeDistance");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.NodeDistanceVar, "NodeDistanceVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.MonopodialFactor, "MonopodialFactor");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.DominanceFactor, "DominanceFactor");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.AuxinsProduction, "AuxinsProduction");
		SetInt(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode, "LateralsPerNode");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LateralRoll, "LateralRoll");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LateralRollVar, "LateralRollVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LateralPitch, "LateralPitch");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LateralPitchVar, "LateralPitchVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafPitch, "LeafPitch");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.LeafPitchVar, "LeafPitchVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.TwigsBending, "TwigsBending");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingLevel, "TwigsBendingLevel");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingApical, "TwigsBendingApical");
		SetShootsGravitaxis(json, config);
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime, "WoodGrowthTime");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTimeVar, "WoodGrowthTimeVar");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.RizomeLength, "RizomeLength");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.RizomeRadius, "RizomeRadius");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.FloweringStartAgeHours, "FloweringStartAgeHours");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.FloweringEndAgeHours, "FloweringEndAgeHours");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.PetioleCoverThreshold, "PetioleCoverThreshold");

		SetNum(json, config, BerganiaTickGraphBuilder.ConfigIds.GrowthFactor, "growthFactor");
		SetNum(json, config, BerganiaTickGraphBuilder.ConfigIds.MaxRadius, "MaxRadius");
		SetNum(json, config, BerganiaTickGraphBuilder.ConfigIds.PNewCrown, "pNewCrown");
		SetNum(json, config, BerganiaTickGraphBuilder.ConfigIds.PExpandRizome, "pExpandRizome");
		SetInt(json, config, BerganiaTickGraphBuilder.ConfigIds.RizomeMaxDepth, "RizomeMaxDepth");
		SetNum(json, config, BerganiaTickGraphBuilder.ConfigIds.CrownPitch, "crownPitch");
		SetNum(json, config, DefaultSpeciesGraphBuilder.ConfigIds.PetioleAgeBudMinHours, "MaxLeaveAge");

		SetArray(json, config, BerganiaTickGraphBuilder.ConfigIds.PChaining, "pChaningSeaonns");
		SetArray(json, config, BerganiaTickGraphBuilder.ConfigIds.PFlowering, "pFloweringSeaonns");
	}

	static void SetShootsGravitaxis(System.Text.Json.Nodes.JsonObject json, IReadOnlyDictionary<string, BehaviorConfigEntry> config)
	{
		if (!BehaviorGraphConfig.TryNumber(config, DefaultSpeciesGraphBuilder.ConfigIds.ShootsGravitaxis, out var postInit))
			return;
		// Configuration stores post-Init effective gravitaxis; Init multiplies by 0.4.
		json["ShootsGravitaxis"] = postInit / 0.4f;
	}

	static void SetNum(
		System.Text.Json.Nodes.JsonObject json,
		IReadOnlyDictionary<string, BehaviorConfigEntry> config,
		string configId,
		string propertyName)
	{
		if (BehaviorGraphConfig.TryNumber(config, configId, out var value))
			json[propertyName] = value;
	}

	static void SetInt(
		System.Text.Json.Nodes.JsonObject json,
		IReadOnlyDictionary<string, BehaviorConfigEntry> config,
		string configId,
		string propertyName)
	{
		if (BehaviorGraphConfig.TryNumber(config, configId, out var value))
			json[propertyName] = (int)MathF.Round(value);
	}

	static void SetArray(
		System.Text.Json.Nodes.JsonObject json,
		IReadOnlyDictionary<string, BehaviorConfigEntry> config,
		string configId,
		string propertyName)
	{
		if (BehaviorGraphConfig.TryArray(config, configId, out var values) && values.Length > 0)
			json[propertyName] = JsonSerializer.SerializeToNode(values);
	}
}
