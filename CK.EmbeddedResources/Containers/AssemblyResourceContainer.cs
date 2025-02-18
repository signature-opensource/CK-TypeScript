using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CK.Core;

/// <summary>
/// Resource container for embedded resources.
/// This can only be created from a <see cref="AssemblyResources"/>.
/// </summary>
public sealed class AssemblyResourceContainer : IResourceContainer
{
    readonly ReadOnlyMemory<string> _names;
    readonly AssemblyResources _assemblyResources;
    readonly string _prefix;
    readonly string _displayName;

    internal AssemblyResourceContainer( AssemblyResources assemblyResources,
                                        string prefix,
                                        string displayName,
                                        ReadOnlyMemory<string> resourceNames )
    {
        Throw.DebugAssert( prefix.Length == 0
                           || (prefix.StartsWith( "ck@" )
                                && (prefix.Length == 3 || prefix.EndsWith( '/' ))) );
        _assemblyResources = assemblyResources;
        _prefix = prefix;
        _displayName = displayName;
        _names = resourceNames;
    }

    internal AssemblyResourceContainer( AssemblyResources assemblyResources,
                                        string prefix,
                                        string? displayName,
                                        Type type,
                                        ReadOnlyMemory<string> resourceNames )
        : this( assemblyResources, prefix, displayName ?? $"resources of '{type.ToCSharpName()}' type", resourceNames )
    {
    }

    /// <summary>
    /// Gets the assembly that contains this container.
    /// </summary>
    public AssemblyResources AssemblyResources => _assemblyResources;

    /// <inheritdoc />
    public bool IsValid => _prefix.Length > 0;

    /// <inheritdoc />
    public string DisplayName => _displayName;

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> AllResources => MemoryMarshal.ToEnumerable( _names ).Select( p => new ResourceLocator( this, p ) );

    /// <inheritdoc />
    public StringComparer NameComparer => StringComparer.Ordinal;

    /// <inheritdoc />
    public string ResourcePrefix => _prefix;

    /// <inheritdoc />
    public bool HasLocalFilePathSupport => !_assemblyResources.LocalPath.IsEmptyPath;

    /// <inheritdoc />
    public string? GetLocalFilePath( in ResourceLocator resource )
    {
        var p = _assemblyResources.LocalPath;
        if( p.IsEmptyPath ) return null;
        resource.CheckContainer( this );
        Throw.DebugAssert( resource.ResourceName.StartsWith( "ck@" ) );
        var s = string.Concat( p.Path.AsSpan(), "/", resource.ResourceName.AsSpan( 3 ) );
        if( Path.DirectorySeparatorChar != NormalizedPath.DirectorySeparatorChar )
        {
            s = s.Replace( NormalizedPath.DirectorySeparatorChar, Path.DirectorySeparatorChar );
        }
        return s;
    }

    /// <inheritdoc />
    public Stream GetStream( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        return _assemblyResources.OpenResourceStream( resource.ResourceName ); 
    }

    /// <inheritdoc />
    public void WriteStream( in ResourceLocator resource, Stream target )
    {
        resource.CheckContainer( this );
        using var source = _assemblyResources.OpenResourceStream( resource.ResourceName );
        source.CopyTo( target );
    }

    /// <inheritdoc />
    public ResourceLocator GetResource( ReadOnlySpan<char> localResourceName ) => DynamicResourceContainer.DoGetResource( _prefix, this, _names.Span, localResourceName );

    /// <inheritdoc />
    public ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> localResourceName )
    {
        folder.CheckContainer( this );
        return DynamicResourceContainer.DoGetResource( folder.FolderName, this, _names.Span, localResourceName );
    }

    /// <inheritdoc />
    public ResourceFolder GetFolder( ReadOnlySpan<char> localFolderName ) => DynamicResourceContainer.DoGetFolder( _prefix, this, _names.Span, localFolderName );

    /// <inheritdoc />
    public ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> localFolderName )
    {
        folder.CheckContainer( this );
        return DynamicResourceContainer.DoGetFolder( folder.FolderName, this, _names.Span, localFolderName );
    }

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetAllResources( ResourceFolder folder ) => DynamicResourceContainer.DoGetAllResources( folder, this, _names );

    /// <inheritdoc />
    public IEnumerable<ResourceLocator> GetResources( ResourceFolder folder ) => DynamicResourceContainer.DoGetResources( folder, this, _names );

    /// <inheritdoc />
    public IEnumerable<ResourceFolder> GetFolders( ResourceFolder folder ) => DynamicResourceContainer.DoGetFolders( folder, this, _names );

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
