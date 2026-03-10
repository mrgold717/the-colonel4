using System.Collections.ObjectModel;
using System.Windows.Input;
using VirtualControllerEmulator.Helpers;
using VirtualControllerEmulator.Models;
using VirtualControllerEmulator.Services;

// Explicit aliases to avoid ambiguity with System.Windows.Input
using ServiceKeyEventArgs = VirtualControllerEmulator.Services.KeyEventArgs;
using ServiceMouseButtonEventArgs = VirtualControllerEmulator.Services.MouseButtonEventArgs;
using ModelInputType = VirtualControllerEmulator.Models.InputType;

namespace VirtualControllerEmulator.ViewModels;

public class KeyMappingItem : ViewModelBase
{
    private string _buttonName = string.Empty;
    private string _mappedKey = string.Empty;
    private bool _turboEnabled;

    public string ButtonName { get => _buttonName; set => SetProperty(ref _buttonName, value); }
    public string MappedKey { get => _mappedKey; set => SetProperty(ref _mappedKey, value); }
    public bool TurboEnabled { get => _turboEnabled; set => SetProperty(ref _turboEnabled, value); }
    public KeyMapping? SourceMapping { get; set; }
}

public class MappingViewModel : ViewModelBase
{
    private readonly InputCaptureService _captureService;
    private readonly InputMappingService _mappingService;
    private readonly ProfileService _profileService;

    private bool _isCapturing;
    private string _capturingButton = string.Empty;
    private string _statusMessage = string.Empty;
    private ControllerProfile? _currentProfile;

    public ObservableCollection<KeyMappingItem> Mappings { get; } = new();

    public bool IsCapturing { get => _isCapturing; private set => SetProperty(ref _isCapturing, value); }
    public string CapturingButton { get => _capturingButton; private set => SetProperty(ref _capturingButton, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand StartCapturingCommand { get; }
    public ICommand CancelCapturingCommand { get; }
    public ICommand SaveMappingsCommand { get; }
    public ICommand ResetToDefaultCommand { get; }
    public ICommand ClearMappingCommand { get; }

    public MappingViewModel(
        InputCaptureService captureService,
        InputMappingService mappingService,
        ProfileService profileService)
    {
        _captureService = captureService;
        _mappingService = mappingService;
        _profileService = profileService;

        StartCapturingCommand = new RelayCommand<string>(StartCapturing, b => !IsCapturing);
        CancelCapturingCommand = new RelayCommand(CancelCapturing, () => IsCapturing);
        SaveMappingsCommand = new RelayCommand(SaveMappings);
        ResetToDefaultCommand = new RelayCommand(ResetToDefault);
        ClearMappingCommand = new RelayCommand<string>(ClearMapping);

        _captureService.KeyPressed += OnKeyCaptured;
        _captureService.MouseButtonChanged += OnMouseButtonCaptured;

        InitializeButtonList();
    }

    private void InitializeButtonList()
    {
        var buttons = new[]
        {
            "A", "B", "X", "Y",
            "LeftShoulder", "RightShoulder",
            "LeftTrigger", "RightTrigger",
            "Back", "Start",
            "LeftThumb", "RightThumb",
            "Up", "Down", "Left", "Right"
        };

        foreach (var btn in buttons)
            Mappings.Add(new KeyMappingItem { ButtonName = btn, MappedKey = "Not mapped" });
    }

    public void LoadProfile(ControllerProfile profile)
    {
        _currentProfile = profile;
        foreach (var item in Mappings)
        {
            var mapping = profile.KeyMappings.FirstOrDefault(m => m.ControllerButton == item.ButtonName);
            if (mapping != null)
            {
                item.MappedKey = KeyNameHelper.GetKeyName(mapping.InputKey);
                item.TurboEnabled = mapping.TurboEnabled;
                item.SourceMapping = mapping;
            }
            else
            {
                item.MappedKey = item.ButtonName switch
                {
                    "A" => "Space (built-in)",
                    "B" => "Left Shift (built-in)",
                    "X" => "E (built-in)",
                    "Y" => "Q (built-in)",
                    _ => "Not mapped"
                };
            }
        }
    }

    private void StartCapturing(string? buttonName)
    {
        if (string.IsNullOrEmpty(buttonName)) return;
        IsCapturing = true;
        CapturingButton = buttonName;
        StatusMessage = $"Press a key or mouse button to map to '{buttonName}'...";
    }

    private void CancelCapturing()
    {
        IsCapturing = false;
        CapturingButton = string.Empty;
        StatusMessage = string.Empty;
    }

    private void OnKeyCaptured(object? sender, ServiceKeyEventArgs e)
    {
        if (!IsCapturing) return;
        ApplyCapture(e.VkCode, ModelInputType.Key);
    }

    private void OnMouseButtonCaptured(object? sender, ServiceMouseButtonEventArgs e)
    {
        if (!IsCapturing || !e.IsPressed) return;
        ApplyCapture(e.Button, ModelInputType.MouseButton);
    }

    private void ApplyCapture(int inputKey, ModelInputType inputType)
    {
        if (!IsCapturing) return;

        var item = Mappings.FirstOrDefault(m => m.ButtonName == CapturingButton);
        if (item == null) { CancelCapturing(); return; }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            item.MappedKey = KeyNameHelper.GetKeyName(inputKey);

            if (item.SourceMapping == null)
            {
                item.SourceMapping = new KeyMapping(inputKey, inputType, CapturingButton);
                _currentProfile?.KeyMappings.Add(item.SourceMapping);
            }
            else
            {
                item.SourceMapping.InputKey = inputKey;
                item.SourceMapping.InputType = inputType;
            }

            StatusMessage = $"'{CapturingButton}' mapped to '{item.MappedKey}'";
            IsCapturing = false;
            CapturingButton = string.Empty;
        });
    }

    private void SaveMappings()
    {
        if (_currentProfile == null) return;
        _profileService.SaveProfile(_currentProfile);
        _mappingService.SetProfile(_currentProfile);
        StatusMessage = "Mappings saved!";
    }

    private void ResetToDefault()
    {
        var defaultProfile = ProfileService.CreateDefaultProfile();
        _currentProfile = defaultProfile;
        LoadProfile(defaultProfile);
        StatusMessage = "Reset to default mappings.";
    }

    private void ClearMapping(string? buttonName)
    {
        if (string.IsNullOrEmpty(buttonName) || _currentProfile == null) return;
        var mapping = _currentProfile.KeyMappings.FirstOrDefault(m => m.ControllerButton == buttonName);
        if (mapping != null) _currentProfile.KeyMappings.Remove(mapping);

        var item = Mappings.FirstOrDefault(m => m.ButtonName == buttonName);
        if (item != null) { item.MappedKey = "Not mapped"; item.SourceMapping = null; }
    }
}
