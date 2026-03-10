using System.Diagnostics;
using System.Timers;
using VirtualControllerEmulator.Helpers;

namespace VirtualControllerEmulator.Services;

public class ActiveProcessChangedEventArgs : EventArgs
{
    public string ProcessName { get; }
    public uint ProcessId { get; }
    public ActiveProcessChangedEventArgs(string processName, uint processId)
    { ProcessName = processName; ProcessId = processId; }
}

public class ProcessMonitorService : IDisposable
{
    private readonly System.Timers.Timer _pollTimer;
    private string _lastProcessName = string.Empty;
    private uint _lastProcessId;
    private bool _disposed;

    public event EventHandler<ActiveProcessChangedEventArgs>? ActiveProcessChanged;

    public string CurrentProcessName => _lastProcessName;

    public ProcessMonitorService()
    {
        _pollTimer = new System.Timers.Timer(500);
        _pollTimer.AutoReset = true;
        _pollTimer.Elapsed += OnPollTimerElapsed;
    }

    public void Start()
    {
        _pollTimer.Start();
    }

    public void Stop()
    {
        _pollTimer.Stop();
    }

    private void OnPollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == _lastProcessId) return;

            string processName = GetProcessName(processId);
            _lastProcessId = processId;
            _lastProcessName = processName;

            ActiveProcessChanged?.Invoke(this, new ActiveProcessChangedEventArgs(processName, processId));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ProcessMonitor error: {ex.Message}");
        }
    }

    private static string GetProcessName(uint processId)
    {
        try
        {
            var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool IsMatchingProcess(string? linkedProcess)
    {
        if (string.IsNullOrWhiteSpace(linkedProcess)) return false;
        return string.Equals(_lastProcessName, linkedProcess, StringComparison.OrdinalIgnoreCase) ||
               _lastProcessName.Contains(linkedProcess, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _pollTimer.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
