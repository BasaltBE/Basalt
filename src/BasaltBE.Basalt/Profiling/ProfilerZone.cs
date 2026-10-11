using static Tracy.PInvoke;

namespace BasaltBE.Basalt.Profiling;

public readonly struct ProfilerZone : IDisposable {
    public readonly TracyCZoneCtx Context;

    internal ProfilerZone(TracyCZoneCtx context) {
        Context = context;
    }

    public void Dispose() {
        if (Profiler.Enabled)
            TracyEmitZoneEnd(Context);
    }
}
