using System.Numerics;

namespace Agro.BehaviorGraph;

static class OrientationEffects
{
	public static Quaternion RandomOrientation(
		ref AboveGroundAgent parent,
		PlantFormation2 plant,
		Quaternion orientation,
		TwigOrientationParams twig)
		=> parent.RandomOrientation(plant, twig, orientation);
}
