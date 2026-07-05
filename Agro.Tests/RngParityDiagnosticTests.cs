using Agro.BehaviorGraph;
using Agro.Testing;
using Xunit;

namespace Agro.Tests;

/// <summary>Plant RNG stream must match legacy when all Default species graphs are enabled.</summary>
public class RngParityDiagnosticTests
{
	static SimulationRequest BuildRequest(int graphCount, int totalHours = 4)
	{
		var subgraphs = DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs();
		var graphs = subgraphs.Take(graphCount).Select(g => new SpeciesGraphUploadEntry
		{
			Id = Guid.NewGuid().ToString(),
			Name = g.Name,
			Graph = g.Graph,
		}).ToList();

		return new SimulationRequest
		{
			Seed = 42,
			TotalHours = totalHours,
			HoursPerTick = 1,
			Plants = [new PlantRequest { SpeciesName = "Default" }],
			SpeciesGraphs = graphCount > 0
				? new Dictionary<string, List<SpeciesGraphUploadEntry>> { ["Default"] = graphs }
				: null,
			SpeciesConfiguration = new Dictionary<string, List<BehaviorConfigUploadEntry>>
			{
				["Default"] = [.. DefaultSpeciesGraphBuilder.BuildDefaultConfiguration()],
			},
		};
	}

	static (ulong State, ulong Increment) PlantRngAtTimestep(SimulationRequest request, BehaviorRunMode mode, uint timestep)
	{
		var path = Path.Combine(Path.GetTempPath(), $"agro-rng-{Guid.NewGuid():N}.jsonl");
		try
		{
			SimulationHarness.RecordTrace(request, mode, path, maxHours: (int)timestep + 1);
			var step = SimulationHarness.ReadSteps(path).First(s => s.Timestep == timestep);
			var rng = step.Plants[0].Rng ?? throw new InvalidOperationException("Missing RNG snapshot.");
			return (rng.State, rng.Increment);
		}
		finally
		{
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	[Theory]
	[InlineData(2)]
	[InlineData(62)]
	[InlineData(63)]
	public void RngParity_AllDefaultGraphs_MatchesLegacy(uint timestep)
	{
		var hours = (int)timestep + 1;
		var legacy = PlantRngAtTimestep(BuildRequest(0, hours), BehaviorRunMode.Legacy, timestep);
		var graphCount = DefaultSpeciesGraphBuilder.BuildDefaultSpeciesSubgraphs().Count;
		var node = PlantRngAtTimestep(BuildRequest(graphCount, hours), BehaviorRunMode.Node, timestep);
		Assert.Equal(legacy, node);
	}
}
