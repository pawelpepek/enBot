using enBot.Models;

namespace enBot.Services.AgentCli;

public static class AgentCliProcessorFactory
{
    public static IAgentCliProcessor Create(AnalysisProvider provider) => provider switch
    {
        AnalysisProvider.Codex => new CodexCliProcessor(),
        _                      => new ClaudeCliProcessor()
    };
}
