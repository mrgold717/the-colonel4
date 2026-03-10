using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using VirtualControllerEmulator.Models;

namespace VirtualControllerEmulator.Services;

public class ProfileService
{
    private static readonly string ProfilesDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VirtualControllerEmulator", "Profiles");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ProfileService()
    {
        Directory.CreateDirectory(ProfilesDirectory);
    }

    public List<ControllerProfile> LoadProfiles()
    {
        var profiles = new List<ControllerProfile>();

        foreach (var file in Directory.GetFiles(ProfilesDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<ControllerProfile>(json, JsonOptions);
                if (profile != null)
                    profiles.Add(profile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load profile {file}: {ex.Message}");
            }
        }

        if (profiles.Count == 0)
        {
            var defaultProfile = CreateDefaultProfile();
            SaveProfile(defaultProfile);
            profiles.Add(defaultProfile);
        }

        return profiles;
    }

    public void SaveProfile(ControllerProfile profile)
    {
        profile.ModifiedAt = DateTime.UtcNow;
        var path = GetProfilePath(profile.Id);
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(path, json);
    }

    public void DeleteProfile(Guid id)
    {
        var path = GetProfilePath(id);
        if (File.Exists(path))
            File.Delete(path);
    }

    public ControllerProfile? GetDefaultProfile()
    {
        var profiles = LoadProfiles();
        return profiles.FirstOrDefault(p => p.IsDefault) ?? profiles.FirstOrDefault();
    }

    public void ExportProfile(ControllerProfile profile, string filePath)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public ControllerProfile? ImportProfile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var profile = JsonSerializer.Deserialize<ControllerProfile>(json, JsonOptions);
        if (profile != null)
        {
            profile.Id = Guid.NewGuid();
            profile.CreatedAt = DateTime.UtcNow;
            profile.ModifiedAt = DateTime.UtcNow;
            SaveProfile(profile);
        }
        return profile;
    }

    private static string GetProfilePath(Guid id) =>
        Path.Combine(ProfilesDirectory, $"{id}.json");

    public static ControllerProfile CreateDefaultProfile()
    {
        var profile = new ControllerProfile
        {
            Id = Guid.NewGuid(),
            Name = "Default (WASD + Mouse)",
            ControllerType = ControllerType.Xbox360,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            MouseMapping = new MouseMapping
            {
                TargetStick = TargetStick.RightStick,
                Sensitivity = 1.5,
                InvertX = false,
                InvertY = false,
                DeadZone = 0.05
            },
            LeftStickSettings = new StickSettings { DeadZone = 0.1, Sensitivity = 1.0, CurveType = CurveType.Linear },
            RightStickSettings = new StickSettings { DeadZone = 0.1, Sensitivity = 1.0, CurveType = CurveType.Linear },
            KeyMappings = new List<KeyMapping>
            {
                // WASD → Left Stick (handled in InputMappingService as built-in)
                // Arrow keys → Right Stick (built-in)

                // Face buttons
                new(0x20, InputType.Key, "A"),           // Space → A
                new(0xA0, InputType.Key, "B"),           // Left Shift → B
                new(0x45, InputType.Key, "X"),           // E → X
                new(0x51, InputType.Key, "Y"),           // Q → Y

                // Bumpers via mouse wheel
                new(0x200, InputType.MouseWheel, "RightShoulder"),  // Wheel Up → RB
                new(0x201, InputType.MouseWheel, "LeftShoulder"),   // Wheel Down → LB

                // Triggers via mouse buttons
                new(0x01, InputType.MouseButton, "RightTrigger"),   // LMB → RT
                new(0x02, InputType.MouseButton, "LeftTrigger"),    // RMB → LT

                // Menu buttons
                new(0x09, InputType.Key, "Back"),        // Tab → Back/Select
                new(0x0D, InputType.Key, "Start"),       // Enter → Start

                // D-Pad
                new(0x31, InputType.Key, "Up"),          // 1 → D-Pad Up
                new(0x32, InputType.Key, "Down"),        // 2 → D-Pad Down
                new(0x33, InputType.Key, "Left"),        // 3 → D-Pad Left
                new(0x34, InputType.Key, "Right"),       // 4 → D-Pad Right

                // Stick clicks
                new(0x46, InputType.Key, "LeftThumb"),   // F → LS Click
                new(0x52, InputType.Key, "RightThumb"),  // R → RS Click

                // Mouse movement → Right Stick (handled in InputMappingService)
            }
        };

        return profile;
    }
}
