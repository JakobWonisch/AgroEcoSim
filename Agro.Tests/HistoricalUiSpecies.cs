using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Agro.Testing;

namespace Agro.Tests;

/// <summary>
/// The species payload the baseline web UI (7b80ad2, ThreeFrontend/src/helpers/Species.ts load() + serialize())
/// uploaded for each predefined species. The legacy reference runs on this payload, not on the raw presets.
/// </summary>
public static class HistoricalUiSpecies
{
	/// <summary>GET /Simulation/species response of the baseline server (pre-Init presets, camelCase).</summary>
	public const string BaselineCatalogFile = "TestData/baseline-predefined-species.json";

	const double DegToRad = Math.PI / 180.0;
	const double RadToDeg = 180.0 / Math.PI;

	static readonly Lazy<JsonArray> payload = new(() =>
	{
		var path = Path.Combine(AppContext.BaseDirectory, BaselineCatalogFile);
		var catalog = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
		return RoundTrip(catalog);
	});

	public static JsonArray Payload => payload.Value;

	[ModuleInitializer]
	internal static void InstallLegacyReference() => SimulationHarness.LegacySpeciesPayload = Payload;

	/// <summary>
	/// JavaScript (double) arithmetic of the baseline UI. Fields the UI did not carry are dropped, so the
	/// server falls back to <see cref="SpeciesSettings"/> defaults; misspelled or unknown keys are dropped server-side.
	/// </summary>
	public static JsonArray RoundTrip(JsonArray serverSpecies)
	{
		var result = new JsonArray();
		foreach (var item in serverSpecies)
		{
			var s = item!.AsObject();
			double N(string key) => s[key]!.GetValue<double>();
			result.Add(new JsonObject
			{
				["Name"] = s["name"]!.GetValue<string>(),
				["Aka"] = s["aka"]?.GetValue<string>(),
				["Behavior"] = s["behavior"]!.GetValue<int>(),
				["Height"] = N("height"),
				["NodeDistance"] = N("nodeDistance"),
				["NodeDistanceVar"] = N("nodeDistanceVar"),
				["MonopodialFactor"] = N("monopodialFactor"),
				["DominanceFactor"] = N("dominanceFactor"),
				["AuxinsProduction"] = N("auxinsProduction"),
				["LateralsPerNode"] = N("lateralsPerNode"),
				["LateralRoll"] = N("lateralRoll") * RadToDeg * DegToRad,
				["LateralRollVar"] = N("lateralRollVar") * RadToDeg * DegToRad,
				["LateralPitch"] = N("lateralPitch") * RadToDeg * DegToRad,
				["LateralPitchVar"] = N("lateralPitchVar") * RadToDeg * DegToRad,
				["TwigsBending"] = N("twigsBending"),
				["TwigsBendingLevel"] = N("twigsBendingLevel"),
				["TwigsBendingApical"] = 1.0 - N("twigsBendingApical"),
				["ShootsGravitaxis"] = N("shootsGravitaxis"),
				["WoodGrowthTime"] = N("woodGrowthTime") * 24,
				["WoodGrowthTimeVar"] = N("woodGrowthTimeVar") * 24,
				["LeafLength"] = N("leafLength"),
				["LeafLengthVar"] = N("leafLengthVar"),
				["LeafRadius"] = N("leafRadius"),
				["LeafRadiusVar"] = N("leafRadiusVar"),
				["LeafGrowthTime"] = N("leafGrowthTime"),
				["LeafGrowthTimeVar"] = N("leafGrowthTimeVar"),
				["LeafPitch"] = N("leafPitch") * RadToDeg * DegToRad,
				["LeafPitchVar"] = N("leafPitchVar") * RadToDeg * DegToRad,
				["PetioleLength"] = N("petioleLength"),
				["PetioleLengthVar"] = N("petioleLengthVar"),
				["PetioleRadius"] = N("petioleRadius"),
				["PetioleRadiusVar"] = N("petioleRadiusVar"),
				["RootsGravitaxis"] = N("rootsGravitaxis"),
			});
		}
		return result;
	}
}
