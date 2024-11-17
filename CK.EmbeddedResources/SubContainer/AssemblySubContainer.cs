using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using static CK.Core.AssemblyResources;

namespace CK.Core;

sealed class AssemblySubContainer : IResourceContainer
{
    readonly ReadOnlyMemory<string> _resourceNames;
    readonly AssemblyResources _assemblyResources;
    readonly string _prefix;
    readonly string _displayName;
    IFileProvider? _fileProvider;

    internal AssemblySubContainer( AssemblyResources assemblyResources, string prefix, string displayName, ReadOnlyMemory<string> resourceNames )
    {
        _assemblyResources = assemblyResources;
        _prefix = prefix;
        _displayName = displayName;
        _resourceNames = resourceNames;
    }

    internal AssemblySubContainer( AssemblyResources assemblyResources, string prefix, string? displayName, Type type, ReadOnlyMemory<string> resourceNames )
    : this( assemblyResources, prefix, displayName ?? $"Assembly embedded resources of '{type.ToCSharpName()}'", resourceNames )
    {
    }

    public bool IsValid => _prefix.Length > 0;

    public string DisplayName => _displayName;

    public IFileProvider GetFileProvider() => _fileProvider ??= new FileProvider( _assemblyResources, _prefix, _resourceNames );

    public ResourceLocator GetResourceLocator( IFileInfo fileInfo )
    {
        return fileInfo is AssemblyResources.FileInfo f && f.FileProvider == _fileProvider
                ? new ResourceLocator( this, f.Path )
                : default;
    }

    public IEnumerable<ResourceLocator> AllResources
    {
        get
        {
            if( GetFileProvider() is FileProvider p )
            {
                for( int i = 0; i < p.ResourceNames.Length; ++i )
                {
                    yield return new ResourceLocator( this, p.ResourceNames.Span[i] );
                }
            }
        }
    }

    public StringComparer ResourceNameComparer => StringComparer.Ordinal;

    public string ResourcePrefix => _prefix;

    public Stream GetStream( ResourceLocator resource )
    {
        Throw.CheckArgument( resource.IsValid && resource.Container == this );
        return _assemblyResources.OpenResourceStream( resource.ResourceName ); 
    }

    public bool TryGetResource( ReadOnlySpan<char> localResourceName, out ResourceLocator locator )
    {
        var name = String.Concat( _prefix.AsSpan(), localResourceName );
        int idx = ImmutableOrdinalSortedStrings.IndexOf( name, _resourceNames.Span );
        if( idx >= 0 )
        {
            locator = new ResourceLocator( this, name );
            return true;
        }
        locator = default;
        return false;
    }

    /// <inheritdoc />
    public bool HasDirectory( ReadOnlySpan<char> localResourceName )
    {
        var name = String.Concat( _prefix.AsSpan(), localResourceName );
        if( name[name.Length - 1] != '/' ) name += '/';
        return ImmutableOrdinalSortedStrings.IsPrefix( name, _resourceNames.Span ); ;
    }

    public override string ToString() =>_displayName;
}
