using CK.EmbeddedResources;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Base class for <see cref="ResourceSpace.FileHandlers"/>.
/// </summary>
public abstract class ResourceSpaceFileHandler
{
    readonly ImmutableArray<string> _fileExtensions;

    /// <summary>
    /// Initializes a new handler that will manage resources with the provided file extensions.
    /// </summary>
    /// <param name="fileExtensions">One or more file extensions that must start with '.' (like ".css").</param>
    protected ResourceSpaceFileHandler( params ImmutableArray<string> fileExtensions )
    {
        Throw.CheckArgument( fileExtensions.Length > 0 && fileExtensions.All( e => e.Length >= 2 && e[0] == '.' ) );
        _fileExtensions = fileExtensions;
    }

    /// <summary>
    /// Gets the file extensions that will be handled by this handler.
    /// </summary>
    public ImmutableArray<string> FileExtensions => _fileExtensions;

    public override string ToString()
    {
        return $"{GetType().Name} - Files '*{_fileExtensions.Concatenate( "', *'" )}'";
    }

    /// <summary>
    /// Provides a filter for <see cref="ResourceLocator"/> and <see cref="ResourceLocator"/>
    /// that are handled by any of the registered <see cref="ResourceSpaceFolderHandler"/>.
    /// </summary>
    public readonly struct FolderExclusion
    {
        readonly string[] _folders;

        internal FolderExclusion( ImmutableArray<ResourceSpaceFolderHandler> folders )
        {
            _folders = folders.Select( f => f.RootFolderName ).ToArray();
        }

        /// <summary>
        /// Checks whether the <paramref name="resource"/> is handled by an
        /// existing <see cref="ResourceSpaceFolderHandler"/>.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>True if the resource is already handled and should be ignored.</returns>
        public bool IsExcluded( ResourceLocator resource ) => IsExcluded( resource.ResourceName );

        /// <summary>
        /// Checks whether the <paramref name="folder"/> is handled by an
        /// existing <see cref="ResourceSpaceFolderHandler"/>.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <returns>True if the folder is already handled and should be ignored.</returns>
        public bool IsExcluded( ResourceFolder folder ) => IsExcluded( folder.FolderName );

        bool IsExcluded( ReadOnlySpan<char> path )
        {
            foreach( var p in _folders )
            {
                int len = p.Length;
                if( path.Length > len
                    && (path[len] == '\\' || path[len] == '/')
                    && path.StartsWith( p, StringComparison.Ordinal ) )
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Must initialize this handler from the ordered set of packages.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="packages">The topological ordered set of packages to consider.</param>
    /// <param name="folderFilter">The filter for resources or folder to use.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal abstract bool Initialize( IActivityMonitor monitor,
                                       ImmutableArray<ResPackage> packages,
                                       FolderExclusion folderFilter );
}
