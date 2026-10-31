using System.IO;
using System.Reflection;
using System.Windows.Input;
using MindAttic.Vault.Launch;
using MindAttic.Vault.Paths;
using MindAttic.Vault.Settings;
using MindAttic.Vault.Settings.Manifest;
using NUnit.Framework;
using UnstuckTaskbar.Hotkeys;
using UnstuckTaskbar.Settings;
using UnstuckTaskbar.Tray;

namespace UnstuckTaskbar.Tests;

/// <summary>LAMP (UNS-T17): settings described for MindAttic.Vault.Desktop, one end-to-end test per setting.</summary>
[NonParallelizable]
public class LampSettingsTests
{
    private string AppFolder => Path.Combine(VaultPaths.LocalRoot, SettingsService.AppId);
    private string SettingsFile => Path.Combine(AppFolder, "settings.json");

    [SetUp]
    public void SetUp()
    {
        if (Directory.Exists(AppFolder)) Directory.Delete(AppFolder, recursive: true);
    }

    private sealed class Recorder
    {
        public List<string> Calls { get; } = new();
        public SettingsApplier Applier() => new(
            g => Calls.Add("hotkey " + (g?.ToSettingsString() ?? "none")),
            on => Calls.Add("startup " + on),
            () => Calls.Add("tooltip"));
    }

    // Today's file (written by the pre-LAMP app) loads unchanged into the same per-machine folder.
    private void WriteTodaysFile(string json)
    {
        Directory.CreateDirectory(AppFolder);
        File.WriteAllText(SettingsFile, json);
    }

    [Test]
    public void Setting_HotkeyGesture_ImportsRoundTripsAndRegistersTheHotkey()
    {
        WriteTodaysFile("""{ "hotkeyGesture": "Control+Alt+U", "startWithWindows": true }""");
        var service = new SettingsService();
        var loaded = service.LoadOrSeed();
        Assert.That(loaded.HotkeyGesture, Is.EqualTo("Control+Alt+U"), "today's value carried over");

        service.Update(s => s.HotkeyGesture = "MediaPlayPause");
        Assert.That(new SettingsService().Load().HotkeyGesture, Is.EqualTo("MediaPlayPause"));
        Assert.That(SettingsManifestBuilder.Build<TaskbarSettings>().Find("MindAttic:Settings:UnstuckTaskbar:hotkeyGesture"), Is.Not.Null);

        var rec = new Recorder();
        rec.Applier().Apply(new SettingsService().Load());
        Assert.That(rec.Calls, Does.Contain("hotkey MediaPlayPause"));
    }

    [Test]
    public void Setting_HotkeyGesture_EmptyOrInvalidUnregisters()
    {
        var rec = new Recorder();
        var applier = rec.Applier();
        applier.Apply(new TaskbarSettings { HotkeyGesture = "Control+Alt+U" });
        applier.Apply(new TaskbarSettings { HotkeyGesture = "not a key" });
        applier.Apply(new TaskbarSettings { HotkeyGesture = null });
        Assert.That(rec.Calls.Where(c => c.StartsWith("hotkey")), Is.EqualTo(new[] { "hotkey Control+Alt+U", "hotkey none", "hotkey none" }));
    }

    [Test]
    public void Setting_StartWithWindows_ImportsRoundTripsAndAppliesStartup()
    {
        WriteTodaysFile("""{ "hotkeyGesture": null, "startWithWindows": false }""");
        var service = new SettingsService();
        Assert.That(service.LoadOrSeed().StartWithWindows, Is.False, "today's value carried over");

        service.Update(s => s.StartWithWindows = true);
        Assert.That(new SettingsService().Load().StartWithWindows, Is.True);
        Assert.That(SettingsManifestBuilder.Build<TaskbarSettings>().Find("MindAttic:Settings:UnstuckTaskbar:startWithWindows")!.Kind, Is.EqualTo(SettingKind.Bool));

        var rec = new Recorder();
        rec.Applier().Apply(new SettingsService().Load());
        Assert.That(rec.Calls, Does.Contain("startup True"));
    }

    [Test]
    public void EverySettingHasItsOwnTest()
    {
        var tested = GetType().GetMethods().Select(m => m.Name).Where(n => n.StartsWith("Setting_")).ToList();
        foreach (var field in SettingsManifestBuilder.Build<TaskbarSettings>().Fields)
            Assert.That(tested.Any(t => t.StartsWith("Setting_" + char.ToUpperInvariant(field.Property[0]) + field.Property[1..] + "_")), Is.True, field.Property);
    }

