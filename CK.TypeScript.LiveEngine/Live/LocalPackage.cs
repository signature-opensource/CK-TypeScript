using CK.Core;

namespace CK.TypeScript.LiveEngine;

public sealed class LocalPackage
{
    readonly FileSystemResourceContainer _resources;

    public LocalPackage( string localResPath, string displayName )
    {
        _resources = new FileSystemResourceContainer( localResPath, displayName );
    }

    /// <summary>
    /// Gets the full path "Res\" (ending with <see cref="Path.DirectorySeparatorChar"/>).
    /// </summary>
    public string ResPath => _resources.ResourcePrefix;

    /// <summary>
    /// Gets the package "Res/" folder resources.
    /// </summary>
    public FileSystemResourceContainer Resources => _resources;
}

