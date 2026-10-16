using BasaltBE.Basalt.Permissions;
using BasaltBE.Logging;

namespace BasaltBE.DedicatedServer;

internal sealed class PermissionFile : IDisposable
{
    private readonly PermissionStore permissions;
    private readonly string path;
    private readonly Timer autosave;

    public PermissionFile(PermissionStore permissions, string path)
    {
        this.permissions = permissions;
        this.path = Path.GetFullPath(path);
        permissions.Load(this.path);
        autosave = new Timer(_ =>
        {
            try
            {
                permissions.Save(this.path);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Logger.Scoped("Permissions").Error($"Could not save permissions: {error.Message}");
            }
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        using ManualResetEvent stopped = new(false);
        autosave.Dispose(stopped);
        stopped.WaitOne();
        permissions.Save(path);
    }
}
