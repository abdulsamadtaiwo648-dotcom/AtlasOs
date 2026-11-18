using System.Text.Json.Serialization;

namespace Atlas.AI.Models;

public class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("options")]
    public Dictionary<string, object>? Options { get; set; }
}

public class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }
}
