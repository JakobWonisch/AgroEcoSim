using System.Numerics;
using Agro.Plant.Flower;

namespace Agro.Testing;

public sealed class SimulationTraceHeader
{
	public required string Mode { get; init; }
	public ulong? Seed { get; init; }
	public int? HoursPerTick { get; init; }
	public int? TotalHours { get; init; }
}

public sealed class StepSnapshot
{
	public uint Timestep { get; init; }
	public required PlantSnapshot[] Plants { get; init; }
}

public sealed class PlantSnapshot
{
	public int PlantIndex { get; init; }
	public bool SeedAlive { get; init; }
	public SeedSnapshot? Seed { get; init; }
	public float WaterBalance { get; init; }
	public float WaterBalanceUG { get; init; }
	public float EnergyBalance { get; init; }
	public float EnergyProductionMax { get; init; }
	public RngSnapshot? Rng { get; init; }
	public VirtualRootsSnapshot? VirtualRoots { get; init; }
	public required AgentSnapshot[] AboveGround { get; init; }
	public required AgentSnapshot[] BelowGround { get; init; }
}

public sealed class SeedSnapshot
{
	public int SoilIndex { get; init; }
	public Vector3Snapshot Center { get; init; }
	public float Radius { get; init; }
	public float Water_g { get; init; }
	public float GerminationProgress { get; init; }
}

public sealed class RngSnapshot
{
	public ulong State { get; init; }
	public ulong Increment { get; init; }
}

public sealed class VirtualRootsSnapshot
{
	public float Water_g { get; init; }
	public float Energy { get; init; }
	public Vector3Snapshot Size { get; init; }
}

public sealed class Vector3Snapshot
{
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }

	public static Vector3Snapshot From(Vector3 v) => new() { X = v.X, Y = v.Y, Z = v.Z };
}

public sealed class QuaternionSnapshot
{
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }
	public float W { get; init; }

	public static QuaternionSnapshot From(Quaternion q) => new() { X = q.X, Y = q.Y, Z = q.Z, W = q.W };
}

public sealed class FlowerSnapshot
{
	public bool FlowerBase { get; init; }
	public int Debth { get; init; }
	public uint BirthTime { get; init; }
	public uint FlowerStartTime { get; init; }
}

public sealed class RizomeInfoSnapshot
{
	public bool Test { get; init; }
	public bool Test2 { get; init; }
	public bool Test3 { get; init; }
	public bool Test4 { get; init; }
	public int RizomeDepth { get; init; }
}

public sealed class AgentSnapshot
{
	public int Index { get; init; }
	public string Organ { get; init; } = "";
	public int Parent { get; init; }
	public float Length { get; init; }
	public float Radius { get; init; }
	public Vector3Snapshot BaseOffset { get; init; } = new();
	public QuaternionSnapshot Orientation { get; init; } = new() { W = 1f };
	public float Energy { get; init; }
	public float Water_g { get; init; }
	public float Auxins { get; init; }
	public byte DominanceLevel { get; init; }
	public bool IsRizome { get; init; }
	public float LateralAngle { get; init; }
	public float ParentRadiusAtBirth { get; init; }
	public float LeafMaxRadiusRatio { get; init; }
	public uint BirthTime { get; init; }
	public float LengthVar { get; init; }
	public float RadiusVar { get; init; }
	public float GrowthTimeVar { get; init; }
	public FlowerSnapshot Flower { get; init; } = new();
	public RizomeInfoSnapshot? RizomeInfo { get; init; }
}

public static class PlantStateSnapshot
{
	public static StepSnapshot Capture(AgroWorld world, uint timestep)
	{
		var plants = new List<PlantSnapshot>();
		world.ForEach(formation =>
		{
			if (formation is PlantFormation2 plant)
				plants.Add(CapturePlant(plant, plants.Count));
		});
		return new StepSnapshot { Timestep = timestep, Plants = plants.ToArray() };
	}

