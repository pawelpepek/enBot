using System.Diagnostics;

namespace enBot.Services;

public class ClaudeCliProcessor : IAgentCliProcessor
{
    public string Name => "claude";

    public ProcessStartInfo GetProcessStartInfo(string prompt)
    {
        var psi = new ProcessStartInfo(Name)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };
        psi.ArgumentList.Add("--print");
        psi.Environment["ENBOT_ANALYSIS"] = "1";
        return psi;
    }
}
