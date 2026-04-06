using enBot.Services.AgentCli;
using System.Threading.Tasks;

namespace enBot.Tests;

public class TestCliProcessor : IAgentCliRunner
{
    private readonly string _response;
    private readonly Exception _throws;

    public string CapturedPrompt { get; private set; }

    public TestCliProcessor(string response) => _response = response;
    public TestCliProcessor(Exception throws) => _throws = throws;

    public Task<string> RunAsync(string prompt)
    {
        CapturedPrompt = prompt;
        if (_throws is not null) throw _throws;
        return Task.FromResult(_response!);
    }
}
