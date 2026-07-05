using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agro;

/// <summary>One shared configuration value for a species behavior graph (wire format).</summary>
public sealed class BehaviorConfigUploadEntry
{
	[JsonPropertyName("Id")]
	public required string Id { get; init; }

	[JsonPropertyName("Key")]
	public required string Key { get; init; }

	[JsonPropertyName("Label")]
	public required string Label { get; init; }

	[JsonPropertyName("Type")]
	public required string Type { get; init; }

	[JsonPropertyName("Value")]
	public JsonElement Value { get; init; }
}

/// <summary>Resolved configuration entry used at simulation runtime.</summary>
public sealed class BehaviorConfigEntry
{
	public required string Id { get; init; }
	public required string Key { get; init; }
	public required string Label { get; init; }
	public bool IsBoolean { get; init; }
	public float NumberValue { get; init; }
	public bool BoolValue { get; init; }
}

public static class BehaviorConfigurationCatalog
{
	public static Dictionary<string, BehaviorConfigEntry> ParseSpeciesConfiguration(
		Dictionary<string, List<BehaviorConfigUploadEntry>>? speciesConfiguration,
		string? speciesName)
	{
		var result = new Dictionary<string, BehaviorConfigEntry>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(speciesName) || speciesConfiguration is null)
			return result;

		if (!speciesConfiguration.TryGetValue(speciesName, out var entries))
		{
			var match = speciesConfiguration.Keys
				.FirstOrDefault(k => string.Equals(k, speciesName, StringComparison.OrdinalIgnoreCase));
			if (match is null || !speciesConfiguration.TryGetValue(match, out entries))
				return result;
		}

		foreach (var entry in entries)
		{
			if (string.IsNullOrWhiteSpace(entry.Id))
				continue;
			var isBool = string.Equals(entry.Type, "boolean", StringComparison.OrdinalIgnoreCase);
			result[entry.Id] = new BehaviorConfigEntry
			{
				Id = entry.Id,
				Key = string.IsNullOrWhiteSpace(entry.Key) ? entry.Id : entry.Key.Trim(),
				Label = entry.Label ?? "",
				IsBoolean = isBool,
				NumberValue = isBool ? 0f : ReadNumberValue(entry.Value),
				BoolValue = isBool && ReadBoolValue(entry.Value),
			};
		}

		return result;
	}

	static float ReadNumberValue(JsonElement value) =>
		value.ValueKind switch
		{
			JsonValueKind.Number => value.TryGetSingle(out var f) ? f : 0f,
			JsonValueKind.True => 1f,
			JsonValueKind.False => 0f,
			_ => 0f,
		};

	static bool ReadBoolValue(JsonElement value) =>
		value.ValueKind switch
		{
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			JsonValueKind.Number => Math.Abs(value.GetSingle()) > 1e-6f,
			_ => false,
		};
}
