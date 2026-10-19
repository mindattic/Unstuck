using System.Windows;
using System.Windows.Input;

namespace UnstuckTaskbar.Hotkeys;

/// <summary>"Change Hotkey..." dialog: press a combination (including a media key) and it's captured live.</summary>
public partial class HotkeyCaptureDialog : Window
{
    private static readonly Key[] ModifierOnlyKeys =
    [
        Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt,
        Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.System,
    ];

    public HotkeyGesture? Result { get; private set; }

    public HotkeyCaptureDialog() => InitializeComponent();

    private void CaptureBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // A bare Esc must reach the Cancel button (IsCancel) instead of being captured.
        if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (ModifierOnlyKeys.Contains(key)) return; // wait for a real key

        var gesture = new HotkeyGesture(Keyboard.Modifiers, key);
        if (!gesture.IsValid)
        {
            WarningText.Text = "Add Ctrl, Alt, Shift or Win - only media and volume keys work on their own.";
            WarningText.Visibility = Visibility.Visible;
            return;
        }

        Result = gesture;
        CaptureBox.Text = gesture.ToDisplayString();
        SaveButton.IsEnabled = true;

        // Trial-register so a conflict is visible before Save, rather than only discovered when
        // HotkeyManager applies it for real and the hotkey silently never fires.
        using var trial = new HotkeyManager();
        if (trial.RegisterCurrent(gesture))
        {
            WarningText.Visibility = Visibility.Collapsed;
        }
        else
        {
            WarningText.Text = "This combination may already be in use by another app. You can still save it.";
            WarningText.Visibility = Visibility.Visible;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }
}
