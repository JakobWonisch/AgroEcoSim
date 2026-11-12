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

	/// <summary>
	/// Bergania.Tick after chaining: walks from <paramref name="startParent"/> up to the rhizome and bends every
	/// petiole child (and its first child) via orientation messages. Renormalizes even when <paramref name="moveDown"/> is 0.
	/// </summary>
	public static void BendAncestorPetioles(PlantSubFormation<AboveGroundAgent> formation, int startParent, float moveDown, float moveDownMax)
	{
		var parent = startParent;
		while (parent >= 0 && !formation.GetIsRizome(parent))
		{
			foreach (var child in formation.GetChildren(parent))
			{
				if (formation.GetOrgan(child).Equals(OrganTypes.Petiole))
					BendPetiole(formation, child, moveDown, moveDownMax);
			}
			parent = formation.GetParent(parent);
		}
	}

	static void BendPetiole(PlantSubFormation<AboveGroundAgent> formation, int agentID, float moveDown, float moveDownMax)
	{
		float maxAngle = moveDownMax * MathF.PI;
		var parentRot = formation.GetDirection(formation.GetParent(agentID));
		var parentDir = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, parentRot));

		var dir = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, formation.GetDirection(agentID)));

		float cos = Vector3.Dot(dir, parentDir);
		cos = Math.Clamp(cos, -1f, 1f);
		float angleFromParent = MathF.Acos(cos);

		if (angleFromParent < maxAngle)
		{
			var axis = Vector3.Cross(parentDir, dir);
			if (axis.LengthSquared() > 1e-8f)
			{
				axis = Vector3.Normalize(axis);

				float delta = moveDown * MathF.PI;
				var worldRot = Quaternion.CreateFromAxisAngle(axis, delta);
				var or = formation.GetDirection(agentID);
				formation.SendProtected(agentID, new AboveGroundAgent.OrientationSet(Quaternion.Normalize(worldRot * or)));

				formation.SendProtected(formation.GetChildren(agentID).First(), new AboveGroundAgent.OrientationSet(Quaternion.Normalize(worldRot * formation.GetDirection(formation.GetChildren(agentID).First()))));
			}
		}
	}
}
