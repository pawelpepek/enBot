namespace enBot.Models;

public enum AppStateKey
{
    PromptsSinceLastSuggestion = 0,
}

public class AppStateEntry
{
    public AppStateKey Key { get; set; }
    public int Value { get; set; }
}
