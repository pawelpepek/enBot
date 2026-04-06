using System.Diagnostics;

namespace enBot.Services.AgentCli;

public interface IAgentCliProcessor
{
    string Name { get; }
    ProcessStartInfo GetProcessStartInfo(string prompt);
}
