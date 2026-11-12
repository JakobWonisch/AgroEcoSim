namespace Agro;

/// <summary>
/// Resolves <see cref="SpeciesSettings"/> (morphology and static parameters) from <see cref="SimulationRequest"/> by name.
/// Does not use behavior graphs; see <see cref="PlantSpeciesProfile"/>.
/// </summary>
public static class SpeciesMorphology
{
	/// <summary>
	/// Legacy morphology resolve: request species object wins as-is, else shared predefined, else Default.
	/// Do not change this path — it is part of the legacy simulation contract.
	/// </summary>
	public static SpeciesSettings Resolve(string? speciesName, SimulationRequest? settings)
	{
		if (!string.IsNullOrEmpty(speciesName))
		{
			var legacy = settings?.Species?.FirstOrDefault(x => x.Name == speciesName);
			if (legacy != null)
				return legacy;

			var predefined = SpeciesSettings.Predefined.FirstOrDefault(x => x.Name == speciesName);
			if (predefined != null)
				return predefined;
		}

		return SpeciesSettings.Default;
	}
}
