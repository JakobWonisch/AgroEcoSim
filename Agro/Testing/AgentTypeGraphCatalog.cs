namespace Agro.Testing;

/// <summary>
/// Maps above-ground organ types to Default-species behavior graph names that primarily
/// gate on that organ. Shared graphs (life support, energy, auxins, wood) are included
/// for every vegetative organ so a filtered single-agent node run still covers maintenance.
/// </summary>
public static class AgentTypeGraphCatalog
{
	static readonly string[] SharedVegetative =
	[
		"Life support",
		"Energy depletion",
		"Auxins update",
		"Wood lignify",
	];

	static readonly IReadOnlyDictionary<OrganTypes, string[]> DefaultByOrgan =
		new Dictionary<OrganTypes, string[]>
		{
			[OrganTypes.Leaf] =
			[
				.. SharedVegetative,
				"Photosynthesis",
				"Growth leaf",
			],
			[OrganTypes.Petiole] =
			[
				.. SharedVegetative,
				"Petiole age bud",
				"Growth petiole",
				"Petiole cover bud",
				"Petiole unproductive death",
			],
			[OrganTypes.Stem] =
			[
				.. SharedVegetative,
				"Stem dominance death",
				"Auxin twig",
				"Growth stem",
			],
			[OrganTypes.Meristem] =
			[
				.. SharedVegetative,
				"Meristem tick marker",
				"Growth meristem",
				"Meristem chain",
			],
			[OrganTypes.Bud] =
			[
				.. SharedVegetative,
			],
			[OrganTypes.FlowerStem] =
			[
				.. SharedVegetative,
			],
			[OrganTypes.FlowerMeristem] =
			[
				.. SharedVegetative,
			],
			[OrganTypes.FlowerBud] =
			[
				.. SharedVegetative,
			],
			[OrganTypes.FlowerPadel] =
			[
				.. SharedVegetative,
			],
			[OrganTypes.FlowerPetiol] =
			[
				.. SharedVegetative,
			],
		};

	/// <summary>Primary vegetative organ types useful for Default-species agent-type parity.</summary>
	public static OrganTypes[] DefaultFocusOrgans { get; } =
	[
		OrganTypes.Leaf,
		OrganTypes.Petiole,
		OrganTypes.Stem,
		OrganTypes.Meristem,
		OrganTypes.Bud,
	];

	public static IReadOnlyList<string> GraphsFor(OrganTypes organ)
	{
		if (DefaultByOrgan.TryGetValue(organ, out var names))
			return names;
		return SharedVegetative;
	}

	/// <summary>Union of graph names for one or more focus organs (case-sensitive catalog names).</summary>
	public static IReadOnlyList<string> GraphsFor(IEnumerable<OrganTypes> organs)
	{
		var set = new HashSet<string>(StringComparer.Ordinal);
		foreach (var organ in organs)
		{
			foreach (var name in GraphsFor(organ))
				set.Add(name);
		}
		return set.OrderBy(n => n, StringComparer.Ordinal).ToList();
	}
}
