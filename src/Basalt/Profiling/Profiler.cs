namespace Basalt.Core.Profiling;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using bottlenoselabs.C2CS.Runtime;
using static Tracy.PInvoke;

public static class Profiler {
    public static bool Enabled;

    private static readonly ConcurrentDictionary<string, CString> CStringCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<SourceLocation, ulong> SourceLocationCache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProfilerZone BeginZone(
        string? zoneName = null,
        uint color = 0,
        [CallerLineNumber] uint lineNumber = 0,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null) {
        if (!Enabled) return default;

        SourceLocation sourceLocation = new(zoneName, filePath, memberName, lineNumber, color);
        ulong srcLocId = SourceLocationCache.GetOrAdd(sourceLocation, static location => {
            CString fileStr = GetCachedCString(location.FilePath, out ulong fileLn);
            CString memberStr = GetCachedCString(location.MemberName, out ulong memberLn);
            CString nameStr = GetCachedCString(location.Name, out ulong nameLn);
            return TracyAllocSrclocName(
                location.LineNumber,
                fileStr,
                fileLn,
                memberStr,
                memberLn,
                nameStr,
                nameLn,
                location.Color);
        });
        TracyCZoneCtx context = TracyEmitZoneBeginAlloc(srcLocId, 1);
        return new ProfilerZone(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FrameMark() {
        if (!Enabled) return;
        TracyEmitFrameMark(default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetThreadName(string name) {
        if (!Enabled) return;
        TracySetThreadName(GetOrCreateCString(name));
    }

    private static CString GetCachedCString(string? value, out ulong length) {
        if (value is null) {
            length = 0;
            return new CString(0);
        }

        length = (ulong)value.Length;
        return GetOrCreateCString(value);
    }

    private static CString GetOrCreateCString(string value) {
        return CStringCache.GetOrAdd(value, static v => CString.FromString(v));
    }

    private readonly record struct SourceLocation(
        string? Name,
        string? FilePath,
        string? MemberName,
        uint LineNumber,
        uint Color);
}