    [Test]
    public void Applier_OnlyReappliesWhatChanged()
    {
        var rec = new Recorder();
        var applier = rec.Applier();
        applier.Apply(new TaskbarSettings { HotkeyGesture = "Control+Alt+U", StartWithWindows = true });
        rec.Calls.Clear();
        applier.Apply(new TaskbarSettings { HotkeyGesture = "Control+Alt+U", StartWithWindows = false });
        Assert.That(rec.Calls, Is.EqualTo(new[] { "startup False", "tooltip" }));
    }

    [Test]
    public void Manifest_IsPerMachineLabeledAndSecretFree()
    {
        var m = SettingsManifestBuilder.Build<TaskbarSettings>();
        Assert.That(m.AppId, Is.EqualTo(SettingsService.AppId));
        Assert.That(m.Scope, Is.EqualTo(SettingsManifest.LocalScope));
        Assert.That(m.Fields.Select(f => f.Label), Is.EqualTo(new[] { "Recovery hotkey", "Start with Windows" }));
        Assert.That(m.Fields.All(f => !string.IsNullOrWhiteSpace(f.Help)), Is.True);
        Assert.That(m.Credentials, Is.Empty);
    }

    [Test]
    public void LoadOrSeed_OnAFreshMachine_SeedsTodaysDefaultsAndTheManifest()
    {
        var seeded = new SettingsService().LoadOrSeed();
        Assert.That(seeded.HotkeyGesture, Is.Null);
        Assert.That(seeded.StartWithWindows, Is.True);
        Assert.That(File.Exists(Path.Combine(AppFolder, SettingsManifest.FileName)), Is.True);
    }

    [Test]
    public void WriteSettingsManifestSwitch_SeedsAndReportsTheManifest()
    {
        var output = new StringWriter();
        Assert.That(SettingsManifestCommand.TryRun<TaskbarSettings>(new[] { SettingsManifestCommand.Switch }, output), Is.True);
        Assert.That(output.ToString().Trim(), Is.EqualTo(Path.Combine(AppFolder, SettingsManifest.FileName)));
    }

    [Test]
    public async Task SavedEdits_AreReportedSoTheTrayAppliesThemLive()
    {
        using var service = new SettingsService();
        service.LoadOrSeed();
        var changed = new TaskCompletionSource<TaskbarSettings>();
        service.Changed += s => changed.TrySetResult(s);
        service.Watch();
        new JsonSettingsStore<TaskbarSettings>(AppFolder).Save(new TaskbarSettings { HotkeyGesture = "Control+Shift+F12" });
        var seen = await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(seen.HotkeyGesture, Is.EqualTo("Control+Shift+F12"));
        Assert.That(HotkeyGesture.TryParse(seen.HotkeyGesture, out var g) && g.Key == Key.F12, Is.True);
    }

    [Test]
    public void SettingsMenu_OpensVaultAtThisAppsTab()
    {
        var original = VaultSettings.Launcher;
        var fake = new FakeLauncher();
        try
        {
            VaultSettings.Launcher = fake;
            Assert.That(VaultSettings.Open(SettingsService.AppId), Is.True);
            Assert.That(fake.Sent, Is.EqualTo(new[] { "--app UnstuckTaskbar" }));
        }
        finally { VaultSettings.Launcher = original; }

        Assert.That(typeof(TrayIconService).GetMethod("OpenSettings"), Is.Not.Null);
    }

    [Test]
    public void OldSettingsUi_IsGone()
    {
        var asm = typeof(TrayIconService).Assembly;
        Assert.That(asm.GetType("UnstuckTaskbar.Hotkeys.HotkeyCaptureDialog"), Is.Null);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        Assert.That(typeof(TrayIconService).GetMethod("ChangeHotkey", flags), Is.Null);
        Assert.That(typeof(TrayIconService).GetMethod("ToggleStartWithWindows", flags), Is.Null);
    }

    private sealed class FakeLauncher : IVaultDesktopLauncher
    {
        public List<string> Sent { get; } = new();
        public bool TrySend(VaultOpenRequest request) { Sent.Add(string.Join(' ', request.ToArgs())); return true; }
        public bool TryStart(VaultOpenRequest request) => false;
    }
}