	static PlantSnapshot CapturePlant(PlantFormation2 plant, int plantIndex)
	{
		SeedSnapshot? seed = null;
		if (plant.SeedAlive)
		{
			var s = plant.GetSeedForTesting();
			seed = new SeedSnapshot
			{
				SoilIndex = s.SoilIndex,
				Center = Vector3Snapshot.From(s.Center),
				Radius = s.Radius,
				Water_g = s.Water_g,
				GerminationProgress = s.GerminationProgress,
			};
		}

		VirtualRootsSnapshot? virtualRoots = null;
		AgentSnapshot[] belowGround;
		if (plant.World.VirtualRoots && plant.UG is VirtualRootsFormation vr)
		{
			var (water, energy, size) = vr.SnapshotForTesting();
			virtualRoots = new VirtualRootsSnapshot
			{
				Water_g = water,
				Energy = energy,
				Size = Vector3Snapshot.From(size),
			};
			belowGround = [];
		}
		else if (plant.UG is PlantSubFormation<UnderGroundAgent> ug)
			belowGround = CaptureUnderGround(ug.ActiveAgentsForTesting());
		else
			belowGround = [];

		var (rngState, rngInc) = plant.RNG.Snapshot();
		return new PlantSnapshot
		{
			PlantIndex = plantIndex,
			SeedAlive = plant.SeedAlive,
			Seed = seed,
			WaterBalance = plant.WaterBalanceForTesting(),
			WaterBalanceUG = plant.WaterBalanceUGForTesting(),
			EnergyBalance = plant.EnergyBalanceForTesting(),
			EnergyProductionMax = plant.EnergyProductionMaxForTesting(),
			Rng = new RngSnapshot { State = rngState, Increment = rngInc },
			VirtualRoots = virtualRoots,
			AboveGround = CaptureAboveGround(plant.AG.ActiveAgentsForTesting()),
			BelowGround = belowGround,
		};
	}

	static AgentSnapshot[] CaptureAboveGround(ReadOnlySpan<AboveGroundAgent> agents)
	{
		var result = new AgentSnapshot[agents.Length];
		for (var i = 0; i < agents.Length; ++i)
		{
			var a = agents[i];
			result[i] = new AgentSnapshot
			{
				Index = i,
				Organ = a.Organ.ToString(),
				Parent = a.Parent,
				Length = a.Length,
				Radius = a.Radius,
				BaseOffset = Vector3Snapshot.From(a.BaseOffset),
				Orientation = QuaternionSnapshot.From(a.Orientation),
				Energy = a.Energy,
				Water_g = a.Water_g,
				Auxins = a.Auxins,
				DominanceLevel = a.DominanceLevel,
				IsRizome = a.isRizome,
				LateralAngle = a.LateralAngle,
				ParentRadiusAtBirth = a.ParentRadiusAtBirth,
				LeafMaxRadiusRatio = a.LeafMaxRadiusRatio,
				BirthTime = a.TestingBirthTime,
				LengthVar = a.TestingLengthVar,
				RadiusVar = a.TestingRadiusVar,
				GrowthTimeVar = a.TestingGrowthTimeVar,
				Flower = CaptureFlower(a.FlowerAgent),
				RizomeInfo = CaptureRizome(a.rizomeInfo),
			};
		}
		return result;
	}

	static AgentSnapshot[] CaptureUnderGround(ReadOnlySpan<UnderGroundAgent> agents)
	{
		var result = new AgentSnapshot[agents.Length];
		for (var i = 0; i < agents.Length; ++i)
		{
			var a = agents[i];
			result[i] = new AgentSnapshot
			{
				Index = i,
				Organ = a.Organ.ToString(),
				Parent = a.Parent,
				Length = a.Length,
				Radius = a.Radius,
				Orientation = QuaternionSnapshot.From(a.Orientation),
				Energy = a.Energy,
				Water_g = a.Water_g,
				Auxins = a.Auxins,
				DominanceLevel = a.DominanceLevel,
				IsRizome = a.isRizome,
				BirthTime = a.TestingBirthTime,
			};
		}
		return result;
	}

	static FlowerSnapshot CaptureFlower(Flower f) => new()
	{
		FlowerBase = f.flowerBase,
		Debth = f.debth,
		BirthTime = f.BirthTime,
		FlowerStartTime = f.FlowerStartTime,
	};

	static RizomeInfoSnapshot CaptureRizome(AboveGroundAgent.rizomeInfos info) => new()
	{
		Test = info.test,
		Test2 = info.test2,
		Test3 = info.test3,
		Test4 = info.test4,
		RizomeDepth = info.rizomeDepth,
	};
}
