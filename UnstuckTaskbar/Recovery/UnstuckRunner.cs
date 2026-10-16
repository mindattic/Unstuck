using System.Diagnostics;
using System.IO;

namespace UnstuckTaskbar.Recovery;

public sealed record RecoveryResult(string Summary, string RawOutput, int ExitCode);

/// <summary>
/// Shells out to the deployed <c>unstuck.ps1</c> (always the plain default recovery, no switches —
/// the same thing the plain <c>/unstuck</c> Claude Code command runs) and turns its existing
/// "---- Report ----" text output into a short balloon-tip summary. <c>unstuck.ps1</c> itself is
/// never changed for this.
/// </summary>
public sealed class UnstuckRunner
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2);
    private const int MaxSummaryLength = 240;

    private DateTime? lastRunUtc;

    /// <summary>
    /// UnstuckTaskbar.exe only ever runs from its deployed location (<c>C:\Apps\Unstuck\</c>), where
    /// deploy.ps1 always places unstuck.ps1 alongside it — so no source-repo fallback is needed.
    /// </summary>
    public static string ScriptPath => Path.Combine(AppContext.BaseDirectory, "unstuck.ps1");

    /// <summary>Runs the recovery, or returns null if a run completed less than <see cref="Cooldown"/> ago
    /// (guards against a mashed or OS-repeated hotkey stacking powershell.exe processes).</summary>
    public RecoveryResult? Run()
    {
        var now = DateTime.UtcNow;
        if (lastRunUtc is { } last && now - last < Cooldown) return null;
        lastRunUtc = now;

        var scriptPath = ScriptPath;
        if (!File.Exists(scriptPath))
            return new RecoveryResult($"unstuck.ps1 not found at {scriptPath} - run deploy.ps1.", RawOutput: "", ExitCode: -1);

        var psi = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(scriptPath);

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new RecoveryResult(ExtractSummary(output), output, process.ExitCode);
    }

    /// <summary>Pure text parsing of unstuck.ps1's own "---- Report ----" section, for the balloon tip.
    /// Internal (not private) so it's directly unit-testable against captured sample output.</summary>
    internal static string ExtractSummary(string output)
    {
        var lines = output.Replace("\r\n", "\n").Split('\n');

        var gamepad = lines.FirstOrDefault(l => l.TrimStart().StartsWith("STUCK GAMEPAD INPUT", StringComparison.Ordinal));
        if (gamepad is not null) return Truncate(gamepad.Trim());

        string? keysStillHeld = null;
        var actions = new List<string>();
        var inActions = false;
        foreach (var line in lines)
        {
            if (line.StartsWith("Keys still held:", StringComparison.Ordinal))
            {
                keysStillHeld = line["Keys still held:".Length..].Trim();
                continue;
            }
            if (line.StartsWith("Actions:", StringComparison.Ordinal)) { inActions = true; continue; }
            if (inActions)
            {
                if (!line.StartsWith("  ", StringComparison.Ordinal)) { inActions = false; continue; }
                var action = line.Trim();
                if (!string.IsNullOrEmpty(action) && !string.Equals(action, "none", StringComparison.OrdinalIgnoreCase))
                    actions.Add(action);
            }
        }

        var summary = actions.Count > 0
            ? string.Join("; ", actions)
            : "No stuck input or runaway processes found.";

        if (!string.IsNullOrEmpty(keysStillHeld) && !string.Equals(keysStillHeld, "none", StringComparison.OrdinalIgnoreCase))
            summary += $" Keys still held: {keysStillHeld}.";

        return Truncate(summary);
    }

    private static string Truncate(string text) =>
        text.Length <= MaxSummaryLength ? text : text[..(MaxSummaryLength - 1)] + "\u2026";
}
