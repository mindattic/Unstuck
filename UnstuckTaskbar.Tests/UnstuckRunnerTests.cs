using System.IO;
using NUnit.Framework;
using UnstuckTaskbar.Recovery;

namespace UnstuckTaskbar.Tests;

public class UnstuckRunnerTests
{
    private const string SampleReportWithActions = """
        RAM: 8000 MB free of 32000 MB
        Top memory:
          chrome                       1200 MB  pid 1234
        Top CPU (2 s sample):
          chrome                         12%  pid 1234
        Keys held before: LeftShift, LeftCtrl
        Keys still held:  none
        Actions:
          released stuck key LeftShift
          restarted Explorer
        No runaway process found.
        """;

    private const string SampleReportNoActions = """
        RAM: 8000 MB free of 32000 MB
        Top memory:
          chrome                       1200 MB  pid 1234
        Keys held before: none
        Keys still held:  none
        Actions:
          none
        No runaway process found.
        """;

    private const string SampleReportStuckGamepad = """
        Keys held before: LeftTrigger
        Keys still held:  LeftTrigger
        STUCK GAMEPAD INPUT: Windows still thinks LeftTrigger is held - its release never arrived, and injected
          input can't clear a gamepad key. Press and release it on the controller.
        Actions:
          none
        """;

    [Test]
    public void ExtractSummary_lists_actions_when_present()
    {
        var summary = UnstuckRunner.ExtractSummary(SampleReportWithActions);

        Assert.That(summary, Does.Contain("released stuck key LeftShift"));
        Assert.That(summary, Does.Contain("restarted Explorer"));
        Assert.That(summary, Does.Not.Contain("Keys still held"));
    }

    [Test]
    public void ExtractSummary_reports_nothing_found_when_no_actions() =>
        Assert.That(UnstuckRunner.ExtractSummary(SampleReportNoActions),
            Is.EqualTo("No stuck input or runaway processes found."));

    [Test]
    public void ExtractSummary_appends_keys_still_held_when_nonempty()
    {
        const string report = """
            Keys still held:  LeftShift
            Actions:
              none
            """;

        var summary = UnstuckRunner.ExtractSummary(report);

        Assert.That(summary, Does.Contain("Keys still held: LeftShift"));
    }

    [Test]
    public void ExtractSummary_prioritizes_stuck_gamepad_message()
    {
        var summary = UnstuckRunner.ExtractSummary(SampleReportStuckGamepad);

        Assert.That(summary, Does.StartWith("STUCK GAMEPAD INPUT"));
    }

    [Test]
    public void NeedsAttention_flags_a_stuck_gamepad_or_a_held_key()
    {
        Assert.That(UnstuckRunner.NeedsAttention(SampleReportStuckGamepad), Is.True);
        Assert.That(UnstuckRunner.NeedsAttention("Keys still held:  Left Shift\n"), Is.True);
        Assert.That(UnstuckRunner.NeedsAttention(SampleReportNoActions), Is.False);
    }

    [Test]
    public void Switches_map_each_option_to_its_script_switch_in_a_stable_order()
    {
        Assert.That(UnstuckRunner.Switches(RecoveryOptions.None), Is.Empty);
        Assert.That(UnstuckRunner.Switches(RecoveryOptions.Overlays | RecoveryOptions.Check | RecoveryOptions.Explorer | RecoveryOptions.Hooks),
            Is.EqualTo(new[] { "-Check", "-Explorer", "-Hooks", "-Overlays" }));
    }

    [Test]
    public void Reports_live_under_LocalAppData_MindAttic_Unstuck()
    {
        Assert.That(UnstuckRunner.LastReportPath, Does.EndWith(Path.Combine("MindAttic", "Unstuck", "last-run.txt")));
        Assert.That(UnstuckRunner.HistoryPath, Does.EndWith(Path.Combine("MindAttic", "Unstuck", "history.log")));
    }

    [Test]
    public async Task Run_applies_a_cooldown_between_triggers()
    {
        var runner = new UnstuckRunner();

        var first = await runner.RunAsync(); // script won't exist at the test-runner's AppContext.BaseDirectory
        var second = await runner.RunAsync(); // immediate re-trigger within the cooldown window

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Null);
    }
}
