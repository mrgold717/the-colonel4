namespace VirtualControllerEmulator.Models;

public enum TargetStick
{
    LeftStick,
    RightStick
}

public class MouseMapping
{
    public TargetStick TargetStick { get; set; } = TargetStick.RightStick;
    public double Sensitivity { get; set; } = 1.0;
    public bool InvertX { get; set; }
    public bool InvertY { get; set; }
    public double DeadZone { get; set; } = 0.05;
}
