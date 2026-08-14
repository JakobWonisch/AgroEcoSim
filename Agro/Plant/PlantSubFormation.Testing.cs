using System.Numerics;

namespace Agro;

/// <summary>Test-only helpers to install a fixed agent population for parity harnesses.</summary>
partial class PlantSubFormation<T>
{
	/// <summary>
	/// Replace the live agent buffer with <paramref name="agents"/> and rebuild the tree cache.
	/// Clears pending births/deaths. Does not respect <see cref="AgroWorld.ParitySuppressSpawn"/>.
	/// </summary>
	internal void ReplaceAgentsForTesting(params T[] agents)
	{
		Births.Clear();
		Deaths.Clear();
		DeathsHelper.Clear();
		ReadTMP = false;
		Agents = (T[])agents.Clone();
		AgentsTMP = new T[Agents.Length];

		TreeCache.Clear(Agents.Length);
		for (var i = 0; i < Agents.Length; ++i)
			TreeCache.AddChild(Agents[i].Parent, i);
		TreeCache.FinishUpdate();
		TreeCache.UpdateBases(this);
		BuildBvh();
	}
}

partial class PlantFormation2
{
	/// <summary>
	/// Kill the seed without germinating, add a minimal underground root for resource census,
	/// then install the subject above-ground agent (with an optional stem scaffold parent when
	/// the organ type reads parent state during tick).
	/// </summary>
	/// <returns>Index of the subject above-ground agent.</returns>
	internal int InstallSingleOrganSceneForTesting(AboveGroundAgent subject, bool withScaffoldParent)
	{
		if (SeedAlive)
			SeedDeath();

		// Empty UG makes energy/water census Debug.Assert fail for some organs.
		if (UG is PlantSubFormation<UnderGroundAgent> ugFormation && ugFormation.Count == 0)
		{
			UGBirth(new UnderGroundAgent(
				this,
				World.Timestep,
				parent: -1,
				orientation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -0.5f * MathF.PI),
				initialEnergy: 50f,
				initialWater_g: 20f,
				initialResources: 1f,
				initialProduction: 1f));
			ugFormation.Census();
		}

		if (withScaffoldParent)
		{
			var scaffold = new AboveGroundAgent(
				this,
				parent: -1,
				organ: OrganTypes.Stem,
				orientation: Quaternion.Identity,
				initialEnergy: 50f,
				radius: 0.005f,
				length: 0.05f,
				initialResources: 1f,
				initialProduction: 1f)
			{
				Water_g = 10f,
				DominanceLevel = 0,
				Auxins = 0f,
			};
			AG.ReplaceAgentsForTesting(scaffold, subject);
		}
		else
		{
			AG.ReplaceAgentsForTesting(subject);
		}

		AG.FirstDay();
		return withScaffoldParent ? 1 : 0;
	}
}
