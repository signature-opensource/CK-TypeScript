using CK.Core;

namespace CK.TypeScript.LiveEngine;

sealed class LocalPackage
{
    readonly FileSystemResourceContainer _resources;

    public LocalPackage( string localResPath, string displayName )
    {
        _resources = new FileSystemResourceContainer( localResPath, displayName );
    }

    /// <summary>
    /// Gets the package "Res/" folder resources.
    /// </summary>
    public FileSystemResourceContainer Resources => _resources;
}

