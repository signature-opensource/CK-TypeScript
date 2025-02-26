using CK.Core;

namespace CK.TypeScript.LiveEngine;

/// <summary>
/// Captures a local package. This is basically its <see cref="Resources"/>.
/// </summary>
public sealed class LocalPackage
{
    readonly FileSystemResourceContainer _resources;
    readonly NormalizedPath _typeScriptFolder;

    public LocalPackage( FileSystemResourceContainer resources, NormalizedPath typeScriptFolder )
    {
        _resources = resources;
        _typeScriptFolder = typeScriptFolder;
    }

    /// <summary>
    /// Gets the full path "Res\" (ending with <see cref="Path.DirectorySeparatorChar"/>).
    /// </summary>
    public string ResPath => _resources.ResourcePrefix;

    /// <summary>
    /// Gets the package "Res/" folder resources.
    /// </summary>
    public FileSystemResourceContainer Resources => _resources;

    /// <summary>
    /// Gets the relative path in the ck-gen/ folder for this package.
    /// </summary>
    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    public override string ToString() => _typeScriptFolder.Path;
}

