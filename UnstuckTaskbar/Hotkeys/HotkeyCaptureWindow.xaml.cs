using System.Windows;
using System.Windows.Input;

namespace UnstuckTaskbar.Hotkeys;

/// <summary>
/// A small modal that captures the next key chord for the user's one configurable extra hotkey (UNS-T34), so it
/// can be set from the tray menu without typing a gesture string in Vault Settings. Esc cancels; a bare modifier
/// press (just Ctrl, just Alt, ...) isn't itself a chord, so it's ignored rather than shown as invalid.
/// </summary>
public partial class HotkeyCaptureWindow : Window
{
    /// <summary>The captured gesture once <see cref="OkButton_Click"/> confirms it; null if cancelled.</summary>
    public HotkeyGesture? Result { get; private set; }

    private HotkeyGesture? candidate;

    public HotkeyCaptureWindow() => InitializeComponent();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) { DialogResult = false; return; }
        e.Handled = true;

        // A bare modifier key on its own isn't a chord yet - keep waiting for the key that goes with it.
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin) return;

        var gesture = new HotkeyGesture(Keyboard.Modifiers, key);
        CapturedText.Text = gesture.ToDisplayString();

        if (!gesture.IsValid) { ShowError("Needs at least one modifier (Ctrl/Alt/Shift/Win), unless it's a media or volume key."); return; }
        if (gesture == HotkeyGesture.BuiltIn || gesture == HotkeyGesture.BuiltInExtra)
        { ShowError($"{gesture.ToDisplayString()} is already a permanent hotkey - pick another."); return; }

        candidate = gesture;
        ErrorText.Visibility = Visibility.Collapsed;
        OkButton.IsEnabled = true;
    }

    private void ShowError(string message)
    {
        candidate = null;
        OkButton.IsEnabled = false;
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Result = candidate;
        DialogResult = true;
    }
}
