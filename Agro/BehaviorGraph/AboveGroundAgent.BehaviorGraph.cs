using System.Numerics;

namespace Agro;

public partial struct AboveGroundAgent
{
	/// <summary>
	/// Delegates to legacy <see cref="CreateLeaves"/> so node mode uses morphology
	/// (<see cref="PlantFormation2.Parameters"/>) like <see cref="TickDefault"/>, not graph config alone.
	/// </summary>
	internal static void GraphCreateLeaves(
		ref AboveGroundAgent parent,
		PlantFormation2 plant,
		IReadOnlyDictionary<string, BehaviorConfigEntry>? _,
		float lateralAngle,
		int meristem)
	{
		var parentCopy = parent;
		parentCopy.CreateLeaves(parentCopy, plant, lateralAngle, meristem);
	}
}
