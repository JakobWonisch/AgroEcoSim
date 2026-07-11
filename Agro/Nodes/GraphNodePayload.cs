using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Serializable <see cref="GraphNode.Data"/> fields shared with the behavior graph editor.</summary>
public sealed class GraphNodePayload
{
	[JsonPropertyName("value")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public float? Value { get; init; }

	[JsonPropertyName("bool")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Bool { get; init; }

	[JsonPropertyName("comment")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Comment { get; init; }

	[JsonPropertyName("configId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ConfigId { get; init; }

	[JsonPropertyName("configType")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ConfigType { get; init; }

	public static GraphNodePayload FromNumber(float value, string? comment = null) =>
		new() { Value = value, Comment = NormalizeComment(comment) };

	public static GraphNodePayload FromBool(bool value, string? comment = null) =>
		new() { Bool = value, Comment = NormalizeComment(comment) };

	public static GraphNodePayload FromComment(string comment) =>
		new() { Comment = NormalizeComment(comment) ?? comment.Trim() };

	public static GraphNodePayload FromConfig(string configId, bool isBoolean, string? comment = null) =>
		new()
		{
			ConfigId = configId,
			ConfigType = isBoolean ? "boolean" : "number",
			Comment = NormalizeComment(comment),
		};

	public static GraphNodePayload FromConfigArray(string configId, string? comment = null) =>
		new()
		{
			ConfigId = configId,
			ConfigType = "number[]",
			Comment = NormalizeComment(comment),
		};

	static string? NormalizeComment(string? comment) =>
		string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

	public JsonElement ToJsonElement() =>
		JsonSerializer.SerializeToElement(this, AgroJsonSerializerContext.Default.GraphNodePayload);
}
