using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using VirtualControllerEmulator.Helpers;

namespace VirtualControllerEmulator.Services;

public class MouseMoveEventArgs : EventArgs
{
    public int DeltaX { get; }
    public int DeltaY { get; }
    public int AbsoluteX { get; }
    public int AbsoluteY { get; }
    public MouseMoveEventArgs(int dx, int dy, int absX, int absY)
    { DeltaX = dx; DeltaY = dy; AbsoluteX = absX; AbsoluteY = absY; }
}

public class MouseWheelEventArgs : EventArgs
{
    public int Delta { get; }
    public MouseWheelEventArgs(int delta) { Delta = delta; }
}

public class MouseButtonEventArgs : EventArgs
{
    public int Button { get; }
    public bool IsPressed { get; }
    public MouseButtonEventArgs(int button, bool pressed) { Button = button; IsPressed = pressed; }
}

public class KeyEventArgs : EventArgs
{
    public int VkCode { get; }
    public KeyEventArgs(int vkCode) { VkCode = vkCode; }
}

public class InputCaptureService : IDisposable
{
    private IntPtr _keyboardHookHandle = IntPtr.Zero;
    private IntPtr _mouseHookHandle = IntPtr.Zero;
    private NativeMethods.HookProc? _keyboardProc;
    private NativeMethods.HookProc? _mouseProc;
    private bool _disposed;
    private bool _suppressInput;

    private int _lastMouseX;
    private int _lastMouseY;
    private bool _mouseInitialized;

    public event EventHandler<KeyEventArgs>? KeyPressed;
    public event EventHandler<KeyEventArgs>? KeyReleased;
    public event EventHandler<MouseMoveEventArgs>? MouseMoved;
    public event EventHandler<MouseButtonEventArgs>? MouseButtonChanged;
    public event EventHandler<MouseWheelEventArgs>? MouseWheelScrolled;

    public bool SuppressInput
    {
        get => _suppressInput;
        set => _suppressInput = value;
    }

    public void StartCapturing()
    {
        if (_keyboardHookHandle != IntPtr.Zero) return;

        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        var moduleHandle = NativeMethods.GetModuleHandle(curModule.ModuleName);

        _keyboardHookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);

        _mouseHookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);

        if (_keyboardHookHandle == IntPtr.Zero || _mouseHookHandle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to install input hooks.");
    }

    public void StopCapturing()
    {
        if (_keyboardHookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHookHandle);
            _keyboardHookHandle = IntPtr.Zero;
        }
        if (_mouseHookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookHandle);
            _mouseHookHandle = IntPtr.Zero;
        }
        _mouseInitialized = false;
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            int vkCode = (int)hookStruct.vkCode;

            bool isKeyDown = wParam == NativeMethods.WM_KEYDOWN || wParam == NativeMethods.WM_SYSKEYDOWN;
            bool isKeyUp = wParam == NativeMethods.WM_KEYUP || wParam == NativeMethods.WM_SYSKEYUP;

            if (isKeyDown)
                DispatchEvent(() => KeyPressed?.Invoke(this, new KeyEventArgs(vkCode)));
            else if (isKeyUp)
                DispatchEvent(() => KeyReleased?.Invoke(this, new KeyEventArgs(vkCode)));

            if (_suppressInput)
                return new IntPtr(1);
        }
        return NativeMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            int msg = wParam.ToInt32();

            switch (msg)
            {
                case NativeMethods.WM_MOUSEMOVE:
                    HandleMouseMove(hookStruct);
                    break;
                case NativeMethods.WM_LBUTTONDOWN:
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(0x01, true)));
                    break;
                case NativeMethods.WM_LBUTTONUP:
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(0x01, false)));
                    break;
                case NativeMethods.WM_RBUTTONDOWN:
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(0x02, true)));
                    break;
                case NativeMethods.WM_RBUTTONUP:
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(0x02, false)));
                    break;
                case NativeMethods.WM_MBUTTONDOWN:
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(0x04, true)));
                    break;
                case NativeMethods.WM_MBUTTONUP:
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(0x04, false)));
                    break;
                case NativeMethods.WM_XBUTTONDOWN:
                    int xBtn = (hookStruct.mouseData >> 16) == 1 ? 0x05 : 0x06;
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(xBtn, true)));
                    break;
                case NativeMethods.WM_XBUTTONUP:
                    int xBtnUp = (hookStruct.mouseData >> 16) == 1 ? 0x05 : 0x06;
                    DispatchEvent(() => MouseButtonChanged?.Invoke(this, new MouseButtonEventArgs(xBtnUp, false)));
                    break;
                case NativeMethods.WM_MOUSEWHEEL:
                    int wheelDelta = (short)(hookStruct.mouseData >> 16);
                    DispatchEvent(() => MouseWheelScrolled?.Invoke(this, new MouseWheelEventArgs(wheelDelta)));
                    break;
            }

            if (_suppressInput)
                return new IntPtr(1);
        }
        return NativeMethods.CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    private void HandleMouseMove(NativeMethods.MSLLHOOKSTRUCT hookStruct)
    {
        int currentX = hookStruct.pt.x;
        int currentY = hookStruct.pt.y;

        if (!_mouseInitialized)
        {
            _lastMouseX = currentX;
            _lastMouseY = currentY;
            _mouseInitialized = true;
            return;
        }

        int dx = currentX - _lastMouseX;
        int dy = currentY - _lastMouseY;
        _lastMouseX = currentX;
        _lastMouseY = currentY;

        if (dx != 0 || dy != 0)
            DispatchEvent(() => MouseMoved?.Invoke(this, new MouseMoveEventArgs(dx, dy, currentX, currentY)));
    }

    private static void DispatchEvent(Action action)
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            Application.Current.Dispatcher.BeginInvoke(action);
        else
            action();
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopCapturing();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
