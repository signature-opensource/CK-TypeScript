using CK.TypeScript.LiveEngine;
using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

sealed class LocalPackagesFilter : IFileEventFilter
{
    readonly ImmutableArray<LocalPackage> _packages;

    public LocalPackagesFilter( ImmutableArray<LocalPackage> packages )
    {
        _packages = packages;
    }

    public object? GetChange( string path )
    {
        foreach( var p in _packages )
        {
            int delta = path.Length - p.ResPath.Length;
            if( delta == -1 )
            {
                if( path.AsSpan().Equals( p.ResPath.AsSpan( 0, p.ResPath.Length - 1 ), StringComparison.Ordinal ) )
                {
                    return new ChangedEvent( p, string.Empty );
                }
            }
            else if( delta >= 0 && path.StartsWith( p.ResPath, StringComparison.Ordinal ) )
            {
                return new ChangedEvent( p, path.Substring( p.ResPath.Length ) );
            }
        }
        return null;
    }
}
