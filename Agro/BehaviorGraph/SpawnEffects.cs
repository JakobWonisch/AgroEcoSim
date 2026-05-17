using System.Numerics;

namespace Agro.BehaviorGraph;

public static class SpawnEffects
{
	public static int SpawnChild(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		uint timestep,
		OrganTypes organ,
		float energyFraction = 0.1f,
		float waterFraction = 0.1f,
		Quaternion? orientationOverride = null)
	{
		var plant = formation.Plant;
		var species = plant.Parameters;
		var world = plant.World;

		float prevResources, prevProduction;
		if (timestep - parent.GraphBirthTime() > world.HoursPerTick)
		{
			prevResources = parent.PreviousDayEnvResourcesInvariant;
			prevProduction = parent.PreviousDayProductionInvariant;
		}
		else
		{
			prevResources = formation.DailyResourceMax;
			prevProduction = formation.DailyProductionMax;
		}

		var orientation = orientationOverride
			?? AboveGroundAgent.TurnUpwards(parent.RandomOrientation(plant, species, parent.Orientation));
		var lateralPitch = parent.LateralAngle + species.LateralRoll;

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

	public static void SpawnRhizome(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		Quaternion orientation)
	{
		var plant = formation.Plant;
		var species = plant.Parameters;
		var rizome = new AboveGroundAgent(plant, parentAgentId, OrganTypes.Stem, orientation, 0,
			length: species.RizomeLength, radius: species.RizomeRadius);
		rizome.isRizome = true;
		rizome.rizomeInfo.rizomeDepth = parent.rizomeInfo.rizomeDepth + 1;
		var rizomeIndex = formation.Birth(rizome);
		var lateralPitch = parent.LateralAngle + species.LateralRoll;
		var bud = new AboveGroundAgent(plant, rizomeIndex, OrganTypes.Bud, orientation, 0,
			initialResources: 1f, initialProduction: 1f)
		{
			LateralAngle = lateralPitch,
			Radius = 0f,
		};
		formation.Birth(bud);
	}
}
