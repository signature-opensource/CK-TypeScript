using CK.EmbeddedResources;
using System;
using System.IO;

namespace CK.Core;

/// <summary>
/// Captures a file or folder change event.
/// </summary>
public sealed class PathChangedEvent
{
    readonly IResPackageResources _resources;
    readonly string _fullPath;
    bool? _fileExists;
    bool? _pathExists;

    /// <summary>
    /// Initializes a new <see cref="PathChangedEvent"/>.
    /// </summary>
    /// <param name="resources">The resources that contains the changed path.</param>
    /// <param name="fullPath">The full changed path.</param>
    public PathChangedEvent( IResPackageResources resources, string fullPath )
    {
        _resources = resources;
        _fullPath = fullPath;
    }

    /// <summary>
    /// Gets the sub path relative to <see cref="Resources"/>.
    /// </summary>
    public ReadOnlySpan<char> SubPath
    {
        get
        {
            Throw.DebugAssert( Resources.LocalPath != null );
            return FullPath.AsSpan( Resources.LocalPath.Length );
        }
    }

    /// <summary>
    /// Gets the resources that contains the changed path.
    /// <see cref="IResPackageResources.LocalPath"/> is necessarily not null.
    /// </summary>
    public IResPackageResources Resources => _resources;

    /// <summary>
    /// Gets the full changed path.
    /// </summary>
    public string FullPath => _fullPath;

    /// <summary>
    /// Gets whether this <see cref="FullPath"/> is a file that exists.
    /// </summary>
    public bool FileExists => _fileExists ??= File.Exists( FullPath );

    /// <summary>
    /// Gets whether this <see cref="FullPath"/> is a path that exists.
    /// </summary>
    public bool PathExists => _pathExists ??= Path.Exists( FullPath );
}
