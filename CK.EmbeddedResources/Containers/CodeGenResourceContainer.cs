using CK.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CK.EmbeddedResources;

/// <summary>
/// A resource container in which a string, a reader or a writer function for a
/// resource can be registered.
/// <para>
/// Whether a string, a reader or a writer is registered for a resource is irrelevant: this container
/// adapts its behavior to always support <see cref="GetStream(in ResourceLocator)"/>,
/// <see cref="WriteStream(in ResourceLocator, Stream)"/> and <see cref="ReadAsText(in ResourceLocator)"/>.
/// </para>
/// <para>
/// The path separator is '/', '\' are normalized to '/'. This kind of container
/// doesn't support <see cref="ResourceLocator.LocalFilePath"/> (it is always null)
/// and <see cref="IResourceContainer.HasLocalFilePathSupport"/> is false by design.
/// </para>
/// </summary>
public sealed class CodeGenResourceContainer : IResourceContainer
{
    string[] _pathStore;
    object[] _streamStore;

    ReadOnlyMemory<string> _names;
    readonly string _displayName;

    /// <summary>
    /// Initializes a new empty dynamic container.
    /// </summary>
    /// <param name="displayName">The display name.</param>
    public CodeGenResourceContainer( string displayName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( displayName );
        _displayName = displayName;
        _pathStore = new string[4];
        _streamStore = new object[4];
    }

    /// <summary>
    /// Adds a new resource with a dynamic reader.
    /// <para>
    /// This throws if the resource is already associated to a reader or a writer.
    /// </para>
    /// </summary>
    /// <param name="resourcePath">The resource path. Must not be empty or whitespace nor contains '\'.</param>
    /// <param name="streamFactory">The function that must provide the <see cref="ResourceLocator.GetStream()"/>.</param>
    /// <returns>The resource locator.</returns>
    public ResourceLocator AddReader( string resourcePath, Func<Stream> streamFactory )
    {
        Throw.CheckNotNullArgument( streamFactory );
        return DoAdd( resourcePath, streamFactory );
    }

    /// <summary>
    /// Adds a new resource with a dynamic writer.
    /// <para>
    /// This throws if the resource is already associated to a reader or a writer.
    /// </para>
    /// </summary>
    /// <param name="resourcePath">
    /// The resource path. '\' are normalized ro '/'.
    /// Must not be empty or whitespace, contains '//' nor ends with a '/'.
    /// </param>
    /// <param name="streamWriter">The function that must write the stream content.</param>
    /// <returns>The resource locator.</returns>
    public ResourceLocator AddWriter( string resourcePath, Action<Stream> streamWriter )
    {
        Throw.CheckNotNullArgument( streamWriter );
        return DoAdd( resourcePath, streamWriter );
    }

    /// <summary>
    /// Adds a new resource with a dynamic writer.
    /// <para>
    /// This throws if the resource is already associated to a reader or a writer.
    /// </para>
    /// </summary>
    /// <param name="resourcePath">
    /// The resource path. '\' are normalized ro '/'.
    /// Must not be empty or whitespace, contains '//' nor ends with a '/'.
    /// </param>
    /// <param name="text">The text content.</param>
    /// <returns>The resource locator.</returns>
    public ResourceLocator AddText( string resourcePath, string text )
    {
        Throw.CheckNotNullArgument( text );
        return DoAdd( resourcePath, text );
    }

    ResourceLocator DoAdd( string resourcePath, object rws )
    {
        Throw.CheckArgument( resourcePath != null && !String.IsNullOrWhiteSpace( resourcePath ) );
        resourcePath = resourcePath.Replace( '\\', '/' );
        Throw.CheckArgument( !resourcePath.Contains( "//" ) && resourcePath[^1] != '/' );
        if( resourcePath[0] == '/' )
        {
            Throw.CheckArgument( resourcePath != "/" );
            resourcePath = resourcePath.Substring( 1 );
        }
        int idx = ImmutableOrdinalSortedStrings.IndexOf( resourcePath, _names.Span );
        if( idx >= 0 )
        {
            Throw.InvalidOperationException( $"Resource '{resourcePath}' already exists." );
        }
        idx = ~idx;
        if( _names.Length == _pathStore.Length )
        {
            Grow( idx );
        }
        else if( idx < _names.Length )
        {
            Array.Copy( _pathStore, idx, _pathStore, idx + 1, _names.Length - idx );
            Array.Copy( _streamStore, idx, _streamStore, idx + 1, _names.Length - idx );
        }
        _pathStore[idx] = resourcePath;
        _streamStore[idx] = rws;
        _names = _pathStore.AsMemory( 0, _names.Length + 1 );
        return new ResourceLocator( this, resourcePath );
    }

