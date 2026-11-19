using System.IO;
using MindAttic.Vault.Settings;
using NUnit.Framework;
using UnstuckTaskbar.Settings;

namespace UnstuckTaskbar.Tests;

/// <summary>
/// Exercises the settings round-trip against a temp directory (not the real
/// %LocalAppData%\MindAttic\UnstuckTaskbar\ path <see cref="SettingsService"/> itself uses) so a
/// test run never touches a real deployed instance's settings file.
/// </summary>
public class SettingsServiceTests
{
    private string tempDir = null!;
    private JsonSettingsStore<TaskbarSettings> store = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "UnstuckTaskbarTests-" + Guid.NewGuid().ToString("N"));
        store = new JsonSettingsStore<TaskbarSettings>(tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    [Test]
    public void Defaults_when_file_missing()
    {
        var settings = store.Load();

        Assert.That(settings.HotkeyGesture, Is.Null);
    }

    [Test]
    public void Persists_across_load_save_round_trip()
    {
        store.Update(s => s.HotkeyGesture = "Control+Alt+U");

        var reloaded = store.Load();

        Assert.That(reloaded.HotkeyGesture, Is.EqualTo("Control+Alt+U"));
    }

}
