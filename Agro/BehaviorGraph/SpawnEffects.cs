using System.Numerics;
using Agro.Plant.Flower;

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
		TwigOrientationParams twig,
		float lateralRoll,
		float energyFraction = 0.1f,
		float waterFraction = 0.1f,
		Quaternion? orientationOverride = null)
	{
		var plant = formation.Plant;

		var (prevResources, prevProduction) = PrevDayInvariants(ref parent, formation, timestep);

		var orientation = orientationOverride
			?? AboveGroundAgent.TurnUpwards(OrientationEffects.RandomOrientation(
				ref parent, plant, parent.Orientation, twig));
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

	/// <summary>Bergania.Tick commitToFlower — flower-base meristem before regular chaining meristem.</summary>
	public static int SpawnFlowerMeristemChild(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		uint timestep,
		TwigOrientationParams twig,
		float lateralRoll,
		float energyFraction = 0.1f,
		float waterFraction = 0.1f)
	{
		var plant = formation.Plant;
		var (prevResources, prevProduction) = PrevDayInvariants(ref parent, formation, timestep);
		var orientation = AboveGroundAgent.TurnUpwards(OrientationEffects.RandomOrientation(
			ref parent, plant, parent.Orientation, twig));
		var lateralPitch = parent.LateralAngle + lateralRoll;

		var childIndex = formation.Birth(new(plant, parentAgentId, OrganTypes.FlowerMeristem, orientation,
			energyFraction * parent.Energy, initialResources: prevResources, initialProduction: prevProduction)
		{
			Water_g = waterFraction * parent.Water_g,
			LateralAngle = lateralPitch,
			DominanceLevel = parent.DominanceLevel,
			FlowerAgent = new Flower { debth = 0, flowerBase = true },
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
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config,
		TwigOrientationParams twig,
		float lateralRoll)
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
			OrientationEffects.RandomOrientation(ref parent, plant, orientation1, twig), 0.1f * energy,
			initialResources: prevResources, initialProduction: prevProduction)
		{
			Water_g = 0.1f * water,
			LateralAngle = lateralPitch,
			DominanceLevel = parent.DominanceLevel,
		});
		var meristem2 = formation.Birth(new(plant, parentAgentId, OrganTypes.Meristem,
			OrientationEffects.RandomOrientation(ref parent, plant, orientation2, twig), 0.1f * energy,
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

	/// <summary>
	/// Legacy <c>Bergania.CreateRizome</c>: branch orientation, BVH rhizome-overlap abort, soil abort, then rhizome+bud birth.
	/// </summary>
	/// <returns>True when a rhizome was birthed.</returns>
	public static bool TrySpawnRhizome(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		RhizomeSpawnParams rhizome,
		float childYawOffset,
		float rootYawOffset)
	{
		var plant = formation.Plant;

		Quaternion rizomOrientation;
		if (parentAgentId > 0)
			rizomOrientation = parent.Orientation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, childYawOffset);
		else
			rizomOrientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0f * MathF.PI)
				* Quaternion.CreateFromAxisAngle(Vector3.UnitY, rootYawOffset);

		var rizome = new AboveGroundAgent(plant, parentAgentId, OrganTypes.Stem, rizomOrientation, 0,
			length: rhizome.RizomeLength, radius: rhizome.RizomeRadius);
		rizome.isRizome = true;
		rizome.rizomeInfo.rizomeDepth = parent.rizomeInfo.rizomeDepth + 1;

		var tip = formation.GetTipPosition(parentAgentId);
		var overlaps = formation.CollisionBvh.QueryOverlaps(
			formation.ComputeBoundsFromParameters(tip, rizomOrientation, rizome.Length, rizome.Radius));
		if (overlaps is not null)
		{
			var children = formation.GetChildren(parentAgentId);
			foreach (var collision in overlaps)
			{
				if (parentAgentId != collision
					&& (children is null || !children.Contains(collision))
					&& formation.GetIsRizome(collision))
				{
					return false;
				}

				var tipWorld = formation.GetBaseCenterWorld(parentAgentId)
					+ Vector3.Transform(Vector3.UnitX, formation.GetDirection(parentAgentId)) * parent.Length
					+ Vector3.Transform(Vector3.UnitX, rizome.Orientation) * rizome.Length;
				if (plant.Soil.IntersectPoint(tipWorld, plant.SoilIndex) < 0)
					return false;
			}
		}

		var rizomeIndex = formation.Birth(rizome);
		var lateralPitch = parent.LateralAngle + rhizome.LateralRoll;
		var bud = new AboveGroundAgent(plant, rizomeIndex, OrganTypes.Bud, rizomOrientation, 0,
			initialResources: 1f, initialProduction: 1f)
		{
			LateralAngle = lateralPitch,
			Radius = 0f,
			trySpawn = true,
		};
		formation.Birth(bud);
		return true;
	}

	/// <summary>Backward-compatible wrapper used by older call sites; ignores collision result.</summary>
	public static void SpawnRhizome(
		ref AboveGroundAgent parent,
		PlantSubFormation<AboveGroundAgent> formation,
		int parentAgentId,
		Quaternion orientation,
		RhizomeSpawnParams rhizome)
	{
		_ = orientation;
		TrySpawnRhizome(ref parent, formation, parentAgentId, rhizome, childYawOffset: 0f, rootYawOffset: 0f);
	}
}
