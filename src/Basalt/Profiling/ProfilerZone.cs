namespace Basalt.Core.Profiling;

using static Tracy.PInvoke;

public readonly struct ProfilerZone : IDisposable {
    public readonly TracyCZoneCtx Context;

    internal ProfilerZone(TracyCZoneCtx context) {
        Context = context;
    }

    public void Dispose() {
        if (Profiler.Enabled) {
            TracyEmitZoneEnd(Context);
        }
    }
}
