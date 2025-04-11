using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Handles <see cref="ResourceSpaceData"/> and enables <see cref="ResourceSpaceFolderHandler"/>
/// and <see cref="ResourceSpaceFileHandler"/> to be registered in order to produce a
/// final <see cref="ResourceSpaceData"/>.
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

    /// <summary>
    /// Gets the <see cref="ResourceSpaceData"/>.
    /// </summary>
    public ResourceSpaceData SpaceData => _spaceData;

    /// <summary>
    /// Gets or sets the configured Code generated resource container.
    /// This can only be set if this has not been previously set (ie. this is null).
    /// See <see cref="ResourceSpaceConfiguration.GeneratedCodeContainer"/>.
    /// <para>
    /// This is the last possibility to set the generated code.
    /// </para>
    /// </summary>
    [DisallowNull]
    public IResourceContainer? GeneratedCodeContainer
    {
        get => _spaceData.GeneratedCodeContainer;
        set => _spaceData.GeneratedCodeContainer = value;
    }

    /// <summary>
    /// Adds a <see cref="ResourceSpaceFileHandler"/>.
    /// <para>
    /// If a handler with a common <see cref="ResourceSpaceHandler.FileExtensions"/> already
    /// exists, a warnig is emitted.
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
            monitor.Warn( $"Handler '{h}' has some file extension in common with at least '{conflict.First()}'." );
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
        var codeGen = _spaceData.GeneratedCodeContainer;
        if( codeGen == null )
        {
            // Definitly assigns the code generated container. This is a no-op
            // for the ResourceContainerWrapper.InnerContainer (assigned to itself)
            // but this "publish" the wrapped empty container in the _generatedCodeContainer
            // space data field.
            // This "closes" the possibilty to re-assign it.
            _spaceData.GeneratedCodeContainer = _spaceData.CodePackage.AfterResources.Resources;
        }
        var space = new ResourceSpace( _spaceData, _folderHandlers.ToImmutableArray(), _fileHandlers.ToImmutableArray() );
        return space.Initialize( monitor )
                ? space
                : null;
    }
}
