namespace Basalt.Core;

using System.Reflection;

/// <summary>
/// Provides access to embedded Protocol/Data JSON resources.
/// </summary>
internal static class ProtocolData
{
  private static readonly Assembly DataAssembly = typeof(ProtocolData).Assembly;

  /// <summary>
  /// Opens a read-only stream for the specified data file name (e.g. "block_types.json").
  /// Returns null if the resource does not exist.
  /// </summary>
  public static Stream? Open(string fileName)
  {
    string resourceName = $"Protocol.Data.{fileName}";
    return DataAssembly.GetManifestResourceStream(resourceName);
  }

  /// <summary>
  /// Opens a required data file stream. Throws if the resource is missing.
  /// </summary>
  public static Stream Require(string fileName)
  {
    return Open(fileName)
      ?? throw new FileNotFoundException($"Embedded resource 'Protocol.Data.{fileName}' not found.");
  }
}
