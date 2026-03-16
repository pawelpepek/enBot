using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace enBot.Models;

public record AnalysisResult
{
    [JsonPropertyName("displayOriginal")]
    public string DisplayOriginal { get; init; }

    [JsonPropertyName("corrected")]
    public string Corrected { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("complexity")]
    public int Complexity { get; init; }

    [JsonPropertyName("language")]
    public string Language { get; init; }

    [JsonPropertyName("explanations")]
    public List<string> Explanations { get; init; }
}
