namespace Agro;

/// <summary>Parameters for <see cref="AboveGroundAgent"/> leaf layout helpers.</summary>
public readonly record struct LeafLayoutParams(
	int LateralsPerNode,
	float LateralRollVar,
	float LateralPitchVar,
	float LateralPitch,
	float LeafPitch);

/// <summary>Parameters for <see cref="AboveGroundAgent.RandomOrientation"/>.</summary>
public readonly record struct TwigOrientationParams(
	float TwigsBending,
	float TwigsBendingLevel,
	float TwigsBendingApical,
	float ShootsGravitaxis);

/// <summary>Parameters for rhizome spawn helpers.</summary>
public readonly record struct RhizomeSpawnParams(
	float RizomeLength,
	float RizomeRadius,
	float LateralRoll);

public static class MorphologyParams
{
	public static LeafLayoutParams LeafLayout(SpeciesSettings species) => new(
		species.LateralsPerNode,
		species.LateralRollVar,
		species.LateralPitchVar,
		species.LateralPitch,
		species.LeafPitch);

	public static TwigOrientationParams Twig(SpeciesSettings species) => new(
		species.TwigsBending,
		species.TwigsBendingLevel,
		species.TwigsBendingApical,
		species.ShootsGravitaxis);

	public static RhizomeSpawnParams Rhizome(SpeciesSettings species) => new(
		species.RizomeLength,
		species.RizomeRadius,
		species.LateralRoll);
}
