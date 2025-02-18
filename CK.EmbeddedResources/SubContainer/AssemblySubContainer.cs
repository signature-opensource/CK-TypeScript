using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CK.Core;


sealed class AssemblySubContainer : IResourceContainer
{
    readonly ReadOnlyMemory<string> _names;
    readonly AssemblyResources _assemblyResources;
    readonly string _prefix;
    readonly string _displayName;

    internal AssemblySubContainer( AssemblyResources assemblyResources, string prefix, string displayName, ReadOnlyMemory<string> resourceNames )
    {
        Throw.DebugAssert( prefix.Length == 0
                           || (prefix.StartsWith( "ck@" )
                                && (prefix.Length == 3 || prefix.EndsWith( '/' ))) );
        _assemblyResources = assemblyResources;
        _prefix = prefix;
        _displayName = displayName;
        _names = resourceNames;
    }

    internal AssemblySubContainer( AssemblyResources assemblyResources, string prefix, string? displayName, Type type, ReadOnlyMemory<string> resourceNames )
        : this( assemblyResources, prefix, displayName ?? $"resources of '{type.ToCSharpName()}' type", resourceNames )
    {
    }

    public bool IsValid => _prefix.Length > 0;

    public string DisplayName => _displayName;

    public IEnumerable<ResourceLocator> AllResources => MemoryMarshal.ToEnumerable( _names ).Select( p => new ResourceLocator( this, p ) );

    public StringComparer NameComparer => StringComparer.Ordinal;

    public string ResourcePrefix => _prefix;

    public bool HasLocalFilePathSupport => !_assemblyResources.LocalPath.IsEmptyPath;

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

    public Stream GetStream( in ResourceLocator resource )
    {
        resource.CheckContainer( this );
        return _assemblyResources.OpenResourceStream( resource.ResourceName ); 
    }

    public void WriteStream( in ResourceLocator resource, Stream target )
    {
        resource.CheckContainer( this );
        using var source = _assemblyResources.OpenResourceStream( resource.ResourceName );
        source.CopyTo( target );
    }

    public ResourceLocator GetResource( ReadOnlySpan<char> localResourceName ) => DynamicResourceContainer.DoGetResource( _prefix, this, _names.Span, localResourceName );

    public ResourceLocator GetResource( ResourceFolder folder, ReadOnlySpan<char> localResourceName )
    {
        folder.CheckContainer( this );
        return DynamicResourceContainer.DoGetResource( folder.FolderName, this, _names.Span, localResourceName );
    }

    public ResourceFolder GetFolder( ReadOnlySpan<char> localFolderName ) => DynamicResourceContainer.DoGetFolder( _prefix, this, _names.Span, localFolderName );

    public ResourceFolder GetFolder( ResourceFolder folder, ReadOnlySpan<char> localFolderName )
    {
        folder.CheckContainer( this );
        return DynamicResourceContainer.DoGetFolder( folder.FolderName, this, _names.Span, localFolderName );
    }

    public IEnumerable<ResourceLocator> GetAllResources( ResourceFolder folder ) => DynamicResourceContainer.DoGetAllResources( folder, this, _names );

    public IEnumerable<ResourceLocator> GetResources( ResourceFolder folder ) => DynamicResourceContainer.DoGetResources( folder, this, _names );

    public IEnumerable<ResourceFolder> GetFolders( ResourceFolder folder ) => DynamicResourceContainer.DoGetFolders( folder, this, _names );

    public ReadOnlySpan<char> GetFolderName( ResourceFolder folder )
    {
        folder.CheckContainer( this );
        var s = folder.LocalFolderName.Span;
        return s.Length != 0 ? Path.GetFileName( s.Slice( 0, s.Length - 1 ) ) : s;
    }

    public ReadOnlySpan<char> GetResourceName( ResourceLocator resource )
    {
        resource.CheckContainer( this );
        return Path.GetFileName( resource.LocalResourceName.Span );
    }

    public override string ToString() => _displayName;
}
