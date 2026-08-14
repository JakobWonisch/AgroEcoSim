using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Agro.BehaviorGraph;

namespace Agro.Testing;

public enum BehaviorRunMode
{
	Legacy,
	Node,
}

public static class SimulationHarness
{
	public static SimulationRequest PrepareForParity(SimulationRequest source, BehaviorRunMode mode, int? maxHours = null)
		=> PrepareForParity(source, mode, maxHours, includeGraphNames: null, plantRngFixedUnit: null, plantPosition: null);

	public static SimulationRequest PrepareForParity(
		SimulationRequest source,
		BehaviorRunMode mode,
		int? maxHours,
		IReadOnlyCollection<string>? includeGraphNames,
		float? plantRngFixedUnit = null,
		Utils.Json.Vector3XYZ? plantPosition = null)
	{
		var node = JsonSerializer.SerializeToNode(source, AgroJsonSerializerContext.Default.SimulationRequest)!.AsObject();
		node["RenderMode"] = 0;
		node["RequestGeometry"] = false;
		if (maxHours.HasValue)
			node["TotalHours"] = maxHours.Value;
		if (plantRngFixedUnit.HasValue)
			node["PlantRngFixedUnit"] = plantRngFixedUnit.Value;

		if (plantPosition.HasValue && node["Plants"] is JsonArray plants)
		{
			foreach (var plantNode in plants)
			{
				if (plantNode is not JsonObject plantObj)
					continue;
				plantObj["P"] = new JsonObject
				{
					["X"] = plantPosition.Value.X,
					["Y"] = plantPosition.Value.Y,
					["Z"] = plantPosition.Value.Z,
				};
			}
		}

		if (mode == BehaviorRunMode.Legacy)
			node.Remove("SpeciesGraphs");
		else if (includeGraphNames is { Count: > 0 } && node["SpeciesGraphs"] is JsonObject speciesGraphs)
			FilterSpeciesGraphs(speciesGraphs, includeGraphNames);

		var prepared = node.Deserialize(AgroJsonSerializerContext.Default.SimulationRequest)
			?? throw new InvalidOperationException("Failed to clone simulation request.");
		return prepared;
	}

	static void FilterSpeciesGraphs(JsonObject speciesGraphs, IReadOnlyCollection<string> includeGraphNames)
	{
		var allowed = new HashSet<string>(includeGraphNames, StringComparer.OrdinalIgnoreCase);
		foreach (var prop in speciesGraphs.ToList())
		{
			if (prop.Value is not JsonArray arr)
				continue;
			var kept = new JsonArray();
			foreach (var item in arr)
			{
				var name = item?["Name"]?.GetValue<string>();
				if (name != null && allowed.Contains(name))
					kept.Add(item!.DeepClone());
			}
			speciesGraphs[prop.Key] = kept;
		}
	}

	public static void RecordTrace(
		SimulationRequest settings,
		BehaviorRunMode mode,
		string outputPath,
		int? maxHours = null)
	{
		var prepared = PrepareForParity(settings, mode, maxHours);
		var world = Initialize.World(prepared);
		WriteTrace(world, prepared, mode, outputPath);
	}

	/// <summary>
	/// Record a JSONL trace for a plant that contains exactly one above-ground agent of the
	/// configured organ type. Spawn/death are mocked so the agent count stays at one.
	/// </summary>
	public static void RecordSingleAgentTrace(
		SimulationRequest settings,
		BehaviorRunMode mode,
		string outputPath,
		AgentTypeParityOptions agentTypeOptions)
	{
		var prepared = PrepareForParity(
			settings,
			mode,
			agentTypeOptions.MaxHours,
			agentTypeOptions.IncludeGraphNames,
			agentTypeOptions.PlantRngFixedUnit,
			agentTypeOptions.PlantPosition);

		var world = Initialize.World(prepared);

		world.ForEach(formation =>
		{
			if (formation is not PlantFormation2 plant)
				return;

			var (length, radius) = DefaultDimensions(agentTypeOptions);
			var agent = new AboveGroundAgent(
				plant,
				parent: 0,
				organ: agentTypeOptions.Organ,
				orientation: Quaternion.Identity,
				initialEnergy: agentTypeOptions.InitialEnergy,
				radius: radius,
				length: length,
				initialResources: 1f,
				initialProduction: 1f)
			{
				Water_g = agentTypeOptions.InitialWater_g,
				DominanceLevel = 1,
			};

			plant.InstallSingleOrganSceneForTesting(agent);
		});

		// Enable mocks after scene install so UG root Birth is not suppressed.
		world.ParitySuppressSpawn = agentTypeOptions.SuppressSpawn;
		world.ParitySuppressDeath = agentTypeOptions.SuppressDeath;

		WriteTrace(world, prepared, mode, outputPath);
	}

	/// <summary>Subject organ is always index 1 (child of the rhizome scaffold at 0).</summary>
	public const int SingleAgentSubjectIndex = 1;

	static (float Length, float Radius) DefaultDimensions(AgentTypeParityOptions options)
	{
		if (options.InitialLength is float len && options.InitialRadius is float rad)
			return (len, rad);

		var (defaultLen, defaultRad) = options.Organ switch
		{
			OrganTypes.Leaf => (DefaultSpeciesGraphBuilder.DefaultTickConstants.LeafLength * 0.25f,
				DefaultSpeciesGraphBuilder.DefaultTickConstants.LeafRadius * 0.25f),
			OrganTypes.Petiole => (DefaultSpeciesGraphBuilder.DefaultTickConstants.PetioleLength * 0.5f,
				DefaultSpeciesGraphBuilder.DefaultTickConstants.PetioleRadius),
			OrganTypes.Stem => (0.02f, 0.0025f),
			OrganTypes.Meristem => (0.01f, 0.0025f),
			OrganTypes.Bud => (0.005f, 0.0025f),
			_ => (0.01f, 0.0025f),
		};
		return (options.InitialLength ?? defaultLen, options.InitialRadius ?? defaultRad);
	}

	static void WriteTrace(AgroWorld world, SimulationRequest prepared, BehaviorRunMode mode, string outputPath)
	{
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

	/// <summary>
	/// Run legacy + node single-agent traces for one organ and return the first mismatch (or null).
	/// </summary>
	public static TraceMismatch? CompareSingleAgentParity(
		SimulationRequest settings,
		AgentTypeParityOptions agentTypeOptions,
		TraceCompareOptions? compareOptions = null,
		string? legacyPath = null,
		string? nodePath = null)
	{
		var deleteLegacy = legacyPath is null;
		var deleteNode = nodePath is null;
		legacyPath ??= Path.Combine(Path.GetTempPath(), $"agro-legacy-single-{Guid.NewGuid():N}.jsonl");
		nodePath ??= Path.Combine(Path.GetTempPath(), $"agro-node-single-{Guid.NewGuid():N}.jsonl");
		try
		{
			RecordSingleAgentTrace(settings, BehaviorRunMode.Legacy, legacyPath, agentTypeOptions);
			RecordSingleAgentTrace(settings, BehaviorRunMode.Node, nodePath, agentTypeOptions);

			compareOptions ??= TraceCompareOptions.StructuralParity
				.WithFocusAboveGroundIndices(SingleAgentSubjectIndex);

			return TraceComparer.CompareFiles(legacyPath, nodePath, compareOptions);
		}
		finally
		{
			if (deleteLegacy && File.Exists(legacyPath)) File.Delete(legacyPath);
			if (deleteNode && File.Exists(nodePath)) File.Delete(nodePath);
		}
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
