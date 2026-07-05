namespace Agro.BehaviorGraph;

static class BehaviorGraphConfig
{
	public static float Number(IReadOnlyDictionary<string, BehaviorConfigEntry>? config, string configId, float fallback = 0f)
	{
		if (config is null || !config.TryGetValue(configId, out var entry))
			return fallback;
		return entry.NumberValue;
	}

	/// <summary>Indexed lookup with floor + clamp; empty array returns <paramref name="fallback"/>.</summary>
	public static float ArrayElement(
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config,
		string configId,
		float index,
		float fallback = 0f)
	{
		if (config is null || !config.TryGetValue(configId, out var entry))
			return fallback;
		var arr = entry.FloatArrayValue;
		if (arr.Length == 0)
			return fallback;
		var i = (int)MathF.Floor(index);
		if (i < 0)
			i = 0;
		if (i >= arr.Length)
			i = arr.Length - 1;
		return arr[i];
	}
}
