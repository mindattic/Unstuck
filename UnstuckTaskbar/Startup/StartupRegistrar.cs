using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UnstuckTaskbar.Startup;

/// <summary>
/// Startup housekeeping for the installed app. Starting at logon is no longer this app's job: MindAttic.Scheduler's
/// <c>UnstuckTaskbar</c> job (<c>jobs.json</c>, M.SCHD-T9) starts it at sign-in and keeps it running, and that job
/// <c>replaces</c> the <c>\MindAttic\UnstuckTaskbar\Logon</c> task this app used to register (UNS-T9). Re-creating that
/// task on every start fought the Scheduler, so the app now removes it if it's still there, and only keeps its own
/// Start Menu shortcut. The desktop <c>Unstuck.lnk</c> (run.bat, the one-shot recovery) is never touched here.
/// </summary>
public static class StartupRegistrar
{
    /// <summary>The logon task older versions registered; MindAttic.Scheduler owns starting the app now.</summary>
    public const string LegacyTaskName = @"\MindAttic\UnstuckTaskbar\Logon";
    public const string ShortcutName = "UnstuckTaskbar.lnk";

    /// <summary>Removes the legacy logon task and makes sure the Start Menu shortcut points at <paramref name="exe"/>. Idempotent.</summary>
    public static void Apply(string exe)
    {
        // A dev build (bin\Debug, bin\Release, dotnet run) must not repoint the shortcut at itself or touch the
        // installed app's tasks - they'd break as soon as that build output is cleaned.
        if (IsDevBuild(exe)) return;
        RemoveLegacyTask();
        EnsureShortcut(exe);
    }

    internal static bool IsDevBuild(string exe) =>
        exe.Contains(@"\bin\Debug\", StringComparison.OrdinalIgnoreCase) ||
        exe.Contains(@"\bin\Release\", StringComparison.OrdinalIgnoreCase);

    private static void RemoveLegacyTask()
    {
        if (Run("schtasks", "/Query", "/TN", LegacyTaskName).Code == 0)
            Run("schtasks", "/Delete", "/TN", LegacyTaskName, "/F");
    }

    private static void EnsureShortcut(string exe)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "MindAttic", ShortcutName);
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null) return;
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic link = shell.CreateShortcut(path);
        if (File.Exists(path) && string.Equals((string)link.TargetPath, exe, StringComparison.OrdinalIgnoreCase)) return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        link.TargetPath = exe;
        link.IconLocation = exe + ",0";
        link.WorkingDirectory = Path.GetDirectoryName(exe);
        link.Description = "UnstuckTaskbar - tray icon and global hotkey for /unstuck";
        link.Save();
    }

    private static (int Code, string Output) Run(string file, params string[] args)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi)!;
            var stdout = p.StandardOutput.ReadToEndAsync();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode, stdout.Result + stderr);
        }
        catch (Win32Exception ex) { return (-1, ex.Message); }
    }
}
