using System.Windows.Input;
using VirtualControllerEmulator.Helpers;
using VirtualControllerEmulator.Models;
using VirtualControllerEmulator.Services;

namespace VirtualControllerEmulator.ViewModels;

public enum NavigationPage
{
    Dashboard,
    Mapping,
    Profiles,
    Settings
}

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly VirtualControllerService _controllerService;
    private readonly InputCaptureService _captureService;
    private readonly InputMappingService _mappingService;
    private readonly ProfileService _profileService;
    private readonly TurboService _turboService;
    private readonly ProcessMonitorService _processMonitorService;

    private bool _isConnected;
    private string _statusMessage = "Disconnected";
    private string _activeProfileName = "None";
    private ControllerType _activeControllerType = ControllerType.Xbox360;
    private NavigationPage _currentPage = NavigationPage.Dashboard;
    private ControllerProfile? _currentProfile;
    private bool _disposed;

    public bool IsConnected { get => _isConnected; private set => SetProperty(ref _isConnected, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public string ActiveProfileName { get => _activeProfileName; set => SetProperty(ref _activeProfileName, value); }
    public ControllerType ActiveControllerType { get => _activeControllerType; set => SetProperty(ref _activeControllerType, value); }
    public NavigationPage CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }
    public ControllerProfile? CurrentProfile { get => _currentProfile; private set => SetProperty(ref _currentProfile, value); }

    // Sub-ViewModels
    public MappingViewModel MappingViewModel { get; }
    public ProfileViewModel ProfileViewModel { get; }
    public Services.InputMappingService MappingServiceForVisualizer => _mappingService;

    // Commands
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToMappingCommand { get; }
    public ICommand NavigateToProfilesCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }

    public MainViewModel()
    {
        _controllerService = new VirtualControllerService();
        _captureService = new InputCaptureService();
        _mappingService = new InputMappingService();
        _profileService = new ProfileService();
        _turboService = new TurboService();
        _processMonitorService = new ProcessMonitorService();

        MappingViewModel = new MappingViewModel(_captureService, _mappingService, _profileService);
        ProfileViewModel = new ProfileViewModel(_profileService);

        ConnectCommand = new RelayCommand(Connect, () => !IsConnected);
        DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
        NavigateToDashboardCommand = new RelayCommand(() => CurrentPage = NavigationPage.Dashboard);
        NavigateToMappingCommand = new RelayCommand(() => CurrentPage = NavigationPage.Mapping);
        NavigateToProfilesCommand = new RelayCommand(() => CurrentPage = NavigationPage.Profiles);
        NavigateToSettingsCommand = new RelayCommand(() => CurrentPage = NavigationPage.Settings);

        WireUpEvents();
        LoadDefaultProfile();
        _processMonitorService.Start();
    }

    private void WireUpEvents()
    {
        _controllerService.ErrorOccurred += (s, msg) =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => StatusMessage = $"Error: {msg}");

        _mappingService.ControllerStateChanged += (s, e) =>
        {
            if (IsConnected)
                _controllerService.ApplyState(e.State);
        };

        _captureService.KeyPressed += (s, e) => _mappingService.ProcessKeyDown(e.VkCode);
        _captureService.KeyReleased += (s, e) => _mappingService.ProcessKeyUp(e.VkCode);
        _captureService.MouseMoved += (s, e) => _mappingService.ProcessMouseDelta(e.DeltaX, e.DeltaY);
        _captureService.MouseButtonChanged += (s, e) => _mappingService.ProcessMouseButton(e.Button, e.IsPressed);
        _captureService.MouseWheelScrolled += (s, e) => _mappingService.ProcessMouseWheel(e.Delta);

        _turboService.TurboFired += (s, e) =>
        {
            if (IsConnected) _mappingService.ApplyTurboButton(e.ButtonName, e.IsActive);
        };

        _processMonitorService.ActiveProcessChanged += (s, e) => CheckForAutoProfile(e.ProcessName);

        ProfileViewModel.ProfileActivated += (s, profile) => ActivateProfile(profile);
    }

    private void LoadDefaultProfile()
    {
        var profile = _profileService.GetDefaultProfile();
        if (profile != null) ActivateProfile(profile);
    }

    public void ActivateProfile(ControllerProfile profile)
    {
        CurrentProfile = profile;
        ActiveProfileName = profile.Name;
        ActiveControllerType = profile.ControllerType;
        _mappingService.SetProfile(profile);
        MappingViewModel.LoadProfile(profile);

        if (IsConnected)
        {
            _controllerService.Disconnect();
            _controllerService.Connect(profile.ControllerType);
        }

        StatusMessage = IsConnected
            ? $"Profile '{profile.Name}' activated — connected."
            : $"Profile '{profile.Name}' loaded. Click Connect to start.";
    }

    private void Connect()
    {
        var profile = CurrentProfile;
        if (profile == null) { StatusMessage = "No profile loaded."; return; }

        bool ok = _controllerService.Connect(profile.ControllerType);
        if (ok)
        {
            IsConnected = true;
            StatusMessage = $"Connected as {(profile.ControllerType == ControllerType.Xbox360 ? "Xbox 360" : "DualShock 4")}";
            try { _captureService.StartCapturing(); }
            catch (Exception ex) { StatusMessage = $"Hook error: {ex.Message}"; }
        }
    }

    private void Disconnect()
    {
        _captureService.StopCapturing();
        _controllerService.Disconnect();
        IsConnected = false;
        StatusMessage = "Disconnected";
    }

    private void CheckForAutoProfile(string processName)
    {
        if (string.IsNullOrEmpty(processName)) return;
        var profiles = _profileService.LoadProfiles();
        var matched = profiles.FirstOrDefault(p =>
            !string.IsNullOrEmpty(p.LinkedProcess) &&
            p.LinkedProcess.Equals(processName, StringComparison.OrdinalIgnoreCase));

        if (matched != null && matched.Id != CurrentProfile?.Id)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ActivateProfile(matched);
                StatusMessage = $"Auto-switched to profile '{matched.Name}' for '{processName}'.";
            });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _captureService.StopCapturing();
        _controllerService.Dispose();
        _captureService.Dispose();
        _turboService.Dispose();
        _processMonitorService.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
