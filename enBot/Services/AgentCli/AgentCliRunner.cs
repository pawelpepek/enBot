using enBot.Services.Infrastructure;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace enBot.Services.AgentCli;

public class AgentCliRunner(IAgentCliProcessor processor) : IAgentCliRunner
{
    public async Task<string> RunAsync(string prompt)
    {
        var psi = processor.GetProcessStartInfo(prompt);
        psi.Environment["ENBOT_ANALYSIS"] = "1";

        using var process = Process.Start(psi);
        if (process is null)
            throw new InvalidOperationException($"{processor.Name} not found on PATH.");

        if (psi.RedirectStandardInput)
        {
            await process.StandardInput.WriteAsync(prompt).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(outputTask, stderrTask).ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        var output = outputTask.Result;
        var stderr = stderrTask.Result;

        LogService.Log($"[AgentCli] {processor.Name} exited {process.ExitCode}, stdout {output.Length} chars");
        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
            LogService.Log($"[AgentCli] stderr: {stderr.Trim()}");

        return output;
    }

    public static string ExtractJson(string text)
    {
        var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
        if (match.Success) return match.Groups[1].Value.Trim();

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text[start..(end + 1)];

        return text;
    }
}
