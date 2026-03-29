namespace enBot.Models;

public enum AppConfigKey
{
    UserProfile = 0,
}

public class AppConfigEntry
{
    public AppConfigKey Key { get; set; }
    public string Value { get; set; } = "";
}
