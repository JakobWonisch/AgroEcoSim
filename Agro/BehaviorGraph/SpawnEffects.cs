using System.Numerics;

namespace Agro.BehaviorGraph;

public static class SpawnEffects
{
	static (float Resources, float Production) PrevDayInvariants(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		uint timestep)
	{
		var world = formation.Plant.World;
		if (timestep - parent.GraphBirthTime() > world.HoursPerTick)
			return (parent.PreviousDayEnvResourcesInvariant, parent.PreviousDayProductionInvariant);
		return (formation.DailyResourceMax, formation.DailyProductionMax);
	}

	public static int SpawnChild(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		uint timestep,
		OrganTypes organ,
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config,
		float energyFraction = 0.1f,
		float waterFraction = 0.1f,
		Quaternion? orientationOverride = null)
	{
		var plant = formation.Plant;

		var (prevResources, prevProduction) = PrevDayInvariants(ref parent, formation, timestep);

		var orientation = orientationOverride
			?? AboveGroundAgent.TurnUpwards(OrientationEffects.RandomOrientation(
				ref parent, plant, parent.Orientation, config));
		var lateralRoll = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.LateralRoll);
		var lateralPitch = parent.LateralAngle + lateralRoll;

		var childIndex = formation.Birth(new(plant, parentAgentId, organ, orientation, energyFraction * parent.Energy,
			initialResources: prevResources, initialProduction: prevProduction)
		{
			Water_g = waterFraction * parent.Water_g,
			LateralAngle = lateralPitch,
			DominanceLevel = parent.DominanceLevel,
		});

		parent.Energy *= 1f - energyFraction;
		parent.Water_g *= 1f - waterFraction;
		return childIndex;
	}

	/// <summary>TickDefault dichotomous meristem chain — two children at 10% energy/water each, parent retains 80%.</summary>
	public static (int Meristem1, int Meristem2, float LateralPitch) SpawnDichotomousMeristems(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		uint timestep,
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config)
	{
		var plant = formation.Plant;
		var (prevResources, prevProduction) = PrevDayInvariants(ref parent, formation, timestep);

		var monoFactor = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.MonopodialFactor, 1f);
		var lateralPitch = 0.3f * MathF.PI * monoFactor;
		var ou = AboveGroundAgent.TurnUpwards(parent.Orientation);
		var orientation1 = ou * Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.5f * MathF.PI)
			* Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -lateralPitch - 0.25f * MathF.PI);
		var orientation2 = ou * Quaternion.CreateFromAxisAngle(Vector3.UnitX, -0.5f * MathF.PI)
			* Quaternion.CreateFromAxisAngle(Vector3.UnitZ, lateralPitch - 0.25f * MathF.PI);

		var energy = parent.Energy;
		var water = parent.Water_g;

		var meristem1 = formation.Birth(new(plant, parentAgentId, OrganTypes.Meristem,
			OrientationEffects.RandomOrientation(ref parent, plant, orientation1, config), 0.1f * energy,
			initialResources: prevResources, initialProduction: prevProduction)
		{
			Water_g = 0.1f * water,
			LateralAngle = lateralPitch,
			DominanceLevel = parent.DominanceLevel,
		});
		var meristem2 = formation.Birth(new(plant, parentAgentId, OrganTypes.Meristem,
			OrientationEffects.RandomOrientation(ref parent, plant, orientation2, config), 0.1f * energy,
			initialResources: prevResources, initialProduction: prevProduction)
		{
			Water_g = 0.1f * water,
			LateralAngle = lateralPitch,
			DominanceLevel = parent.DominanceLevel,
		});

		parent.Energy *= 0.8f;
		parent.Water_g *= 0.8f;
		return (meristem1, meristem2, lateralPitch);
	}

	public static void SpawnRhizome(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		Quaternion orientation,
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config)
	{
		var plant = formation.Plant;
		var rizomeLength = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.RizomeLength, DefaultSpeciesGraphBuilder.DefaultTickConstants.RizomeLength);
		var rizomeRadius = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.RizomeRadius, DefaultSpeciesGraphBuilder.DefaultTickConstants.RizomeRadius);
		var rizome = new AboveGroundAgent(plant, parentAgentId, OrganTypes.Stem, orientation, 0,
			length: rizomeLength, radius: rizomeRadius);
		rizome.isRizome = true;
		rizome.rizomeInfo.rizomeDepth = parent.rizomeInfo.rizomeDepth + 1;
		var rizomeIndex = formation.Birth(rizome);
		var lateralRoll = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.LateralRoll);
		var lateralPitch = parent.LateralAngle + lateralRoll;
		var bud = new AboveGroundAgent(plant, rizomeIndex, OrganTypes.Bud, orientation, 0,
			initialResources: 1f, initialProduction: 1f)
		{
			LateralAngle = lateralPitch,
			Radius = 0f,
		};
		formation.Birth(bud);
	}
}
