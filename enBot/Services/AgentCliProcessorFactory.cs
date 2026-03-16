namespace enBot.Services;

public static class AgentCliProcessorFactory
{
    public static IAgentCliProcessor Create(string provider) => provider switch
    {
        "codex" => new CodexCliProcessor(),
        _       => new ClaudeCliProcessor()
    };
}
