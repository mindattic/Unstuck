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
