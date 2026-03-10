using VirtualControllerEmulator.Models;

namespace VirtualControllerEmulator.Services;

public class ControllerStateChangedEventArgs : EventArgs
{
    public ControllerState State { get; }
    public ControllerType ControllerType { get; }
    public ControllerStateChangedEventArgs(ControllerState state, ControllerType type)
    { State = state; ControllerType = type; }
}

public class InputMappingService
{
    private readonly object _lock = new();
    private ControllerProfile? _currentProfile;
    private readonly ControllerState _state = new();

    // Pressed key tracking
    private readonly HashSet<int> _pressedKeys = new();

    // Digital directional state from keys
    private bool _leftUp, _leftDown, _leftLeft, _leftRight;
    private bool _rightUp, _rightDown, _rightLeft, _rightRight;

    // Mouse stick accumulation
    private double _mouseStickX;
    private double _mouseStickY;

    // Built-in VK codes
    private const int VK_W = 0x57, VK_A = 0x41, VK_S = 0x53, VK_D = 0x44;
    private const int VK_UP = 0x26, VK_DOWN = 0x28, VK_LEFT = 0x25, VK_RIGHT = 0x27;

    public event EventHandler<ControllerStateChangedEventArgs>? ControllerStateChanged;

    public void SetProfile(ControllerProfile profile)
    {
        lock (_lock)
        {
            _currentProfile = profile;
            ResetState();
        }
    }

    private void ResetState()
    {
        _state.Buttons.Clear();
        _state.LeftStickX = _state.LeftStickY = 0;
        _state.RightStickX = _state.RightStickY = 0;
        _state.LeftTrigger = _state.RightTrigger = 0;
        _pressedKeys.Clear();
        _mouseStickX = _mouseStickY = 0;
        _leftUp = _leftDown = _leftLeft = _leftRight = false;
        _rightUp = _rightDown = _rightLeft = _rightRight = false;
    }

    public void ProcessKeyDown(int vkCode)
    {
        lock (_lock)
        {
            if (_pressedKeys.Contains(vkCode)) return;
            _pressedKeys.Add(vkCode);
            if (_currentProfile == null) return;
            ApplyKeyState(vkCode, true);
            UpdateStickAxes();
            Notify();
        }
    }

    public void ProcessKeyUp(int vkCode)
    {
        lock (_lock)
        {
            if (!_pressedKeys.Contains(vkCode)) return;
            _pressedKeys.Remove(vkCode);
            if (_currentProfile == null) return;
            ApplyKeyState(vkCode, false);
            UpdateStickAxes();
            Notify();
        }
    }

    public void ProcessMouseDelta(int dx, int dy)
    {
        lock (_lock)
        {
            if (_currentProfile == null) return;
            var mm = _currentProfile.MouseMapping;
            double sens = mm.Sensitivity * 0.05;
            double scaledX = dx * sens;
            double scaledY = dy * sens;
            if (mm.InvertX) scaledX = -scaledX;
            if (mm.InvertY) scaledY = -scaledY;

            _mouseStickX = Math.Clamp(_mouseStickX + scaledX, -1.0, 1.0);
            _mouseStickY = Math.Clamp(_mouseStickY + scaledY, -1.0, 1.0);
            ApplyMouseToStick(mm.TargetStick);
            Notify();
            DecayMouseStick();
        }
    }

    public void ProcessMouseButton(int button, bool pressed)
    {
        lock (_lock)
        {
            if (_currentProfile == null) return;
            var mapping = _currentProfile.KeyMappings.FirstOrDefault(m =>
                m.InputType == InputType.MouseButton && m.InputKey == button);
            if (mapping == null) return;
            ApplyButtonName(mapping.ControllerButton, pressed);
            Notify();
        }
    }

