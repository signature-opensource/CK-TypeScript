using System;
using System.Collections.Generic;
using System.IO;

namespace CK.Core;

/// <summary>
/// File system implementation of a <see cref="IResourceContainer"/>.
/// </summary>
public sealed class FileSystemResourceContainer : IResourceContainer
{
    readonly string _displayName;
    readonly string _root;

    /// <summary>
    /// Iniitalizes a new <see cref="FileSystemResourceContainer"/>.
    /// </summary>
    /// <param name="root">The root directory. This should be an absolute path.</param>
    /// <param name="displayName">The <see cref="DisplayName"/> for this container.</param>
    /// <param name="filters">Specifies which files or directories are excluded.</param>
    public FileSystemResourceContainer( string root, string displayName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( displayName );
        Throw.CheckArgument( Path.IsPathRooted( root ) );
        _displayName = displayName;
        _root = Path.GetFullPath( root ) + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Gets whether the <see cref="ResourcePrefix"/> that is the root directory exists on the file system.
    /// </summary>
    public bool IsValid => Directory.Exists( _root );

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
            foreach( var f in Directory.EnumerateFiles( _root, "*", SearchOption.AllDirectories ) )
            {
                yield return new ResourceLocator( this, f );
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetAllResource( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        foreach( var f in Directory.EnumerateFiles( folder.FolderName, "*", SearchOption.AllDirectories ) )
        {
            yield return new ResourceLocator( this, f );
        }
    }

    /// <inheritdoc />
    public StringComparer NameComparer => StringComparer.Ordinal;

    /// <inheritdoc />
    public Stream GetStream( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        return File.OpenRead( resource.ResourceName );
    }

    /// <inheritdoc />
    public void WriteStream( in ResourceLocator resource, Stream target )
    {
        resource.CheckContainer( this );
        using var source = File.OpenRead( resource.ResourceName );
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
        var name = String.Concat( prefix, localResourceName );
        if( File.Exists( name ) )
        {
            return new ResourceLocator( this, name.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar ) );
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

    private ResourceFolder DoGetFolder( string prefix, ReadOnlySpan<char> localFolderName )
    {
        if( localFolderName.Length > 0 && (localFolderName[0] == '/' || localFolderName[0] == '\\') )
        {
            localFolderName = localFolderName.Slice( 1 );
        }
        var name = String.Concat( prefix, localFolderName );
        if( Directory.Exists( name ) )
        {
            return new ResourceFolder( this, name.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar ) + Path.DirectorySeparatorChar );
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
            Throw.DebugAssert( f[f.Length - 1] != Path.DirectorySeparatorChar );
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
        return Path.GetFileName( resource.LocalResourceName.Span );
    }

    /// <inheritdoc />
    public override string ToString() => _displayName;
}
