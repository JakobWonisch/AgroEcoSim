namespace Agro.Testing;

/// <summary>
/// Maps above-ground organ types to behavior-graph names that primarily gate on that organ.
/// Shared maintenance graphs are included so a filtered single-agent node run still covers
/// life support. Underground / seed organs are not in this catalog.
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

	static readonly string[] SharedFlower =
	[
		.. SharedVegetative,
		"Flower meristem growth",
		"Flower stem growth",
		"Flower reset death",
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
				.. SharedFlower,
			],
			[OrganTypes.FlowerMeristem] =
			[
				.. SharedFlower,
			],
			[OrganTypes.FlowerBud] =
			[
				.. SharedFlower,
			],
			[OrganTypes.FlowerPadel] =
			[
				.. SharedFlower,
			],
			[OrganTypes.FlowerPetiol] =
			[
				.. SharedFlower,
			],
			[OrganTypes.FlowerBaseBud] =
			[
				.. SharedFlower,
			],
			[OrganTypes.Fruit] = [.. SharedVegetative],
			[OrganTypes.RizomeMeristem] = [.. SharedVegetative],
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

	/// <summary>Flower / inflorescence organs handled by FlowerHelper in Bergania ticks.</summary>
	public static OrganTypes[] FlowerFocusOrgans { get; } =
	[
		OrganTypes.FlowerStem,
		OrganTypes.FlowerMeristem,
		OrganTypes.FlowerBud,
		OrganTypes.FlowerPadel,
		OrganTypes.FlowerPetiol,
		OrganTypes.FlowerBaseBud,
	];

	/// <summary>Remaining above-ground organs (not roots/seed). Isolated tests skip underground agents.</summary>
	public static OrganTypes[] OtherAboveGroundFocusOrgans { get; } =
	[
		OrganTypes.Fruit,
		OrganTypes.RizomeMeristem,
	];

	/// <summary>Every above-ground organ type the isolated harness can install.</summary>
	public static OrganTypes[] AllAboveGroundFocusOrgans { get; } =
	[
		.. DefaultFocusOrgans,
		.. FlowerFocusOrgans,
		.. OtherAboveGroundFocusOrgans,
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