    public void ProcessMouseWheel(int delta)
    {
        lock (_lock)
        {
            if (_currentProfile == null) return;
            bool isUp = delta > 0;
            var mapping = _currentProfile.KeyMappings.FirstOrDefault(m =>
                m.InputType == InputType.MouseWheel &&
                ((isUp && m.InputKey == 0x200) || (!isUp && m.InputKey == 0x201)));
            if (mapping == null) return;
            ApplyButtonName(mapping.ControllerButton, true);
            Notify();
            // Wheel buttons auto-release after a short pulse
            System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
            {
                lock (_lock)
                {
                    ApplyButtonName(mapping.ControllerButton, false);
                    Notify();
                }
            }, TaskScheduler.Default);
        }
    }

    private void ApplyKeyState(int vkCode, bool pressed)
    {
        if (_currentProfile == null) return;

        // Check profile mappings first
        var mapping = _currentProfile.KeyMappings.FirstOrDefault(m =>
            m.InputType == InputType.Key && m.InputKey == vkCode);
        if (mapping != null)
        {
            ApplyButtonName(mapping.ControllerButton, pressed);
            return;
        }

        // Built-in WASD/arrow key stick mappings
        switch (vkCode)
        {
            case VK_W:     _leftUp    = pressed; break;
            case VK_S:     _leftDown  = pressed; break;
            case VK_A:     _leftLeft  = pressed; break;
            case VK_D:     _leftRight = pressed; break;
            case VK_UP:    _rightUp   = pressed; break;
            case VK_DOWN:  _rightDown = pressed; break;
            case VK_LEFT:  _rightLeft = pressed; break;
            case VK_RIGHT: _rightRight = pressed; break;
        }
    }

    /// <summary>Directly sets a named controller button state (used by turbo service).</summary>
    public void ApplyTurboButton(string buttonName, bool pressed)
    {
        lock (_lock)
        {
            ApplyButtonName(buttonName, pressed);
            Notify();
        }
    }

    private void ApplyButtonName(string name, bool pressed)
    {
        _state.Buttons[name] = pressed;
    }

    private void UpdateStickAxes()
    {
        double lx = (_leftRight ? 1.0 : 0.0) - (_leftLeft ? 1.0 : 0.0);
        double ly = (_leftUp ? 1.0 : 0.0) - (_leftDown ? 1.0 : 0.0);
        Normalize(ref lx, ref ly);

        _state.LeftStickX = ToShort(lx);
        _state.LeftStickY = ToShort(ly);

        // Only override right stick from keys if mouse isn't active
        if (Math.Abs(_mouseStickX) < 0.01 && Math.Abs(_mouseStickY) < 0.01)
        {
            double rx = (_rightRight ? 1.0 : 0.0) - (_rightLeft ? 1.0 : 0.0);
            double ry = (_rightUp ? 1.0 : 0.0) - (_rightDown ? 1.0 : 0.0);
            Normalize(ref rx, ref ry);
            _state.RightStickX = ToShort(rx);
            _state.RightStickY = ToShort(ry);
        }
    }

    private void ApplyMouseToStick(TargetStick stick)
    {
        if (stick == TargetStick.LeftStick)
        {
            _state.LeftStickX = ToShort(_mouseStickX);
            _state.LeftStickY = ToShort(_mouseStickY);
        }
        else
        {
            _state.RightStickX = ToShort(_mouseStickX);
            _state.RightStickY = ToShort(_mouseStickY);
        }
    }

    private void DecayMouseStick()
    {
        const double decay = 0.82;
        _mouseStickX *= decay;
        _mouseStickY *= decay;
        if (Math.Abs(_mouseStickX) < 0.01) _mouseStickX = 0;
        if (Math.Abs(_mouseStickY) < 0.01) _mouseStickY = 0;
    }

    private static void Normalize(ref double x, ref double y)
    {
        double len = Math.Sqrt(x * x + y * y);
        if (len > 1.0) { x /= len; y /= len; }
    }

    private static short ToShort(double v)
        => (short)Math.Clamp(v * 32767.0, -32768, 32767);

    private void Notify()
    {
        // Apply trigger state from button dictionary
        _state.LeftTrigger  = _state.Buttons.GetValueOrDefault("LeftTrigger") ? (byte)255 : (byte)0;
        _state.RightTrigger = _state.Buttons.GetValueOrDefault("RightTrigger") ? (byte)255 : (byte)0;

        ControllerStateChanged?.Invoke(this,
            new ControllerStateChangedEventArgs(
                CopyState(_state),
                _currentProfile?.ControllerType ?? ControllerType.Xbox360));
    }

    private static ControllerState CopyState(ControllerState src) => new()
    {
        Buttons = new Dictionary<string, bool>(src.Buttons),
        LeftStickX = src.LeftStickX,
        LeftStickY = src.LeftStickY,
        RightStickX = src.RightStickX,
        RightStickY = src.RightStickY,
        LeftTrigger = src.LeftTrigger,
        RightTrigger = src.RightTrigger
    };

    public ControllerState GetCurrentState() { lock (_lock) { return CopyState(_state); } }
}
