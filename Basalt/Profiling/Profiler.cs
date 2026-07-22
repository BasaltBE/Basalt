namespace Basalt.Core.Profiling;

using System.Runtime.CompilerServices;
using bottlenoselabs.C2CS.Runtime;
using static Tracy.PInvoke;

public static class Profiler {
    public const bool Enabled = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProfilerZone BeginZone(
        string? zoneName = null,
        uint color = 0,
        [CallerLineNumber] uint lineNumber = 0,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null) {
#pragma warning disable CS0162
        if (!Enabled) return default;
#pragma warning restore CS0162

        CString fileStr = GetCString(filePath, out ulong fileLn);
        CString memberStr = GetCString(memberName, out ulong memberLn);
        CString nameStr = GetCString(zoneName, out ulong nameLn);

        ulong srcLocId = TracyAllocSrclocName(lineNumber, fileStr, fileLn, memberStr, memberLn, nameStr, nameLn, color);
        TracyCZoneCtx context = TracyEmitZoneBeginAlloc(srcLocId, 1);
        return new ProfilerZone(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FrameMark() {
#pragma warning disable CS0162
        if (!Enabled) return;
#pragma warning restore CS0162
        TracyEmitFrameMark(default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetThreadName(string name) {
#pragma warning disable CS0162
        if (!Enabled) return;
#pragma warning restore CS0162
        TracySetThreadName(CString.FromString(name));
    }

    internal static CString GetCString(string? value, out ulong length) {
        if (value is null) {
            length = 0;
            return new CString(0);
        }

        length = (ulong)value.Length;
        return CString.FromString(value);
    }
}
