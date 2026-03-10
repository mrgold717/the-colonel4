using System.Windows;
using System.Windows.Input;
using VirtualControllerEmulator.ViewModels;

namespace VirtualControllerEmulator;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        DashboardVisualizer.Attach(_viewModel.MappingServiceForVisualizer);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        => _viewModel?.Dispose();
}