    void Grow( int indexToInsert )
    {
        int capacity = checked(_names.Length + 1);
        Throw.DebugAssert( _pathStore.Length == _streamStore.Length && _pathStore.Length < capacity );
        int newCapacity = _pathStore.Length == 0 ? 4 : 2 * _pathStore.Length;
        if( (uint)newCapacity > Array.MaxLength ) newCapacity = Array.MaxLength;
        Throw.DebugAssert( newCapacity >= capacity );

        string[] newPStore = new string[newCapacity];
        Func<Stream>[] newSStore = new Func<Stream>[newCapacity];
        if( indexToInsert != 0 )
        {
            Array.Copy( _pathStore, newPStore, length: indexToInsert );
            Array.Copy( _streamStore, newSStore, length: indexToInsert );
        }
        if( _names.Length != indexToInsert )
        {
            Array.Copy( _pathStore, indexToInsert, newPStore, indexToInsert + 1, _names.Length - indexToInsert );
            Array.Copy( _streamStore, indexToInsert, newSStore, indexToInsert + 1, _names.Length - indexToInsert );
        }
        _pathStore = newPStore;
        _streamStore = newSStore;
    }

    /// <inheritdoc />
    public bool IsValid => true;

    /// <inheritdoc />
    public string DisplayName => _displayName;

    /// <inheritdoc />
    public string ResourcePrefix => String.Empty;

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> AllResources => MemoryMarshal.ToEnumerable( _names ).Select( p => new ResourceLocator( this, p ) );

    /// <inheritdoc />
    public StringComparer NameComparer => StringComparer.Ordinal;

    bool IResourceContainer.HasLocalFilePathSupport => false;

    string? IResourceContainer.GetLocalFilePath( in ResourceLocator resource ) => null;

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetAllResources( ResourceFolder folder ) => DoGetAllResources( folder, this, _names );

    /// <inheritdoc />
    public ResourceFolder GetFolder( ReadOnlySpan<char> localFolderName ) => DoGetFolder( string.Empty, this, _names.Span, localFolderName );

