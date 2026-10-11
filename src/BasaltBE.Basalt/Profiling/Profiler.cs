using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using bottlenoselabs.C2CS.Runtime;
using static Tracy.PInvoke;

namespace BasaltBE.Basalt.Profiling;

public static class Profiler {
    private static readonly ConcurrentDictionary<string, CString> CStringCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<SourceLocation, ulong> SourceLocationCache = new();

    public static bool Enabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProfilerZone BeginZone(
        string? zoneName = null,
        uint color = 0,
        [CallerLineNumber] uint lineNumber = 0,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null
    ) {
        if (!Enabled) return default;

        SourceLocation sourceLocation = new(zoneName, filePath, memberName, lineNumber, color);
        ulong sourceLocationId = SourceLocationCache.GetOrAdd(sourceLocation, static location => {
            CString file = GetCachedCString(location.FilePath, out ulong fileLength);
            CString member = GetCachedCString(location.MemberName, out ulong memberLength);
            CString name = GetCachedCString(location.Name, out ulong nameLength);
            return TracyAllocSrclocName(
                location.LineNumber,
                file,
                fileLength,
                member,
                memberLength,
                name,
                nameLength,
                location.Color
            );
        });
        TracyCZoneCtx context = TracyEmitZoneBeginAlloc(sourceLocationId, 1);
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

    private static CString GetOrCreateCString(string value) =>
        CStringCache.GetOrAdd(value, static item => CString.FromString(item));

    private readonly record struct SourceLocation(
        string? Name,
        string? FilePath,
        string? MemberName,
        uint LineNumber,
        uint Color
    );
}
