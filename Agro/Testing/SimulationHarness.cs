using System.Text.Json;
using System.Text.Json.Nodes;
using Utils;
using Utils.Json;

namespace Agro.Testing;

public enum BehaviorRunMode
{
	Legacy,
	Node,
}

public static class SimulationHarness
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = null,
		WriteIndented = false,
	};

	public static SimulationRequest PrepareForParity(SimulationRequest source, BehaviorRunMode mode, int? maxHours = null)
	{
		var node = JsonSerializer.SerializeToNode(source, JsonOptions)!.AsObject();
		node["RenderMode"] = 0;
		node["RequestGeometry"] = false;
		if (maxHours.HasValue)
			node["TotalHours"] = maxHours.Value;
		if (mode == BehaviorRunMode.Legacy)
			node.Remove("SpeciesGraphs");
		var prepared = node.Deserialize<SimulationRequest>(JsonOptions)
			?? throw new InvalidOperationException("Failed to clone simulation request.");
		return prepared;
	}

	public static void RecordTrace(
		SimulationRequest settings,
		BehaviorRunMode mode,
		string outputPath,
		int? maxHours = null)
	{
		var prepared = PrepareForParity(settings, mode, maxHours);
		var world = Initialize.World(prepared);
		var steps = (uint)world.TimestepsTotal();
		var header = new SimulationTraceHeader
		{
			Mode = mode.ToString().ToLowerInvariant(),
			Seed = prepared.Seed,
			HoursPerTick = prepared.HoursPerTick,
			TotalHours = prepared.TotalHours,
		};

		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
		using var writer = new StreamWriter(outputPath, append: false);
		writer.WriteLine(JsonSerializer.Serialize(header, JsonOptions));

		world.Run(steps, (_, timestep) =>
		{
			var step = PlantStateSnapshot.Capture(world, timestep);
			writer.WriteLine(JsonSerializer.Serialize(step, JsonOptions));
		});
	}

	public static SimulationTraceHeader ReadHeader(string path)
	{
		using var reader = new StreamReader(path);
		var line = reader.ReadLine() ?? throw new InvalidDataException("Empty trace file.");
		return JsonSerializer.Deserialize<SimulationTraceHeader>(line, JsonOptions)
			?? throw new InvalidDataException("Invalid trace header.");
	}

	public static IEnumerable<StepSnapshot> ReadSteps(string path)
	{
		using var reader = new StreamReader(path);
		_ = reader.ReadLine();
		string? line;
		while ((line = reader.ReadLine()) != null)
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;
			yield return JsonSerializer.Deserialize<StepSnapshot>(line, JsonOptions)
				?? throw new InvalidDataException("Invalid trace step line.");
		}
	}
}
