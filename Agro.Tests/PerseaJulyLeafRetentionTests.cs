using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Agro.Tests;

/// <summary>Investigate July 2026 recording vs current legacy leaf retention at 1440h.</summary>
public class PerseaJulyLeafRetentionTests
{
	readonly ITestOutputHelper _out;
	public PerseaJulyLeafRetentionTests(ITestOutputHelper output) => _out = output;

	static int LowerStemPetioleCount(StepSnapshot step, int stemSample = 8)
	{
		var ag = step.Plants[0].AboveGround;
		var stems = ag.Where(a => a.Organ is "Stem" or "Meristem").OrderBy(a => a.Index).Take(stemSample);
		return stems.Sum(s => ag.Count(a => a.Parent == s.Index && a.Organ == "Petiole"));
	}

	[Theory]
	[InlineData("July HUD", 100f, 2, 0.12f)]
	[InlineData("Persea catalog", 2400f, 4, 0.2f)]
	public void Legacy_NoGraphs_LowerStemPetioles_At1440h(string label, float wgt, int laterals, float leafLen)
	{
		const int totalHours = 1440;
		const int hoursPerTick = 4;
		var species = SpeciesSettings.Predefined.First(s => s.Name == "Persea americana");
		species = new SpeciesSettings
		{
			Name = species.Name,
			Aka = species.Aka,
			DominanceFactor = species.DominanceFactor,
			WoodGrowthTime = wgt,
			WoodGrowthTimeVar = wgt <= 100f ? 10f : species.WoodGrowthTimeVar,
			LateralsPerNode = laterals,
			LeafLength = leafLen,
			LeafRadius = species.LeafRadius,
			PetioleLength = species.PetioleLength,
			PetioleRadius = species.PetioleRadius,
			LeafGrowthTime = species.LeafGrowthTime,
			Height = species.Height,
		};

		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = hoursPerTick,
			Species = [species],
			Plants = [new PlantRequest { SpeciesName = "Persea americana" }],
			// No SpeciesGraphs → pure TickDefault legacy
		};

		var path = Path.Combine(Path.GetTempPath(), $"persea-legacy-{label.Replace(' ', '-')}.jsonl");
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Legacy, path, maxHours: totalHours);
		var final = SimulationHarness.ReadSteps(path).Last();
		var petioles = final.Plants[0].AboveGround.Count(a => a.Organ == "Petiole");
		var leaves = final.Plants[0].AboveGround.Count(a => a.Organ == "Leaf");
		var lowerPetioles = LowerStemPetioleCount(final);
		_out.WriteLine($"{label}: agents={final.Plants[0].AboveGround.Length} petioles={petioles} leaves={leaves} lowerStemPetioles={lowerPetioles}");
	}

	[Fact]
	public void JulyHudSettings_Graphs_LowerStemPetioles_At1440h()
	{
		const int totalHours = 1440;
		const int hoursPerTick = 4;
		var config = PerseaSpeciesGraphBuilder.BuildConfiguration().ToList();
		void SetCfg(string id, float v)
		{
			var i = config.FindIndex(e => e.Id == id);
			if (i >= 0) config[i] = new BehaviorConfigUploadEntry { Id = config[i].Id, Key = config[i].Key, Label = config[i].Label, Usage = config[i].Usage, Type = config[i].Type, Value = BehaviorGraphJson.Number(v) };
		}
		SetCfg(DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTime, 100f);
		SetCfg(DefaultSpeciesGraphBuilder.ConfigIds.WoodGrowthTimeVar, 10f);
		SetCfg(DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode, 2f);
		SetCfg(DefaultSpeciesGraphBuilder.ConfigIds.LeafLength, 0.12f);

		var request = new SimulationRequest
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = hoursPerTick,
			Species =
			[
				new SpeciesSettings
				{
					Name = "Persea americana",
					Aka = "Avocado",
					DominanceFactor = 0.7f,
					WoodGrowthTime = 100f,
					WoodGrowthTimeVar = 10f,
					LateralsPerNode = 2,
					LeafLength = 0.12f,
					LeafRadius = 0.04f,
					PetioleLength = 0.05f,
					PetioleRadius = 0.007f,
					LeafGrowthTime = 720f,
					Height = 12f,
				},
			],
			Plants = [new PlantRequest { SpeciesName = "Persea americana" }],
			SpeciesGraphs = new Dictionary<string, List<SpeciesGraphUploadEntry>>
			{
				["Persea americana"] = PredefinedSpeciesCatalog.All
					.First(s => s.Name == "Persea americana").Graphs
					.Select(g => new SpeciesGraphUploadEntry { Id = g.Id, Name = g.Name, Graph = g.Graph }).ToList(),
			},
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Persea americana"] = config,
			},
		};

		var path = Path.Combine(Path.GetTempPath(), "persea-july-hud-graphs.jsonl");
		SimulationHarness.RecordTrace(request, BehaviorRunMode.Node, path, maxHours: totalHours);
		var final = SimulationHarness.ReadSteps(path).Last();
		var ag = final.Plants[0].AboveGround;
		var lowerPetioles = LowerStemPetioleCount(final);
		_out.WriteLine($"July HUD+graphs: agents={ag.Length} petioles={ag.Count(a => a.Organ == "Petiole")} lowerStemPetioles={lowerPetioles}");
	}

	[Fact]
	public void JulyRecording_UsesGraphs_NotPureLegacy()
	{
		var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		var path = Path.Combine(repoRoot, "ignore", "legacy-persea-AgroEco-2026-07-11_18-56-24.json");
		if (!File.Exists(path))
		{
			_out.WriteLine($"Optional July recording not found at {path}; skipping.");
			return;
		}

		var json = File.ReadAllText(path);
		Assert.Contains("\"name\":\"Petiole age bud\"", json);
		Assert.Contains("Persea americana", json);
		Assert.Contains("\"woodGrowthTime\":100", json);
		_out.WriteLine("July recording: Persea with graphs, WGT=100, laterals=2, leaf=0.12");
	}
}
