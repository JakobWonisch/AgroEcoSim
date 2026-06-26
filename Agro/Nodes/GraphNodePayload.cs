using System.Collections.Generic;
using System.Text.Json;

/// <summary>Serializable <see cref="GraphNode.Data"/> fields shared with the behavior graph editor.</summary>
public sealed class GraphNodePayload
{
	public float? Value { get; init; }
	public bool? Bool { get; init; }
	public string? Comment { get; init; }

	public static GraphNodePayload FromNumber(float value, string? comment = null) =>
		new() { Value = value, Comment = comment };

	public static GraphNodePayload FromBool(bool value, string? comment = null) =>
		new() { Bool = value, Comment = comment };

	public static GraphNodePayload FromComment(string comment) =>
		new() { Comment = comment };

	public JsonElement ToJsonElement()
	{
		var dict = new Dictionary<string, object>();
		if (Value is float v)
			dict["value"] = v;
		if (Bool is bool b)
			dict["bool"] = b;
		if (!string.IsNullOrWhiteSpace(Comment))
			dict["comment"] = Comment.Trim();
		return JsonSerializer.SerializeToElement(dict);
	}
}
