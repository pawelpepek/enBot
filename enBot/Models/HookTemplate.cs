using System;

namespace enBot.Models;

public class HookTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsActive { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