    /// <inheritdoc />
    public ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> localFolderName )
    {
        folder.CheckContainer( this );
        return DoGetFolder( folder.FolderName, this, _names.Span, localFolderName );
    }

    /// <inheritdoc />
    public IEnumerable<ResourceFolder> GetFolders( ResourceFolder folder ) => DoGetFolders( folder, this, _names );

    /// <inheritdoc />
    public ResourceLocator GetResource( ReadOnlySpan<char> localResourceName ) => DoGetResource( string.Empty, this, _names.Span, localResourceName );

    /// <inheritdoc />
    public ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> localResourceName )
    {
        folder.CheckContainer( this );
        return DoGetResource( folder.FolderName, this, _names.Span, localResourceName );
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetResources( ResourceFolder folder ) => DoGetResources( folder, this, _names );

    /// <inheritdoc />
    public Stream GetStream( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        int idx = ImmutableOrdinalSortedStrings.IndexOf( resource.FullResourceName, _names.Span );
        Throw.DebugAssert( idx >= 0 );
        var s = _streamStore[idx];
        if( s is Func<Stream> reader ) return reader();
        if( s is string str )
        {
            return Util.RecyclableStreamManager.GetStream( Encoding.UTF8.GetBytes( str ) );
        }
        var writer = (Action<Stream>)s;
        var memory = Util.RecyclableStreamManager.GetStream();
        writer( memory );
        memory.Position = 0;
        return memory;
    }

    /// <inheritdoc />
    public void WriteStream( in ResourceLocator resource, Stream target )
    {
        resource.CheckContainer( this );
        int idx = ImmutableOrdinalSortedStrings.IndexOf( resource.FullResourceName, _names.Span );
        Throw.DebugAssert( idx >= 0 );
        var s = _streamStore[idx];
        if( s is Action<Stream> writer )
        {
            writer( target );
        }
        else if( s is string str )
        {
            var len = Encoding.UTF8.GetMaxByteCount( str.Length );
            var buffer = ArrayPool<byte>.Shared.Rent( len );
            len = Encoding.UTF8.GetBytes( str, buffer.AsSpan() );
            target.Write( buffer.AsSpan( 0, len ) );
            ArrayPool<byte>.Shared.Return( buffer );
        }
        else
        {
            using var source = ((Func<Stream>)s)();
            source.CopyTo( target );
        }
    }

    /// <inheritdoc />
    public string ReadAsText( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        int idx = ImmutableOrdinalSortedStrings.IndexOf( resource.FullResourceName, _names.Span );
        Throw.DebugAssert( idx >= 0 );
        var s = _streamStore[idx];
        if( s is string str ) return str;
        if( s is Action<Stream> writer )
        {
            using var memory = Util.RecyclableStreamManager.GetStream();
            writer( memory );
            return Encoding.UTF8.GetString( memory.GetReadOnlySequence() );
        }
        using( var source = ((Func<Stream>)s)() )
        using( var r = new StreamReader( source ) )
        {
            return r.ReadToEnd();
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

    internal static ResourceLocator DoGetResource( string fullPrefix, IResourceContainer container, ReadOnlySpan<string> names, ReadOnlySpan<char> localResourceName )
    {
        Throw.CheckArgument( localResourceName.Contains( '\\' ) is false );
        if( localResourceName.Length > 0 && localResourceName[0] == '/' )
        {
            localResourceName = localResourceName.Slice( 1 );
        }
        var name = String.Concat( fullPrefix.AsSpan(), localResourceName );
        int idx = ImmutableOrdinalSortedStrings.IndexOf( name, names );
        if( idx >= 0 )
        {
            return new ResourceLocator( container, name );
        }
        return default;
    }

    internal static ResourceFolder DoGetFolder( string prefix, IResourceContainer container, ReadOnlySpan<string> names, ReadOnlySpan<char> localFolderName )
    {
        Throw.CheckArgument( localFolderName.Contains( '\\' ) is false );
        if( localFolderName.Length > 0 && localFolderName[0] == '/' )
        {
            localFolderName = localFolderName.Slice( 1 );
        }
        var name = String.Concat( prefix.AsSpan(), localFolderName );
        if( name[^1] != '/' ) name += '/';
        if( ImmutableOrdinalSortedStrings.IsPrefix( name, names ) )
        {
            return new ResourceFolder( container, name );
        }
        return default;
    }

    internal static IEnumerable<ResourceLocator> DoGetAllResources( ResourceFolder folder, IResourceContainer container, ReadOnlyMemory<string> names )
    {
        folder.CheckContainer( container );
        var (idx, len) = ImmutableOrdinalSortedStrings.GetPrefixedRange( folder.FolderName, names.Span );
        if( len > 0 )
        {
            for( int i = 0; i < len; i++ )
            {
                yield return new ResourceLocator( container, names.Span[idx + i] );
            }
        }
    }

    internal static IEnumerable<ResourceLocator> DoGetResources( ResourceFolder folder, IResourceContainer container, ReadOnlyMemory<string> names )
    {
        folder.CheckContainer( container );
        var (idx, len) = ImmutableOrdinalSortedStrings.GetPrefixedRange( folder.FolderName, names.Span );
        if( len > 0 )
        {
            for( int i = 0; i < len; i++ )
            {
                var s = names.Span[idx + i];
                if( !s.AsSpan( folder.FolderName.Length ).Contains( '/' ) )
                {
                    yield return new ResourceLocator( container, s );
                }
            }
        }
    }

    internal static IEnumerable<ResourceFolder> DoGetFolders( ResourceFolder folder, IResourceContainer container, ReadOnlyMemory<string> names )
    {
        folder.CheckContainer( container );
        var (idx, len) = ImmutableOrdinalSortedStrings.GetPrefixedRange( folder.FolderName, names.Span );
        for( int i = 0; i < len; ++i )
        {
            var currentIdx = idx + i;
            var p = names.Span[currentIdx];
            int idxSub = p.AsSpan( folder.FolderName.Length ).IndexOf( '/' );
            if( idxSub != -1 )
            {
                p = p.Remove( folder.FolderName.Length + idxSub + 1 );
                var lenSub = ImmutableOrdinalSortedStrings.GetEndIndex( p, names.Span.Slice( currentIdx ) );
                Throw.DebugAssert( lenSub > 0 );
                Throw.DebugAssert( (currentIdx, lenSub) == ImmutableOrdinalSortedStrings.GetPrefixedRange( p, names.Span ) );
                yield return new ResourceFolder( container, p );
                i += (lenSub - 1);
            }
        }
    }
}
