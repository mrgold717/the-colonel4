using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using VirtualControllerEmulator.Helpers;
using VirtualControllerEmulator.Models;
using VirtualControllerEmulator.Services;

namespace VirtualControllerEmulator.ViewModels;

public class ProfileViewModel : ViewModelBase
{
    private readonly ProfileService _profileService;
    private ControllerProfile? _selectedProfile;
    private string _statusMessage = string.Empty;

    public ObservableCollection<ControllerProfile> Profiles { get; } = new();

    public ControllerProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            SetProperty(ref _selectedProfile, value);
            RaiseCommandsCanExecute();
        }
    }

    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand CreateProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand DuplicateProfileCommand { get; }
    public ICommand ImportProfileCommand { get; }
    public ICommand ExportProfileCommand { get; }
    public ICommand SetActiveProfileCommand { get; }
    public ICommand RenameProfileCommand { get; }

    public event EventHandler<ControllerProfile>? ProfileActivated;

    public ProfileViewModel(ProfileService profileService)
    {
        _profileService = profileService;

        CreateProfileCommand = new RelayCommand(CreateProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile, () => SelectedProfile != null && !SelectedProfile.IsDefault);
        DuplicateProfileCommand = new RelayCommand(DuplicateProfile, () => SelectedProfile != null);
        ImportProfileCommand = new RelayCommand(ImportProfile);
        ExportProfileCommand = new RelayCommand(ExportProfile, () => SelectedProfile != null);
        SetActiveProfileCommand = new RelayCommand(SetActiveProfile, () => SelectedProfile != null);
        RenameProfileCommand = new RelayCommand<string>(RenameProfile);

        LoadProfiles();
    }

    public void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var p in _profileService.LoadProfiles())
            Profiles.Add(p);

        SelectedProfile = Profiles.FirstOrDefault(p => p.IsDefault) ?? Profiles.FirstOrDefault();
    }

    private void CreateProfile()
    {
        var profile = new ControllerProfile
        {
            Name = $"Profile {Profiles.Count + 1}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        _profileService.SaveProfile(profile);
        Profiles.Add(profile);
        SelectedProfile = profile;
        StatusMessage = $"Created profile '{profile.Name}'.";
    }

    private void DeleteProfile()
    {
        if (SelectedProfile == null || SelectedProfile.IsDefault) return;
        var toDelete = SelectedProfile;
        _profileService.DeleteProfile(toDelete.Id);
        Profiles.Remove(toDelete);
        SelectedProfile = Profiles.FirstOrDefault();
        StatusMessage = $"Deleted profile '{toDelete.Name}'.";
    }

    private void DuplicateProfile()
    {
        if (SelectedProfile == null) return;
        var dupe = new ControllerProfile
        {
            Id = Guid.NewGuid(),
            Name = $"{SelectedProfile.Name} (Copy)",
            ControllerType = SelectedProfile.ControllerType,
            KeyMappings = SelectedProfile.KeyMappings.Select(m => new KeyMapping(m.InputKey, m.InputType, m.ControllerButton)
            {
                IsModifierRequired = m.IsModifierRequired,
                ModifierKey = m.ModifierKey,
                TurboEnabled = m.TurboEnabled,
                TurboRate = m.TurboRate
            }).ToList(),
            MouseMapping = new MouseMapping
            {
                TargetStick = SelectedProfile.MouseMapping.TargetStick,
                Sensitivity = SelectedProfile.MouseMapping.Sensitivity,
                InvertX = SelectedProfile.MouseMapping.InvertX,
                InvertY = SelectedProfile.MouseMapping.InvertY,
                DeadZone = SelectedProfile.MouseMapping.DeadZone
            },
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _profileService.SaveProfile(dupe);
        Profiles.Add(dupe);
        SelectedProfile = dupe;
        StatusMessage = $"Duplicated as '{dupe.Name}'.";
    }

    private void ImportProfile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Profile",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var profile = _profileService.ImportProfile(dialog.FileName);
            if (profile != null)
            {
                Profiles.Add(profile);
                SelectedProfile = profile;
                StatusMessage = $"Imported profile '{profile.Name}'.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    private void ExportProfile()
    {
        if (SelectedProfile == null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Export Profile",
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = $"{SelectedProfile.Name}.json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            _profileService.ExportProfile(SelectedProfile, dialog.FileName);
            StatusMessage = $"Exported '{SelectedProfile.Name}' to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private void SetActiveProfile()
    {
        if (SelectedProfile == null) return;
        ProfileActivated?.Invoke(this, SelectedProfile);
        StatusMessage = $"Activated profile '{SelectedProfile.Name}'.";
    }

    private void RenameProfile(string? newName)
    {
        if (SelectedProfile == null || string.IsNullOrWhiteSpace(newName)) return;
        SelectedProfile.Name = newName;
        _profileService.SaveProfile(SelectedProfile);
        StatusMessage = $"Renamed to '{newName}'.";
    }

    private void RaiseCommandsCanExecute()
    {
        (DeleteProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DuplicateProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ExportProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SetActiveProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
