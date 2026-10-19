using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;

namespace UnstuckTaskbar.Startup;

/// <summary>
/// "Start with Windows" via a logon Scheduled Task (not an HKCU Run key) — the same mechanism
/// MindAttic.Orchestrator's <c>Installer.EnsureDaemon</c> uses: a Run key is simpler but a
/// scheduled task survives better (restarts on failure, no odd interactions with elevation) and
/// matches what this ecosystem already ships. Also makes a Start Menu shortcut so the app is
/// launchable without digging into <c>C:\Apps</c>. The existing desktop <c>Unstuck.lnk</c> (which
/// runs the one-shot recovery via deploy.bat) is a separate shortcut for a separate app and is
/// never touched here.
/// </summary>
public static class StartupRegistrar
{
    public const string TaskName = @"\MindAttic\UnstuckTaskbar\Logon";
    public const string ShortcutName = "UnstuckTaskbar.lnk";

    /// <summary>Creates or removes the logon task to match <paramref name="enabled"/>. Idempotent.</summary>
    public static void Apply(bool enabled, string exe)
    {
        // A dev build (bin\Debug, bin\Release, dotnet run) must not repoint the logon task and shortcut
        // at itself - they'd break as soon as that build output is cleaned.
        if (IsDevBuild(exe)) return;
        if (enabled) EnsureTask(exe);
        else RemoveTask();
        EnsureShortcut(exe);
    }

    internal static bool IsDevBuild(string exe) =>
        exe.Contains(@"\bin\Debug\", StringComparison.OrdinalIgnoreCase) ||
        exe.Contains(@"\bin\Release\", StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistered() => Run("schtasks", "/Query", "/TN", TaskName).Code == 0;

    private static void EnsureTask(string exe)
    {
        var (code, xml) = Run("schtasks", "/Query", "/TN", TaskName, "/XML");
        if (code == 0 && xml.Contains(Escape(exe), StringComparison.OrdinalIgnoreCase)) return;

        var file = Path.Combine(Path.GetTempPath(), $"unstucktaskbar-task-{Environment.ProcessId}.xml");
        try
        {
            File.WriteAllText(file, TaskXml(exe, $@"{Environment.UserDomainName}\{Environment.UserName}"), Encoding.Unicode);
            var (created, output) = Run("schtasks", "/Create", "/TN", TaskName, "/XML", file, "/F");
            if (created != 0) throw new InvalidOperationException($"Couldn't register the logon task: {output.Trim()}");
        }
        finally { File.Delete(file); }
    }

    private static void RemoveTask() => Run("schtasks", "/Delete", "/TN", TaskName, "/F");

    private static string TaskXml(string exe, string user) => $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Description>Keeps UnstuckTaskbar's tray icon and global hotkey running. Installed by UnstuckTaskbar.exe.</Description>
          </RegistrationInfo>
          <Triggers>
            <LogonTrigger><Enabled>true</Enabled><UserId>{Escape(user)}</UserId></LogonTrigger>
          </Triggers>
          <Principals>
            <Principal id="Author"><UserId>{Escape(user)}</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <StartWhenAvailable>true</StartWhenAvailable>
            <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
            <RestartOnFailure><Interval>PT1M</Interval><Count>3</Count></RestartOnFailure>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>{Escape(exe)}</Command>
              <WorkingDirectory>{Escape(Path.GetDirectoryName(exe)!)}</WorkingDirectory>
            </Exec>
          </Actions>
        </Task>
        """;

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

    private static string Escape(string s) => SecurityElement.Escape(s) ?? s;

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
