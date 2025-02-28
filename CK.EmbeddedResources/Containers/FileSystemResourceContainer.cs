using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace CK.EmbeddedResources;

/// <summary>
/// File system implementation of a <see cref="IResourceContainer"/>.
/// <para>
/// By default <see cref="HasLocalFilePathSupport"/> is true but this can be changed when
/// instantiating the container to prrvent direct access to the file system.
/// </para>
/// </summary>
public sealed class FileSystemResourceContainer : IResourceContainer
{
    readonly string _displayName;
    readonly string _root;
    readonly bool _alwaysValid;
    readonly bool _allowLocalFilePath;

    /// <summary>
    /// Initializes a new <see cref="FileSystemResourceContainer"/>.
    /// </summary>
    /// <param name="root">The root directory. This should be an absolute path.</param>
    /// <param name="displayName">The <see cref="DisplayName"/> for this container.</param>
    /// <param name="alwaysValid">
    /// By default, this is valid even if the <see cref="ResourcePrefix"/> (the root directory)
    /// doesn't exist on the file system: it is considered as being empty.
    /// <para>
    /// When set to true, <see cref="IsValid"/> is true only when the root directory exists. 
    /// </para>
    /// </param>
    /// <param name="allowLocalFilePath">
    /// Gets whether <see cref="GetLocalFilePath(in ResourceLocator)"/> returns the file path.
    /// <para>
    /// When set to false, this container hides the fact that it is bound to the file system: it behaves
    /// like a purely readonly resource container.
    /// </para>
    /// </param>
    public FileSystemResourceContainer( string root,
                                        string displayName,
                                        bool alwaysValid = true,
                                        bool allowLocalFilePath = true )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( displayName );
        Throw.CheckArgument( Path.IsPathFullyQualified( root ) );
        _displayName = displayName;
        _alwaysValid = alwaysValid;
        _allowLocalFilePath = allowLocalFilePath;
        root = Path.GetFullPath( root );
        if( !Path.EndsInDirectorySeparator( root ) ) root += Path.DirectorySeparatorChar;
        _root = root;
    }

    /// <summary>
    /// Gets whether the <see cref="ResourcePrefix"/> that is the root directory exists on the file system.
    /// </summary>
    public bool IsValid => _alwaysValid || Directory.Exists( _root );

    /// <inheritdoc />
    /// <remarks>
    /// This is the root directory ending with a <see cref="Path.DirectorySeparatorChar"/>.
    /// </remarks>
    public string ResourcePrefix => _root;

    /// <inheritdoc />
    public string DisplayName => _displayName;

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> AllResources
    {
        get
        {
            if( Directory.Exists( _root ) )
            {
                foreach( var f in Directory.EnumerateFiles( _root, "*", SearchOption.AllDirectories ) )
                {
                    yield return new ResourceLocator( this, f );
                }
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetAllResource( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        if( Directory.Exists( _root ) )
        {
            foreach( var f in Directory.EnumerateFiles( folder.FolderName, "*", SearchOption.AllDirectories ) )
            {
                yield return new ResourceLocator( this, f );
            }
        }
    }

    /// <inheritdoc />
    public bool HasLocalFilePathSupport => _allowLocalFilePath;

    /// <inheritdoc />
    public string? GetLocalFilePath( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        return !_allowLocalFilePath ? null : resource.FullResourceName;
    }

    /// <inheritdoc />
    public StringComparer NameComparer => StringComparer.Ordinal;

    /// <inheritdoc />
    public Stream GetStream( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        return File.OpenRead( resource.FullResourceName );
    }

    /// <inheritdoc />
    public void WriteStream( in ResourceLocator resource, Stream target )
    {
        resource.CheckContainer( this );
        using var source = File.OpenRead( resource.FullResourceName );
        source.CopyTo( target );
    }

    /// <inheritdoc />
    public ResourceLocator GetResource( ReadOnlySpan<char> localResourceName ) => DoGetResource( _root, localResourceName );

    /// <inheritdoc />
    public ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> localResourceName )
    {
        folder.CheckContainer( this );
        return DoGetResource( folder.FolderName, localResourceName );
    }

    ResourceLocator DoGetResource( string prefix, ReadOnlySpan<char> localResourceName )
    {
        if( localResourceName.Length > 0 && (localResourceName[0] == '/' || localResourceName[0] == '\\') )
        {
            localResourceName = localResourceName.Slice( 1 );
        }
        // This normalizes the Path.DirectorySeparatorChar.
        // File.Exists calls it anyway.
        var p = Path.GetFullPath( String.Concat( prefix, localResourceName ) );
        if( File.Exists( p ) )
        {
            return new ResourceLocator( this, p );
        }
        return default;
    }

    /// <inheritdoc />
    public ResourceFolder GetFolder( ReadOnlySpan<char> localFolderName ) => DoGetFolder( _root, localFolderName );

    /// <inheritdoc />
    public ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> localFolderName )
    {
        folder.CheckContainer( this );
        return DoGetFolder( folder.FolderName, localFolderName );
    }

    ResourceFolder DoGetFolder( string prefix, ReadOnlySpan<char> localFolderName )
    {
        if( localFolderName.Length > 0 && (localFolderName[0] == '/' || localFolderName[0] == '\\') )
        {
            localFolderName = localFolderName.Slice( 1 );
        }
        var p = Path.GetFullPath( String.Concat( prefix, localFolderName ) );
        if( Directory.Exists( p ) )
        {
            return new ResourceFolder( this, Path.EndsInDirectorySeparator( p )
                                                ? p
                                                : p + Path.DirectorySeparatorChar );
        }
        return default;
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetAllResources( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        foreach( var f in Directory.EnumerateFiles( folder.FolderName, "*", SearchOption.AllDirectories ) )
        {
            yield return new ResourceLocator( this, f );
        }
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetResources( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        foreach( var f in Directory.EnumerateFiles( folder.FolderName ) )
        {
            yield return new ResourceLocator( this, f );
        }
    }

    /// <inheritdoc />
    public IEnumerable<ResourceFolder> GetFolders( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        foreach( var f in Directory.EnumerateDirectories( folder.FolderName ) )
        {
            Throw.DebugAssert( f[^1] != Path.DirectorySeparatorChar );
            yield return new ResourceFolder( this, f + Path.DirectorySeparatorChar );
        }
    }

    /// <inheritdoc />
    public ReadOnlySpan<char> GetFolderName( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        var s = folder.LocalFolderName.Span;
        return s.Length != 0 ? Path.GetFileName( s.Slice( 0, s.Length - 1 ) ) : s;
    }

    /// <inheritdoc />
    public ReadOnlySpan<char> GetResourceName( ResourceLocator resource )
    {
        resource.CheckContainer( this );
        return Path.GetFileName( resource.ResourceName.Span );
    }

    /// <inheritdoc />
    public override string ToString() => _displayName;
}
