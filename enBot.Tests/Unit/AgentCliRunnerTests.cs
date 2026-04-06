using enBot.Services.AgentCli;
using System.Diagnostics;

namespace enBot.Tests.Unit;

public class AgentCliRunnerTests
{
    private sealed class FakeCliProcessor(ProcessStartInfo psi) : IAgentCliProcessor
    {
        public string Name => "fake";
        public ProcessStartInfo GetProcessStartInfo(string prompt) => psi;
    }

    private static ProcessStartInfo EchoPsi(string text) => new ProcessStartInfo("cmd", $"/c echo {text}")
    {
        RedirectStandardInput = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    [Fact]
    public async Task RunAsync_ReturnsStdout()
    {
        var runner = new AgentCliRunner(new FakeCliProcessor(EchoPsi("enbot_test_marker")));
        var result = await runner.RunAsync("any prompt");
        Assert.Contains("enbot_test_marker", result);
    }

    [Fact]
    public async Task RunAsync_SetsEnbotAnalysisEnvVar()
    {
        var runner = new AgentCliRunner(new FakeCliProcessor(EchoPsi("%ENBOT_ANALYSIS%")));
        var result = await runner.RunAsync("any");
        Assert.Contains("1", result);
    }

    [Fact]
    public async Task RunAsync_WritesPromptToStdin()
    {
        var psi = new ProcessStartInfo("powershell", "-NoProfile -NonInteractive -Command \"[Console]::In.ReadToEnd()\"")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };
        var runner = new AgentCliRunner(new FakeCliProcessor(psi));
        var result = await runner.RunAsync("hello from test");
        Assert.Contains("hello from test", result);
    }

    [Fact]
    public async Task RunAsync_ThrowsWhenExecutableNotFound()
    {
        var psi = new ProcessStartInfo("nonexistent_cli_xyz_abc")
        {
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var runner = new AgentCliRunner(new FakeCliProcessor(psi));
        await Assert.ThrowsAnyAsync<Exception>(() => runner.RunAsync("any"));
    }


    [Fact]
    public void ExtractJson_MarkdownFence_ReturnsInnerJson()
    {
        var input = "Some text\n```json\n{\"score\":8}\n```\nMore text";
        Assert.Equal("{\"score\":8}", AgentCliRunner.ExtractJson(input));
    }

    [Fact]
    public void ExtractJson_MarkdownFenceNoLang_ReturnsInnerJson()
    {
        var input = "```\n{\"score\":8}\n```";
        Assert.Equal("{\"score\":8}", AgentCliRunner.ExtractJson(input));
    }

    [Fact]
    public void ExtractJson_RawJson_ReturnsBraceContent()
    {
        var input = "Here is the result: {\"score\":8} done.";
        Assert.Equal("{\"score\":8}", AgentCliRunner.ExtractJson(input));
    }

    [Fact]
    public void ExtractJson_NestedBraces_ReturnsOutermostObject()
    {
        var input = "{\"explanations\":{\"a\":\"b\"}}";
        Assert.Equal("{\"explanations\":{\"a\":\"b\"}}", AgentCliRunner.ExtractJson(input));
    }

    [Fact]
    public void ExtractJson_NoJsonMarkers_ReturnsInputAsIs()
    {
        var input = "plain text with no braces";
        Assert.Equal(input, AgentCliRunner.ExtractJson(input));
    }

    [Fact]
    public void ExtractJson_EmptyString_ReturnsEmptyString()
    {
        Assert.Equal("", AgentCliRunner.ExtractJson(""));
    }

    [Fact]
    public void ExtractJson_FencePreferredOverBraces()
    {
        // Fence should win even when raw braces also exist outside
        var input = "prefix {\"wrong\":1} ```json\n{\"right\":2}\n``` suffix";
        Assert.Equal("{\"right\":2}", AgentCliRunner.ExtractJson(input));
    }

    [Fact]
    public void ExtractJson_OnlyOpenBrace_ReturnsInputAsIs()
    {
        var input = "no closing brace {here";
        Assert.Equal(input, AgentCliRunner.ExtractJson(input));
    }
}
