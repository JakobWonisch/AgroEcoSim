using System.Numerics;

namespace Agro.BehaviorGraph;

static class OrientationEffects
{
	/// <summary>Legacy <see cref="AboveGroundAgent.RandomOrientation"/> uses morphology parameters.</summary>
	public static Quaternion RandomOrientation(
		ref AboveGroundAgent parent,
		PlantFormation2 plant,
		Quaternion orientation,
		IReadOnlyDictionary<string, BehaviorConfigEntry>? _)
		=> parent.RandomOrientation(plant, plant.Parameters, orientation);
}
