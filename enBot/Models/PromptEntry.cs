using System;

namespace enBot.Models;

public class PromptEntry
{
    public int Id { get; set; }

    public string Original { get; set; } = "";
    public string Corrected { get; set; } = "";
    public int Score { get; set; }
    public int Complexity { get; set; }
    public int WordCount { get; set; }
    public string ExplanationsJson { get; set; }
    public string HookVersion { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
