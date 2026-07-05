using System.Numerics;
using Agro.BehaviorGraph;

namespace Agro;

public partial struct AboveGroundAgent
{
	/// <summary>
	/// Config-driven leaf creation. Unlike legacy <c>CreateLeavesBase</c>, parent energy is deducted via <c>ref</c>
	/// (legacy passes parent by value so <c>Energy *= 0.9f</c> never persists).
	/// </summary>
	internal static void GraphCreateLeaves(
		ref AboveGroundAgent parent,
		PlantFormation2 plant,
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config,
		float lateralAngle,
		int meristem)
	{
		var laterals = (int)BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode, 2);
		if (laterals <= 0)
			return;

		var lateralRollVar = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.LateralRollVar);
		var lateralPitchVar = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.LateralPitchVar);
		var lateralPitch = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.LateralPitch);
		var leafPitch = BehaviorGraphConfig.Number(config, DefaultSpeciesGraphBuilder.ConfigIds.LeafPitch);

		var initialResources = parent.PreviousDayEnvResourcesInvariant;
		var initialProduction = parent.PreviousDayProductionInvariant;
		var angleStep = 2f * MathF.PI / laterals;

		for (var l = 0; l < laterals; ++l)
		{
			var roll = plant.RNG.NextFloatVar(lateralRollVar);
			var pitch = plant.RNG.NextFloatVar(lateralPitchVar);
			var orientation = parent.Orientation
				* Quaternion.CreateFromAxisAngle(Vector3.UnitX, l * angleStep + lateralAngle)
				* Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -lateralPitch);
			orientation = TurnUpwards(orientation)
				* Quaternion.CreateFromAxisAngle(Vector3.UnitX, roll)
				* Quaternion.CreateFromAxisAngle(Vector3.UnitZ, pitch);

			var petioleIdx = plant.AG.Birth(new(plant, meristem, OrganTypes.Petiole, orientation, parent.Energy * 0.1f,
				initialResources: initialResources, initialProduction: initialProduction)
			{
				DominanceLevel = parent.DominanceLevel,
				ParentRadiusAtBirth = parent.Radius,
			});
			parent.Energy *= 0.9f;

			var leafPitchVar = plant.RNG.NextFloatVar(lateralPitchVar);
			orientation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, leafPitchVar - leafPitch);

			plant.AG.Birth(new(plant, petioleIdx, OrganTypes.Leaf, orientation, parent.Energy * 0.1f,
				initialResources: initialResources, initialProduction: initialProduction)
			{
				DominanceLevel = parent.DominanceLevel,
				ParentRadiusAtBirth = float.MaxValue,
			});
			parent.Energy *= 0.9f;
		}
	}
}
