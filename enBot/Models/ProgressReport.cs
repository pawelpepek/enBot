using System;

namespace enBot.Models;

public class ProgressReport
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string ReportText { get; set; } = "";
    public int SincePromptId { get; set; }
}
