using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BasaltBE.Basalt.Worlds.Ticking;

public sealed class TickTimer : IDisposable {
    private readonly ManualResetEventSlim stop;
    private readonly Action? poll;
    private readonly WaitHandle[] handles = [];
    private WindowsTimer? timer;
    private long sleepOvershoot = Stopwatch.Frequency / 1000;
    private bool disposed;
    private long pollDuration;

    public TickTimer(ManualResetEventSlim stop, Action? poll = null) {
        ArgumentNullException.ThrowIfNull(stop);
        this.stop = stop;
        this.poll = poll;
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134)) return;

        WindowsTimer candidate = new();
        if (candidate.SafeWaitHandle.IsInvalid) {
            candidate.Dispose();
            return;
        }

        timer = candidate;
        handles = [stop.WaitHandle, candidate];
    }

    public bool Wait(long deadline) {
        ObjectDisposedException.ThrowIf(disposed, this);
        while (!stop.IsSet) {
            long now = Stopwatch.GetTimestamp();
            long remaining = deadline - now;
            if (remaining <= 0) return true;

            long margin = Math.Clamp(sleepOvershoot, Stopwatch.Frequency / 10_000, Stopwatch.Frequency / 500);
            if (remaining <= margin) {
                Thread.SpinWait(1);
                continue;
            }

            if (poll is not null && remaining > margin + pollDuration) {
                poll();
                long finished = Stopwatch.GetTimestamp();
                pollDuration = Math.Max(finished - now, pollDuration * 31 / 32);
                if (finished >= deadline) return !stop.IsSet;
                remaining = deadline - finished;
            }
            if (stop.IsSet) return false;

            long requested = remaining - margin;
            if (poll is not null) requested = Math.Min(requested, Stopwatch.Frequency / 200);
            if (requested <= 0) continue;

            long before = Stopwatch.GetTimestamp();
            if (timer is not null) {
                long due = -(long)Math.Ceiling(requested * 10000000.0 / Stopwatch.Frequency);
                if (!SetWaitableTimer(timer.SafeWaitHandle, ref due, 0, 0, 0, false)) {
                    timer.Dispose();
                    timer = null;
                    continue;
                }
                if (WaitHandle.WaitAny(handles) == 0) return false;
            } else {
                int milliseconds = (int)Math.Min(int.MaxValue, requested * 1000 / Stopwatch.Frequency);
                if (milliseconds <= 0) {
                    Thread.SpinWait(1);
                    continue;
                }
                requested = milliseconds * Stopwatch.Frequency / 1000;
                if (stop.Wait(milliseconds)) return false;
            }

            long after = Stopwatch.GetTimestamp();
            long overshoot = Math.Max(0, after - before - requested);
            sleepOvershoot = Math.Clamp(Math.Max(overshoot, sleepOvershoot * 31 / 32),
                Stopwatch.Frequency / 10_000, Stopwatch.Frequency / 500);
            if (after >= deadline) return !stop.IsSet;
        }
        return false;
    }

    public void Dispose() {
        if (disposed) return;
        disposed = true;
        timer?.Dispose();
    }

    private sealed class WindowsTimer : WaitHandle {
        internal WindowsTimer() {
            SafeWaitHandle = CreateWaitableTimerExW(0, 0, 2, 0x100002);
        }
    }

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern SafeWaitHandle CreateWaitableTimerExW(nint attributes, nint name, uint flags, uint access);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWaitableTimer(
        SafeWaitHandle timer, ref long due, int period, nint callback, nint argument,
        [MarshalAs(UnmanagedType.Bool)] bool resume
    );
}
