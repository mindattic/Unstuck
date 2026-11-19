using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace UnstuckTaskbar.Recovery;

/// <summary>The outcome of one recovery run.</summary>
/// <param name="Summary">One short line for the balloon tip.</param>
/// <param name="RawOutput">The script's full report.</param>
/// <param name="ExitCode">PowerShell's exit code; -1 when it couldn't start or was stopped.</param>
/// <param name="NeedsAttention">True when the user has to do something the script couldn't (a stuck gamepad
/// button, a key still held) or the run itself failed.</param>
public sealed record RecoveryResult(string Summary, string RawOutput, int ExitCode, bool NeedsAttention = false);

/// <summary>The optional switches of <c>unstuck.ps1</c>, as offered in the tray's "Run with" menu.</summary>
[Flags]
public enum RecoveryOptions
{
    None = 0,
    /// <summary><c>-Check</c>: report only, change nothing.</summary>
    Check = 1,
    /// <summary><c>-Explorer</c>: force-restart Explorer.</summary>
    Explorer = 2,
    /// <summary><c>-Hooks</c>: also restart PowerToys.</summary>
    Hooks = 4,
    /// <summary><c>-Overlays</c>: also close overlay apps.</summary>
    Overlays = 8,
}

/// <summary>
/// Shells out to the deployed <c>unstuck.ps1</c> (the same script the <c>/unstuck</c> Claude Code command runs)
/// and turns its "---- Report ----" text output into a short balloon-tip summary.
/// </summary>
public sealed class UnstuckRunner
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    /// <summary>A normal run takes 3-5 s; past this the script itself is stuck and is stopped.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    private const int MaxSummaryLength = 240;

    private bool running;
    private DateTime? lastFinishedUtc;

    /// <summary>True while a run is in progress.</summary>
    public bool IsRunning => running;

    /// <summary>
    /// UnstuckTaskbar.exe only ever runs from its deployed location (<c>C:\Apps\Unstuck\</c>), where
    /// run.ps1 always places unstuck.ps1 alongside it — so no source-repo fallback is needed.
    /// </summary>
    public static string ScriptPath => Path.Combine(AppContext.BaseDirectory, "unstuck.ps1");

    /// <summary>Where <c>unstuck.ps1</c> saves its reports (<c>last-run.txt</c>, <c>history.log</c>).</summary>
    public static string ReportDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MindAttic", "Unstuck");

    public static string LastReportPath => Path.Combine(ReportDirectory, "last-run.txt");

    public static string HistoryPath => Path.Combine(ReportDirectory, "history.log");

    /// <summary>
    /// Runs the recovery off the calling thread (the script takes several seconds; blocking the UI
    /// thread would freeze the tray and queue up WM_HOTKEY presses). Returns null if a run is still
    /// in progress or finished less than <see cref="Cooldown"/> ago, so a mashed hotkey can't stack runs.
    /// Call from a single thread (the UI thread).
    /// </summary>
    public async Task<RecoveryResult?> RunAsync(RecoveryOptions options = RecoveryOptions.None)
    {
        if (running) return null;
        if (lastFinishedUtc is { } last && DateTime.UtcNow - last < Cooldown) return null;

        running = true;
        try { return await Task.Run(() => RunScriptAsync(options)); }
        finally
        {
            running = false;
            lastFinishedUtc = DateTime.UtcNow;
        }
    }

    /// <summary>The <c>unstuck.ps1</c> switches for <paramref name="options"/>, in a stable order.</summary>
    internal static IEnumerable<string> Switches(RecoveryOptions options)
    {
        if (options.HasFlag(RecoveryOptions.Check)) yield return "-Check";
        if (options.HasFlag(RecoveryOptions.Explorer)) yield return "-Explorer";
        if (options.HasFlag(RecoveryOptions.Hooks)) yield return "-Hooks";
        if (options.HasFlag(RecoveryOptions.Overlays)) yield return "-Overlays";
    }

    private static async Task<RecoveryResult> RunScriptAsync(RecoveryOptions options)
    {
        var scriptPath = ScriptPath;
        if (!File.Exists(scriptPath))
            return new RecoveryResult($"unstuck.ps1 not found at {scriptPath} - deploy it with the repo's run.ps1.", RawOutput: "", ExitCode: -1, NeedsAttention: true);

        var psi = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", scriptPath })
            psi.ArgumentList.Add(arg);
        foreach (var sw in Switches(options)) psi.ArgumentList.Add(sw);

        Process process;
        try { process = Process.Start(psi)!; }
        catch (Win32Exception ex)
        {
            return new RecoveryResult($"Couldn't start PowerShell: {ex.Message}", RawOutput: "", ExitCode: -1, NeedsAttention: true);
        }

        using (process)
        {
            // Drain both pipes concurrently: reading them one after the other deadlocks once the unread one fills.
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var cts = new CancellationTokenSource(Timeout);
            try { await process.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException)
            {
                // Only PowerShell itself: its children include apps it just relaunched (DisplayFusion, PowerToys).
                try { process.Kill(); } catch (InvalidOperationException) { } catch (Win32Exception) { }
                return new RecoveryResult($"unstuck.ps1 didn't finish within {Timeout.TotalSeconds:0} s and was stopped.", RawOutput: "", ExitCode: -1, NeedsAttention: true);
            }

            var output = await stdout;
            var errors = (await stderr).Trim();
            var summary = ExtractSummary(output);
            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output) && errors.Length > 0)
                summary = Truncate($"unstuck.ps1 failed: {errors}");
            return new RecoveryResult(summary, output, process.ExitCode, NeedsAttention(output) || process.ExitCode != 0);
        }
    }

    /// <summary>True when the report asks the user to act: a stuck gamepad button or a key still held.</summary>
    internal static bool NeedsAttention(string output)
    {
        foreach (var line in output.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("STUCK GAMEPAD INPUT", StringComparison.Ordinal)) return true;
            if (line.StartsWith("Keys still held:", StringComparison.Ordinal))
            {
                var held = line["Keys still held:".Length..].Trim();
                if (held.Length > 0 && !held.Equals("none", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
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
