using System.Diagnostics;

namespace enBot.Services;

public interface IAgentCliProcessor
{
    string Name { get; }
    ProcessStartInfo GetProcessStartInfo(string prompt);
}
