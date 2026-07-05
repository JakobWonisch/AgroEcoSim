namespace Agro;

partial class PlantFormation2
{
	internal SeedAgent GetSeedForTesting() => Seed[0];

	internal float WaterBalanceForTesting() => WaterBalance;
	internal float WaterBalanceUGForTesting() => WaterBalanceUG;
	internal float EnergyBalanceForTesting() => EnergyBalance;
	internal float EnergyProductionMaxForTesting() => EnergyProductionMax;
}
