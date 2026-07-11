using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ExportedGraph
{
    [JsonPropertyName("nodes")]
    public required List<GraphNode> Nodes { get; set; }

    [JsonPropertyName("connections")]
    public required List<GraphConnection> Connections { get; set; }
}

public class GraphNode
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("label")]
    public required string Label { get; set; }

    // `any` can be cast as `object`, or as `JsonElement` / `JsonNode` if you need to inspect its properties dynamically after parsing.
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("position")]
    public required NodePosition Position { get; set; }
}

public class NodePosition
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}

public class GraphConnection
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("sourceOutput")]
    public required string SourceOutput { get; set; }

    [JsonPropertyName("target")]
    public required string Target { get; set; }

    [JsonPropertyName("targetInput")]
    public required string TargetInput { get; set; }
}
