namespace Agro;

public partial struct AboveGroundAgent
{
	internal static void GraphCreateLeaves(
		ref AboveGroundAgent parent,
		PlantFormation2 plant,
		float lateralAngle,
		int meristem,
		LeafLayoutParams layout)
	{
		var parentCopy = parent;
		parentCopy.CreateLeaves(parentCopy, plant, lateralAngle, meristem, layout);
		parent = parentCopy;
	}
}
