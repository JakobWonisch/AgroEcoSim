namespace Agro.BehaviorGraph;

static class BehaviorGraphConfig
{
	public static float Number(IReadOnlyDictionary<string, BehaviorConfigEntry>? config, string configId, float fallback = 0f)
	{
		if (config is null || !config.TryGetValue(configId, out var entry))
			return fallback;
		return entry.NumberValue;
	}
}
