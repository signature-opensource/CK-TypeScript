using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace CK.Core;

/// <summary>
/// File system implementation of a <see cref="IResourceContainer"/>.
/// This is a simple wrapper around a <see cref="PhysicalFileProvider"/> that does the hard job.
/// </summary>
public sealed class FileSystemResourceContainer : IResourceContainer, IFileProvider
{
    readonly string _displayName;
    readonly string _root;
    readonly string _normalizedRoot;

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
        _root = Path.GetFullPath( root );
        _normalizedRoot = Normalize( _root );
    }

    static string Normalize( string path )
    {
        return Path.DirectorySeparatorChar != '/'
                ? path.Replace( '\\', '/' )
                : path;
    }

    /// <summary>
    /// Always true.
    /// </summary>
    public bool IsValid => true;

    /// <inheritdoc />
    public string DisplayName => _displayName;

    /// <inheritdoc />
    public IFileProvider GetFileProvider() => this;

    /// <inheritdoc />
    public ResourceLocator GetResourceLocator( IFileInfo fileInfo )
    {
        return fileInfo is PhysicalFileInfo f && f.PhysicalPath.StartsWith( _fileProvider.Root, StringComparison.Ordinal )
                ? new ResourceLocator( this, Normalize( f.PhysicalPath ) )
                : default;
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> AllResources
    {
        get
        {
            foreach( var f in Directory.EnumerateFiles( _fileProvider.Root, "*", SearchOption.AllDirectories ) )
            {
                yield return new ResourceLocator( this, Normalize( f ) );
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetAllResourceLocatorsFrom( IDirectoryContents directory )
    {
        if( directory is not PhysicalDirectoryContents d || d.PhysicalPath == null || !d.PhysicalPath.StartsWith( _fileProvider.Root ) )
        {
            throw new ArgumentException( $"The provided directory is not from this '{DisplayName}'." );
        }
        foreach( var f in Directory.EnumerateFiles( d.PhysicalPath ) )
        {
            yield return new ResourceLocator( this, Normalize( f ) );
        }
    }

    /// <summary>
    /// Gets the <see cref="StringComparer.Ordinal"/>.
    /// <para>
    /// This is a mess (see https://github.com/dotnet/runtime/issues/35128).
    /// </summary>
    public StringComparer ResourceNameComparer => StringComparer.Ordinal;

    /// <summary>
    /// Gets the <see cref="PhysicalFileProvider.Root"/> but with '/' instead of '\'.
    /// <para>
    /// This path ends with a '/'.
    /// </para>
    /// </summary>
    public string ResourcePrefix => _resourcePrefix;

    /// <inheritdoc />
    public Stream GetStream( ResourceLocator resource )
    {
        Throw.CheckArgument( resource.IsValid && resource.Container == this );
        return File.OpenRead( resource.ResourceName );
    }

    /// <inheritdoc />
    public bool TryGetResource( ReadOnlySpan<char> localResourceName, out ResourceLocator locator )
    {
        var name = String.Concat( _fileProvider.Root.AsSpan(), localResourceName );
        if( File.Exists( name ) )
        {
            locator = new ResourceLocator( this, Normalize( name ) );
            return true;
        }
        locator = default;
        return false;
    }

    /// <inheritdoc />
    public bool HasDirectory( ReadOnlySpan<char> localResourceName )
    {
        if( localResourceName.Length > 0 && (localResourceName[0] == '/' || localResourceName[0] == '\\') )
        {
            localResourceName = localResourceName.Slice( 0, localResourceName.Length - 1 );
        }
        var name = String.Concat( _fileProvider.Root.AsSpan(), localResourceName );
        return Directory.Exists( name );
    }

    /// <inheritdoc />
    public override string ToString() => _displayName;

    IFileInfo IFileProvider.GetFileInfo( string subpath )
    {
        throw new NotImplementedException();
    }

    IDirectoryContents IFileProvider.GetDirectoryContents( string subpath )
    {
        throw new NotImplementedException();
    }

    IChangeToken IFileProvider.Watch( string filter )
    {
        throw new NotImplementedException();
    }
}
