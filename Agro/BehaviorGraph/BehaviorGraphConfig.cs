namespace Agro.BehaviorGraph;

static class BehaviorGraphConfig
{
	public static bool TryNumber(
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config,
		string configId,
		out float value)
	{
		value = 0f;
		if (config is null || !config.TryGetValue(configId, out var entry))
			return false;
		if (entry.IsBoolean || entry.IsNumberArray)
			return false;
		value = entry.NumberValue;
		return true;
	}

	public static bool TryArrayElement(
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config,
		string configId,
		float index,
		out float value)
	{
		value = 0f;
		if (config is null || !config.TryGetValue(configId, out var entry) || !entry.IsNumberArray)
			return false;
		var arr = entry.FloatArrayValue;
		if (arr.Length == 0)
			return false;
		var i = (int)MathF.Floor(index);
		if (i < 0)
			i = 0;
		if (i >= arr.Length)
			i = arr.Length - 1;
		value = arr[i];
		return true;
	}

	public static float Number(IReadOnlyDictionary<string, BehaviorConfigEntry>? config, string configId, float fallback = 0f)
		=> TryNumber(config, configId, out var value) ? value : fallback;

	/// <summary>Indexed lookup with floor + clamp; empty array returns <paramref name="fallback"/>.</summary>
	public static float ArrayElement(
		IReadOnlyDictionary<string, BehaviorConfigEntry>? config,
		string configId,
		float index,
		float fallback = 0f)
		=> TryArrayElement(config, configId, index, out var value) ? value : fallback;
}
