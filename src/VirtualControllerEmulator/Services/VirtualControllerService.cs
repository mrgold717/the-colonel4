using System.Diagnostics;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using VirtualControllerEmulator.Models;

namespace VirtualControllerEmulator.Services;

public class VibrationEventArgs : EventArgs
{
    public byte LargeMotor { get; }
    public byte SmallMotor { get; }
    public VibrationEventArgs(byte large, byte small) { LargeMotor = large; SmallMotor = small; }
}

public class DS4FeedbackEventArgs : EventArgs
{
    public byte LargeMotor { get; }
    public byte SmallMotor { get; }
    public byte LightbarR { get; }
    public byte LightbarG { get; }
    public byte LightbarB { get; }
    public DS4FeedbackEventArgs(byte large, byte small, byte r, byte g, byte b)
    { LargeMotor = large; SmallMotor = small; LightbarR = r; LightbarG = g; LightbarB = b; }
}

/// <summary>Internal controller state snapshot passed between services.</summary>
public class ControllerState
{
    // Named buttons by their controller-button string key
    public Dictionary<string, bool> Buttons { get; set; } = new();
    public short LeftStickX { get; set; }
    public short LeftStickY { get; set; }
    public short RightStickX { get; set; }
    public short RightStickY { get; set; }
    public byte LeftTrigger { get; set; }
    public byte RightTrigger { get; set; }
}

public class VirtualControllerService : IDisposable
{
    private ViGEmClient? _client;
    private IXbox360Controller? _xbox360;
    private IDualShock4Controller? _ds4;
    private bool _disposed;
    private readonly object _lock = new();

    public bool IsConnected { get; private set; }
    public ControllerType ActiveControllerType { get; private set; } = ControllerType.Xbox360;

    public event EventHandler<VibrationEventArgs>? Xbox360VibrationReceived;
    public event EventHandler<DS4FeedbackEventArgs>? DS4FeedbackReceived;
    public event EventHandler<string>? ErrorOccurred;

