using System.Numerics;

namespace Agro.BehaviorGraph;

static class OrientationEffects
{
	public static Quaternion RandomOrientation(
		ref AboveGroundAgent parent,
		PlantFormation2 plant,
		Quaternion orientation,
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config)
	{
		var twigsBendingLevel = BehaviorGraphConfig.Number(
			config, DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingLevel, DefaultSpeciesGraphBuilder.DefaultTickConstants.TwigsBendingLevel);
		var twigsBendingApical = BehaviorGraphConfig.Number(
			config, DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingApical, DefaultSpeciesGraphBuilder.DefaultTickConstants.TwigsBendingApical);
		var twigsBending = BehaviorGraphConfig.Number(
			config, DefaultSpeciesGraphBuilder.ConfigIds.TwigsBending, DefaultSpeciesGraphBuilder.DefaultTickConstants.TwigsBending);
		var shootsGravitaxis = BehaviorGraphConfig.Number(
			config, DefaultSpeciesGraphBuilder.ConfigIds.ShootsGravitaxis, DefaultSpeciesGraphBuilder.DefaultTickConstants.ShootsGravitaxis);

		var range = 0.2f * MathF.PI * (twigsBendingLevel * parent.DominanceLevel - twigsBendingApical);
		var factor = twigsBending * range;
		var a = plant.RNG.NextFloatVar(factor);
		orientation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, a);
		var y = Vector3.Transform(Vector3.UnitX, orientation).Y;

		if (y < 0)
			orientation = Quaternion.Slerp(orientation, PlantFormation2.AdjustUpBase(orientation, up: true), plant.RNG.NextPositiveFloat(-y));
		else
			orientation = Quaternion.Slerp(orientation, PlantFormation2.AdjustUpBase(orientation, up: true), plant.RNG.NextPositiveFloat(shootsGravitaxis));

		return orientation;
	}
}
