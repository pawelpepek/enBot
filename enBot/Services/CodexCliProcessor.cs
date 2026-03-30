using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace enBot.Services;

public class CodexCliProcessor : IAgentCliProcessor
{
    public string Name => "codex";

    public ProcessStartInfo GetProcessStartInfo(string prompt)
    {
        var psi = new ProcessStartInfo(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "npm",
                "codex.cmd"
            ),
            "exec --sandbox read-only --skip-git-repo-check -")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8
        };
        return psi;
    }
}
