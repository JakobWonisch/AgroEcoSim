namespace Agro.BehaviorGraph;

static class MorphologyGraphInputs
{
	public static LeafLayoutParams ReadLeafLayout(
		CompiledNode node,
		Dictionary<(int, string), WireValue> outs,
		TickEvalContext ctx)
	{
		var n = (string id) => InputOrConfig(node, outs, ctx, id, id);
		return new LeafLayoutParams(
			(int)MathF.Round(n("lateralsPerNode")),
			n("lateralRollVar"),
			n("lateralPitchVar"),
			n("lateralPitch"),
			n("leafPitch"));
	}

	public static TwigOrientationParams ReadTwigOrientation(
		CompiledNode node,
		Dictionary<(int, string), WireValue> outs,
		TickEvalContext ctx)
	{
		var n = (string id) => InputOrConfig(node, outs, ctx, id, id);
		return new TwigOrientationParams(
			n("twigsBending"),
			n("twigsBendingLevel"),
			n("twigsBendingApical"),
			n("shootsGravitaxis"));
	}

	public static float ReadLateralRoll(CompiledNode node, Dictionary<(int, string), WireValue> outs, TickEvalContext ctx)
		=> InputOrConfig(node, outs, ctx, "lateralRoll", "lateralRoll");

	public static RhizomeSpawnParams ReadRhizomeSpawn(
		CompiledNode node,
		Dictionary<(int, string), WireValue> outs,
		TickEvalContext ctx)
	{
		var n = (string id) => InputOrConfig(node, outs, ctx, id, id);
		return new RhizomeSpawnParams(n("rizomeLength"), n("rizomeRadius"), n("lateralRoll"));
	}

	static float InputOrConfig(
		CompiledNode node,
		Dictionary<(int, string), WireValue> outs,
		TickEvalContext ctx,
		string inputKey,
		string configId)
	{
		if (node.Inputs.TryGetValue(inputKey, out var list) && list is { Count: > 0 })
			return GraphTickInterpreter.FirstFloatPublic(node.Inputs, inputKey, outs);
		return GraphTickInterpreter.ResolveConfigNumberPublic(MapInputToConfigId(inputKey, configId), ctx);
	}

	static string MapInputToConfigId(string inputKey, string fallback) => inputKey switch
	{
		"lateralsPerNode" => DefaultSpeciesGraphBuilder.ConfigIds.LateralsPerNode,
		"lateralRollVar" => DefaultSpeciesGraphBuilder.ConfigIds.LateralRollVar,
		"lateralPitchVar" => DefaultSpeciesGraphBuilder.ConfigIds.LateralPitchVar,
		"lateralPitch" => DefaultSpeciesGraphBuilder.ConfigIds.LateralPitch,
		"leafPitch" => DefaultSpeciesGraphBuilder.ConfigIds.LeafPitch,
		"lateralRoll" => DefaultSpeciesGraphBuilder.ConfigIds.LateralRoll,
		"twigsBending" => DefaultSpeciesGraphBuilder.ConfigIds.TwigsBending,
		"twigsBendingLevel" => DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingLevel,
		"twigsBendingApical" => DefaultSpeciesGraphBuilder.ConfigIds.TwigsBendingApical,
		"shootsGravitaxis" => DefaultSpeciesGraphBuilder.ConfigIds.ShootsGravitaxis,
		"rizomeLength" => DefaultSpeciesGraphBuilder.ConfigIds.RizomeLength,
		"rizomeRadius" => DefaultSpeciesGraphBuilder.ConfigIds.RizomeRadius,
		_ => fallback,
	};
}
