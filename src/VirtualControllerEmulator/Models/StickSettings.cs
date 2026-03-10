namespace VirtualControllerEmulator.Models;

public enum CurveType
{
    Linear,
    Quadratic,
    Cubic
}

public class StickSettings
{
    public double DeadZone { get; set; } = 0.1;
    public double Sensitivity { get; set; } = 1.0;
    public CurveType CurveType { get; set; } = CurveType.Linear;
}
