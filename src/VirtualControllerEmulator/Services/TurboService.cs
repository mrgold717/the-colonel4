using System.Timers;

namespace VirtualControllerEmulator.Services;

public class TurboFiredEventArgs : EventArgs
{
    public string ButtonName { get; }
    public bool IsActive { get; }
    public TurboFiredEventArgs(string buttonName, bool isActive)
    { ButtonName = buttonName; IsActive = isActive; }
}

public class TurboService : IDisposable
{
    private readonly Dictionary<string, TurboEntry> _turboEntries = new();
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler<TurboFiredEventArgs>? TurboFired;

    private class TurboEntry
    {
        public System.Timers.Timer Timer { get; set; } = null!;
        public bool CurrentState { get; set; }
        public int RateHz { get; set; }
    }

    public void EnableTurbo(string buttonName, int rateHz)
    {
        rateHz = Math.Clamp(rateHz, 5, 30);
        lock (_lock)
        {
            if (_turboEntries.ContainsKey(buttonName))
                DisableTurboInternal(buttonName);

            var entry = new TurboEntry { RateHz = rateHz };
            double intervalMs = 1000.0 / (rateHz * 2); // toggle twice per cycle

            var timer = new System.Timers.Timer(intervalMs);
            timer.AutoReset = true;
            timer.Elapsed += (s, e) => OnTurboTick(buttonName);
            entry.Timer = timer;
            _turboEntries[buttonName] = entry;
            timer.Start();
        }
    }

    public void DisableTurbo(string buttonName)
    {
        lock (_lock) { DisableTurboInternal(buttonName); }
    }

    public bool IsTurboActive(string buttonName)
    {
        lock (_lock) { return _turboEntries.ContainsKey(buttonName); }
    }

    public IReadOnlyList<string> GetActiveTurboButtons()
    {
        lock (_lock) { return _turboEntries.Keys.ToList(); }
    }

    public void SetTurboRate(string buttonName, int rateHz)
    {
        lock (_lock)
        {
            if (!_turboEntries.TryGetValue(buttonName, out var entry)) return;
            rateHz = Math.Clamp(rateHz, 5, 30);
            entry.RateHz = rateHz;
            entry.Timer.Interval = 1000.0 / (rateHz * 2);
        }
    }

    private void OnTurboTick(string buttonName)
    {
        lock (_lock)
        {
            if (!_turboEntries.TryGetValue(buttonName, out var entry)) return;
            entry.CurrentState = !entry.CurrentState;
            TurboFired?.Invoke(this, new TurboFiredEventArgs(buttonName, entry.CurrentState));
        }
    }

    private void DisableTurboInternal(string buttonName)
    {
        if (!_turboEntries.TryGetValue(buttonName, out var entry)) return;
        entry.Timer.Stop();
        entry.Timer.Dispose();
        // Ensure button is released
        TurboFired?.Invoke(this, new TurboFiredEventArgs(buttonName, false));
        _turboEntries.Remove(buttonName);
    }

    public void Dispose()
    {
        if (_disposed) return;
        lock (_lock)
        {
            foreach (var key in _turboEntries.Keys.ToList())
                DisableTurboInternal(key);
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
