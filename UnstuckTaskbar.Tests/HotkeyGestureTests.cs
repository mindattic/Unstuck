using System.Windows.Input;
using NUnit.Framework;
using UnstuckTaskbar.Hotkeys;

namespace UnstuckTaskbar.Tests;

public class HotkeyGestureTests
{
    [TestCase(ModifierKeys.None, Key.MediaPlayPause)]
    [TestCase(ModifierKeys.None, Key.MediaNextTrack)]
    [TestCase(ModifierKeys.None, Key.MediaPreviousTrack)]
    [TestCase(ModifierKeys.None, Key.MediaStop)]
    [TestCase(ModifierKeys.None, Key.VolumeMute)]
    [TestCase(ModifierKeys.None, Key.VolumeUp)]
    [TestCase(ModifierKeys.None, Key.VolumeDown)]
    [TestCase(ModifierKeys.Control, Key.U)]
    [TestCase(ModifierKeys.Control | ModifierKeys.Alt, Key.U)]
    [TestCase(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift | ModifierKeys.Windows, Key.F12)]
    public void RoundTrips_through_settings_string(ModifierKeys modifiers, Key key)
    {
        var original = new HotkeyGesture(modifiers, key);

        var parsed = HotkeyGesture.TryParse(original.ToSettingsString(), out var result);

        Assert.That(parsed, Is.True);
        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void Media_key_has_no_modifiers_and_is_valid()
    {
        var gesture = new HotkeyGesture(ModifierKeys.None, Key.MediaPlayPause);

        Assert.That(gesture.IsValid, Is.True);
        Assert.That(gesture.ToDisplayString(), Is.EqualTo("Media Play/Pause"));
    }

    [TestCase(Key.U)]
    [TestCase(Key.F5)]
    [TestCase(Key.Escape)]
    public void Bare_ordinary_key_is_invalid(Key key) =>
        Assert.That(new HotkeyGesture(ModifierKeys.None, key).IsValid, Is.False);

    [TestCase(Key.F13)]
    [TestCase(Key.VolumeMute)]
    public void Bare_key_with_no_default_meaning_is_valid(Key key) =>
        Assert.That(new HotkeyGesture(ModifierKeys.None, key).IsValid, Is.True);

    [Test]
    public void Bare_letter_settings_string_is_rejected() =>
        Assert.That(HotkeyGesture.TryParse("U", out _), Is.False);

    [TestCase(@"D:\Projects\MindAttic\Unstuck\UnstuckTaskbar\bin\Debug\net10.0-windows\UnstuckTaskbar.exe", true)]
    [TestCase(@"D:\Projects\MindAttic\Unstuck\UnstuckTaskbar\bin\Release\net10.0-windows\win-x64\UnstuckTaskbar.exe", true)]
    [TestCase(@"C:\Apps\Unstuck\UnstuckTaskbar.exe", false)]
    public void Dev_builds_are_detected(string exe, bool expected) =>
        Assert.That(UnstuckTaskbar.Startup.StartupRegistrar.IsDevBuild(exe), Is.EqualTo(expected));

    [Test]
    public void BuiltIn_is_Ctrl_Alt_Break_and_Break_parses_as_Pause()
    {
        Assert.That(HotkeyGesture.BuiltIn.ToDisplayString(), Is.EqualTo("Ctrl+Alt+Break"));
        Assert.That(HotkeyGesture.TryParse("Control+Alt+Break", out var g) && g == HotkeyGesture.BuiltIn, Is.True);
        Assert.That(HotkeyGesture.TryParse(HotkeyGesture.BuiltIn.ToSettingsString(), out var r) && r == HotkeyGesture.BuiltIn, Is.True);
    }

    [Test]
    public void BuiltInExtra_is_Ctrl_Alt_U_and_distinct_from_BuiltIn()
    {
        Assert.That(HotkeyGesture.BuiltInExtra.ToDisplayString(), Is.EqualTo("Ctrl+Alt+U"));
        Assert.That(HotkeyGesture.TryParse("Control+Alt+U", out var g) && g == HotkeyGesture.BuiltInExtra, Is.True);
        Assert.That(HotkeyGesture.BuiltInExtra, Is.Not.EqualTo(HotkeyGesture.BuiltIn));
    }

    [Test]
    public void A_lone_digit_means_that_digit_key_not_the_enum_value()
    {
        Assert.That(HotkeyGesture.TryParse("Control+Alt+1", out var g), Is.True);
        Assert.That(g.Key, Is.EqualTo(Key.D1));
    }

    [TestCase("Control+9999")]
    [TestCase("Control+12")]
    [TestCase("Control+-1")]
    public void Numeric_key_names_are_rejected(string value) =>
        Assert.That(HotkeyGesture.TryParse(value, out _), Is.False);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("NotAModifier+U")]
    [TestCase("Control+NotAKey")]
    [TestCase("Control+None")]
    public void Rejects_invalid_or_empty_strings(string? value)
    {
        var parsed = HotkeyGesture.TryParse(value, out _);

        Assert.That(parsed, Is.False);
    }

    [Test]
    public void ToWin32_encodes_each_modifier_flag()
    {
        var gesture = new HotkeyGesture(ModifierKeys.Control | ModifierKeys.Shift, Key.A);

        var (modifiers, vk) = gesture.ToWin32();

        Assert.That(modifiers, Is.EqualTo(0x0002u | 0x0004u)); // MOD_CONTROL | MOD_SHIFT
        Assert.That(vk, Is.EqualTo((uint)KeyInterop.VirtualKeyFromKey(Key.A)));
    }
}
