using System.Threading.Tasks;

namespace enBot.Services.AgentCli;

public interface IAgentCliRunner
{
    Task<string> RunAsync(string prompt);
}
