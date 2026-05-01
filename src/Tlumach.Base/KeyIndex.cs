// Shared index — lives for the duration of the VS session
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

#if GENERATOR
namespace Tlumach.Generator;
#else
namespace Tlumach.Base;
#endif

public static class KeyIndex
{
#if NETSTANDARD
    private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
    private static readonly bool _isWindows = OperatingSystem.IsWindows();
#endif
    // identifier name > (file path, line, column)
    private static readonly Dictionary<string, KeyLocation> _index = new(_isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
#if NETSTANDARD
    private static readonly object _indexLock = new();
#else
    private static readonly Lock _indexLock = new();
#endif

    public static bool IsPopulated => _index.Count > 0;

    public static void Register(string? @namespace, string? className, string identifier, KeyLocation keyLocation)
    {
        lock (_indexLock)
        {
            _index[MakeFullKey(@namespace, className, identifier)] = keyLocation;
        }
    }

    public static KeyLocation? FindDeclaration(string? @namespace, string? className, string identifier)
    {
        string partialKey = MakeFullKey(@namespace, className, identifier);
        string partialKeyWithDot = '.' + partialKey;
        lock (_indexLock)
        {
            foreach (var key in _index.Keys)
            {
                if (key.Equals(partialKey) || key.EndsWith(partialKeyWithDot, StringComparison.OrdinalIgnoreCase))
                    return _index[key];
            }
        }

        return null;
    }

    private static string MakeFullKey(string? @namespace, string? className, string identifier)
    {
        if (!string.IsNullOrEmpty(@namespace) && !string.IsNullOrEmpty(className))
            return string.Join(".", @namespace, className, identifier);

        if (!string.IsNullOrEmpty(@namespace))
            return string.Join(".", @namespace, identifier);

        if (!string.IsNullOrEmpty(className))
            return string.Join(".", className, identifier);

        return identifier;
    }

    /// <summary>
    /// Removes all index entries associated with the specified file path.
    /// </summary>
    /// <remarks>Call this method before re-analyzing a file to ensure that outdated or stale index entries
    /// are removed. This helps maintain the accuracy of the index when files are updated or reprocessed.</remarks>
    /// <param name="filePath">The path of the file whose related index entries are to be cleared. Cannot be null or empty.</param>
    public static void ClearFile(string filePath)
    {
        lock (_indexLock)
        {
            // Call this before re-analyzing a file, to remove stale entries
            foreach (var key in _index.Keys)
            {
                if (_index.TryGetValue(key, out var loc) && filePath.Equals(loc.FilePath, _isWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    _index.Remove(key);
            }
        }
    }
}
