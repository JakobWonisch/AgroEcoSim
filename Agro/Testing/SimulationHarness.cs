using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agro.Testing;

public enum BehaviorRunMode
{
	Legacy,
	Node,
}

public static class SimulationHarness
{
	public static SimulationRequest PrepareForParity(SimulationRequest source, BehaviorRunMode mode, int? maxHours = null)
	{
		var node = JsonSerializer.SerializeToNode(source, AgroJsonSerializerContext.Default.SimulationRequest)!.AsObject();
		node["RenderMode"] = 0;
		node["RequestGeometry"] = false;
		if (maxHours.HasValue)
			node["TotalHours"] = maxHours.Value;
		if (mode == BehaviorRunMode.Legacy)
			node.Remove("SpeciesGraphs");
		var prepared = node.Deserialize(AgroJsonSerializerContext.Default.SimulationRequest)
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
		writer.WriteLine(JsonSerializer.Serialize(header, AgroJsonSerializerContext.Default.SimulationTraceHeader));

		world.Run(steps, (_, timestep) =>
		{
			var step = PlantStateSnapshot.Capture(world, timestep);
			writer.WriteLine(JsonSerializer.Serialize(step, AgroJsonSerializerContext.Default.StepSnapshot));
		});
	}

	public static SimulationTraceHeader ReadHeader(string path)
	{
		using var reader = new StreamReader(path);
		var line = reader.ReadLine() ?? throw new InvalidDataException("Empty trace file.");
		return JsonSerializer.Deserialize(line, AgroJsonSerializerContext.Default.SimulationTraceHeader)
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
			yield return JsonSerializer.Deserialize(line, AgroJsonSerializerContext.Default.StepSnapshot)
				?? throw new InvalidDataException("Invalid trace step line.");
		}
	}
}
