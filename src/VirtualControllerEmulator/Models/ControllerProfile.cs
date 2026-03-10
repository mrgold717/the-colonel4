namespace VirtualControllerEmulator.Models;

public class ControllerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Profile";
    public ControllerType ControllerType { get; set; } = ControllerType.Xbox360;
    public List<KeyMapping> KeyMappings { get; set; } = new();
    public MouseMapping MouseMapping { get; set; } = new();
    public StickSettings LeftStickSettings { get; set; } = new();
    public StickSettings RightStickSettings { get; set; } = new();
    public Dictionary<string, bool> TurboSettings { get; set; } = new();
    public string LinkedProcess { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
