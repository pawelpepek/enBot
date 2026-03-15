using System.Text.Json.Serialization;

namespace enBot.Models;

public record RawPrompt
{
    [JsonPropertyName("original")]
    public string Original { get; init; } = "";
}
