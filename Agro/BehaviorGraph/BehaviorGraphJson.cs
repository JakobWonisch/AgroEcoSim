using System.Text.Json;

namespace Agro.BehaviorGraph;

internal static class BehaviorGraphJson
{
	public static JsonElement Number(float value) =>
		JsonSerializer.SerializeToElement(value, AgroJsonSerializerContext.Default.Single);

	public static JsonElement Bool(bool value) =>
		JsonSerializer.SerializeToElement(value, AgroJsonSerializerContext.Default.Boolean);

	public static JsonElement NumberArray(float[] values) =>
		JsonSerializer.SerializeToElement(values, AgroJsonSerializerContext.Default.SingleArray);

	public static JsonElement EmptyObject { get; } =
		JsonSerializer.SerializeToElement(
			new Dictionary<string, object>(),
			AgroJsonSerializerContext.Default.DictionaryStringObject);

	public static JsonElement BoolNodeData(bool value) =>
		JsonSerializer.SerializeToElement(
			new Dictionary<string, bool> { ["bool"] = value },
			AgroJsonSerializerContext.Default.DictionaryStringBoolean);
}