    // Xbox360 button name → Xbox360Button mapping
    private static readonly Dictionary<string, Xbox360Button> XboxButtonMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "A",             Xbox360Button.A },
        { "B",             Xbox360Button.B },
        { "X",             Xbox360Button.X },
        { "Y",             Xbox360Button.Y },
        { "LeftShoulder",  Xbox360Button.LeftShoulder },
        { "RightShoulder", Xbox360Button.RightShoulder },
        { "Back",          Xbox360Button.Back },
        { "Start",         Xbox360Button.Start },
        { "LeftThumb",     Xbox360Button.LeftThumb },
        { "RightThumb",    Xbox360Button.RightThumb },
        { "Guide",         Xbox360Button.Guide },
        { "Up",            Xbox360Button.Up },
        { "Down",          Xbox360Button.Down },
        { "Left",          Xbox360Button.Left },
        { "Right",         Xbox360Button.Right },
    };

    // DS4 button name → DualShock4Button mapping
    private static readonly Dictionary<string, DualShock4Button> DS4ButtonMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "A",             DualShock4Button.Cross },
        { "B",             DualShock4Button.Circle },
        { "X",             DualShock4Button.Square },
        { "Y",             DualShock4Button.Triangle },
        { "LeftShoulder",  DualShock4Button.ShoulderLeft },
        { "RightShoulder", DualShock4Button.ShoulderRight },
        { "Back",          DualShock4Button.Share },
        { "Start",         DualShock4Button.Options },
        { "LeftThumb",     DualShock4Button.ThumbLeft },
        { "RightThumb",    DualShock4Button.ThumbRight },
    };

    public bool Connect(ControllerType controllerType)
    {
        lock (_lock)
        {
            if (IsConnected) Disconnect();
            try
            {
                _client = new ViGEmClient();
                ActiveControllerType = controllerType;

                if (controllerType == ControllerType.Xbox360)
                {
                    _xbox360 = _client.CreateXbox360Controller();
                    _xbox360.AutoSubmitReport = false;
                    _xbox360.FeedbackReceived += OnXbox360FeedbackReceived;
                    _xbox360.Connect();
                }
                else
                {
                    _ds4 = _client.CreateDualShock4Controller();
                    _ds4.AutoSubmitReport = false;
#pragma warning disable CS0618 // FeedbackReceived is deprecated but still functional
                    _ds4.FeedbackReceived += OnDS4FeedbackReceived;
#pragma warning restore CS0618
                    _ds4.Connect();
                }

                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                string msg = IsViGEmBusError(ex)
                    ? "ViGEmBus driver is not installed. Please install ViGEmBus from https://github.com/nefarius/ViGEmBus/releases"
                    : $"Failed to connect virtual controller: {ex.Message}";
                ErrorOccurred?.Invoke(this, msg);
                CleanupControllers();
                return false;
            }
        }
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            CleanupControllers();
            IsConnected = false;
        }
    }

    public void ApplyState(ControllerState state)
    {
        lock (_lock)
        {
            if (!IsConnected) return;
            try
            {
                if (ActiveControllerType == ControllerType.Xbox360 && _xbox360 != null)
                    ApplyXboxState(_xbox360, state);
                else if (ActiveControllerType == ControllerType.DualShock4 && _ds4 != null)
                    ApplyDS4State(_ds4, state);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Controller update error: {ex.Message}");
            }
        }
    }

    private static void ApplyXboxState(IXbox360Controller xbox, ControllerState state)
    {
        foreach (var kvp in state.Buttons)
        {
            if (XboxButtonMap.TryGetValue(kvp.Key, out var btn))
                xbox.SetButtonState(btn, kvp.Value);
        }
        xbox.SetAxisValue(Xbox360Axis.LeftThumbX, state.LeftStickX);
        xbox.SetAxisValue(Xbox360Axis.LeftThumbY, state.LeftStickY);
        xbox.SetAxisValue(Xbox360Axis.RightThumbX, state.RightStickX);
        xbox.SetAxisValue(Xbox360Axis.RightThumbY, state.RightStickY);
        xbox.SetSliderValue(Xbox360Slider.LeftTrigger, state.LeftTrigger);
        xbox.SetSliderValue(Xbox360Slider.RightTrigger, state.RightTrigger);
        xbox.SubmitReport();
    }

    private static void ApplyDS4State(IDualShock4Controller ds4, ControllerState state)
    {
        foreach (var kvp in state.Buttons)
        {
            if (DS4ButtonMap.TryGetValue(kvp.Key, out var btn))
                ds4.SetButtonState(btn, kvp.Value);
        }

        // DS4 axis is byte (0-255, 128=center)
        ds4.SetAxisValue(DualShock4Axis.LeftThumbX, NormalizeShortToByte(state.LeftStickX));
        ds4.SetAxisValue(DualShock4Axis.LeftThumbY, NormalizeShortToByte((short)-state.LeftStickY));
        ds4.SetAxisValue(DualShock4Axis.RightThumbX, NormalizeShortToByte(state.RightStickX));
        ds4.SetAxisValue(DualShock4Axis.RightThumbY, NormalizeShortToByte((short)-state.RightStickY));
        ds4.SetSliderValue(DualShock4Slider.LeftTrigger, state.LeftTrigger);
        ds4.SetSliderValue(DualShock4Slider.RightTrigger, state.RightTrigger);

        // D-Pad via DPad direction
        bool up    = state.Buttons.GetValueOrDefault("Up");
        bool down  = state.Buttons.GetValueOrDefault("Down");
        bool left  = state.Buttons.GetValueOrDefault("Left");
        bool right = state.Buttons.GetValueOrDefault("Right");
        ds4.SetDPadDirection(GetDS4DPad(up, down, left, right));

        ds4.SubmitReport();
    }

    private static byte NormalizeShortToByte(short value)
        => (byte)((value / 32767.0 * 127.5) + 127.5).Clamp(0, 255);

    private static DualShock4DPadDirection GetDS4DPad(bool up, bool down, bool left, bool right)
    {
        if (up && right) return DualShock4DPadDirection.Northeast;
        if (up && left)  return DualShock4DPadDirection.Northwest;
        if (down && right) return DualShock4DPadDirection.Southeast;
        if (down && left)  return DualShock4DPadDirection.Southwest;
        if (up)    return DualShock4DPadDirection.North;
        if (down)  return DualShock4DPadDirection.South;
        if (left)  return DualShock4DPadDirection.West;
        if (right) return DualShock4DPadDirection.East;
        return DualShock4DPadDirection.None;
    }

    private static bool IsViGEmBusError(Exception ex)
        => ex.Message.Contains("ViGEm") || ex.Message.Contains("0x80070002") ||
           ex is Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException ||
           ex is Nefarius.ViGEm.Client.Exceptions.VigemBusAccessFailedException;

    private void OnXbox360FeedbackReceived(object? sender, Xbox360FeedbackReceivedEventArgs e)
        => Xbox360VibrationReceived?.Invoke(this, new VibrationEventArgs(e.LargeMotor, e.SmallMotor));

    private void OnDS4FeedbackReceived(object? sender, DualShock4FeedbackReceivedEventArgs e)
        => DS4FeedbackReceived?.Invoke(this, new DS4FeedbackEventArgs(
            e.LargeMotor, e.SmallMotor,
            e.LightbarColor.Red, e.LightbarColor.Green, e.LightbarColor.Blue));

    private void CleanupControllers()
    {
        try
        {
            if (_xbox360 != null)
            {
                _xbox360.FeedbackReceived -= OnXbox360FeedbackReceived;
                try { _xbox360.Disconnect(); } catch { }
                _xbox360 = null;
            }
            if (_ds4 != null)
            {
#pragma warning disable CS0618
                _ds4.FeedbackReceived -= OnDS4FeedbackReceived;
#pragma warning restore CS0618
                try { _ds4.Disconnect(); } catch { }
                _ds4 = null;
            }
            _client?.Dispose();
            _client = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cleanup error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

internal static class DoubleExtensions
{
    public static double Clamp(this double value, double min, double max)
        => Math.Max(min, Math.Min(max, value));
}
