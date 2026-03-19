using System.Text.Json.Serialization;

namespace enBot.Models;

[JsonConverter(typeof(JsonStringEnumConverter<AnalysisProvider>))]
public enum AnalysisProvider
{
    [JsonStringEnumMemberName("claude")] Claude,
    [JsonStringEnumMemberName("codex")] Codex
}
