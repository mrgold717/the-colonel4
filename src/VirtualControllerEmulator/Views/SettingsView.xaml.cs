using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace VirtualControllerEmulator.Views;

public partial class SettingsView : UserControl
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "VirtualControllerEmulator";

    public SettingsView()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            StartWithWindowsCheck.IsChecked = key?.GetValue(AppName) != null;
        }
        catch { /* ignore registry errors */ }
    }

    private void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;

            if (StartWithWindowsCheck.IsChecked == true)
            {
                string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath != null)
                    key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }

            MessageBox.Show("Settings applied.", AppName, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to apply settings: {ex.Message}", AppName,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Reset all settings to defaults?", AppName,
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        SensitivitySlider.Value = 1.5;
        LeftDeadZoneSlider.Value = 0.1;
        RightDeadZoneSlider.Value = 0.1;
        TurboRateSlider.Value = 10;
        StartWithWindowsCheck.IsChecked = false;
        StartMinimizedCheck.IsChecked = false;
        MinimizeToTrayCheck.IsChecked = true;
        AutoConnectCheck.IsChecked = false;
    }
}
