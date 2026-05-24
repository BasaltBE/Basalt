using System.Runtime.InteropServices;

namespace Basalt.Core.Platform;

public static partial class WindowsTimerResolution
{
    private static bool _enabled;

    public static void Enable()
    {
        if (!OperatingSystem.IsWindows() || _enabled)
        {
            return;
        }

        uint result = timeBeginPeriod(1);
        if (result == 0)
        {
            _enabled = true;
        }
    }

    public static void Disable()
    {
        if (!OperatingSystem.IsWindows() || !_enabled)
        {
            return;
        }

        _ = timeEndPeriod(1);
        _enabled = false;
    }

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeEndPeriod(uint uPeriod);
}
