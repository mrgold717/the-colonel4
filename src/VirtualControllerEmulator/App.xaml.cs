using System.Windows;

namespace VirtualControllerEmulator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowError(ex);
    }

    private void OnDispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        ShowError(e.Exception);
        e.Handled = true;
    }

    private static void ShowError(Exception ex)
    {
        string message = ex.Message;

        if (ex.Message.Contains("ViGEm") ||
            ex.Message.Contains("ViGEmBus") ||
            ex.Message.Contains("0x80070002") ||
            ex.Message.Contains("device is not connected"))
        {
            message = "ViGEmBus driver is not installed or not running.\n\n" +
                      "Please download and install ViGEmBus from:\n" +
                      "https://github.com/nefarius/ViGEmBus/releases\n\n" +
                      "After installation, restart the application.";
        }

        MessageBox.Show(
            message,
            "Virtual Controller Emulator — Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
