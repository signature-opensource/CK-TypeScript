using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Builder for <see cref="ResourceSpace"/>.
/// </summary>
public sealed class ResourceSpaceBuilder
{
    readonly ResourceSpaceData _spaceData;
    readonly List<ResourceSpaceFolderHandler> _folderHandlers;
    readonly List<ResourceSpaceFileHandler> _fileHandlers;

    public ResourceSpaceBuilder( ResourceSpaceData spaceData )
    {
        _spaceData = spaceData;
        _folderHandlers = new List<ResourceSpaceFolderHandler>();
        _fileHandlers = new List<ResourceSpaceFileHandler>();
    }

    public ResourceSpaceData SpaceData => _spaceData;

    /// <summary>
    /// Adds a <see cref="ResourceSpaceFileHandler"/>.
    /// <para>
    /// If a handler with a common <see cref="ResourceSpaceHandler.FileExtensions"/> already
    /// exists, this is an error.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="h">The handler to register.</param>
    /// <returns>True on success, false on error.</returns>
    public bool RegisterHandler( IActivityMonitor monitor, ResourceSpaceFileHandler h )
    {
        Throw.CheckNotNullArgument( h );
        if( _fileHandlers.Contains( h ) )
        {
            monitor.Warn( $"Duplicate handler registration for '{h}'. Ignored." );
            return true;
        }
        var conflict = _fileHandlers.Where(
                            existing => existing.FileExtensions.Any(
                                            f => h.FileExtensions.Any( e => e.Equals( f, StringComparison.OrdinalIgnoreCase ) ) ) );
        if( conflict.Any() )
        {
            monitor.Error( $"Unable to add handler '{h}'. At least one file extension is already handled by '{conflict.First()}'." );
            return false;
        }
        _fileHandlers.Add( h );
        return true;
    }

    /// <summary>
    /// Adds a <see cref="ResourceSpaceFolderHandler"/>. 
    /// <para>
    /// If a handler with the same <see cref="ResourceSpaceFolderHandler.RootFolderName"/> already
    /// exists, this is an error.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="h">The handler to register.</param>
    /// <returns>True on success, false on error.</returns>
    public bool RegisterHandler( IActivityMonitor monitor, ResourceSpaceFolderHandler h )
    {
        Throw.CheckNotNullArgument( h );
        if( _folderHandlers.Contains( h ) )
        {
            monitor.Warn( $"Duplicate handler registration for '{h}'. Ignored." );
            return true;
        }
        var conflict = _folderHandlers.Where( existing => h.RootFolderName.Equals( existing.RootFolderName, StringComparison.OrdinalIgnoreCase ) );
        if( conflict.Any() )
        {
            monitor.Error( $"Unable to add handler '{h}'. This folder is already handled by {conflict.First()}." );
            return false;
        }
        _folderHandlers.Add( h );
        return true;
    }

    /// <summary>
    /// Tries to build the <see cref="ResourceSpace"/>. 
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The space on success, null otherwise.</returns>
    public ResourceSpace? Build( IActivityMonitor monitor )
    {
        var space = new ResourceSpace( _spaceData, _folderHandlers.ToImmutableArray(), _fileHandlers.ToImmutableArray() );
        return space.Initialize( monitor )
                ? space
                : null;
    }
}
