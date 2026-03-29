using System;

namespace enBot.Models;

public class PromptSuggestion
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string SuggestionText { get; set; } = "";
    public string ExplanationText { get; set; } = "";
}
