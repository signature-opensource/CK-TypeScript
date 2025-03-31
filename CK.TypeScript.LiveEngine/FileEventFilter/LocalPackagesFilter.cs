using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

sealed class LocalPackagesFilter : IFileEventFilter
{
    readonly ImmutableArray<ResPackage> _packages;

    public LocalPackagesFilter( ImmutableArray<ResPackage> packages )
    {
        Throw.DebugAssert( packages.All( p => p.IsLocalPackage ) );
        _packages = packages;
    }

    public object? GetChange( string path )
    {
        foreach( var p in _packages )
        {
            Throw.DebugAssert( p.IsLocalPackage );


            // If the folder itself changes, emit an event with an empty sub path.
            int delta = path.Length - p.LocalPath.Length;
            if( delta == -1 )
            {
                if( path.AsSpan().Equals( p.LocalPath.AsSpan( 0, p.LocalPath.Length - 1 ), StringComparison.Ordinal ) )
                {
                    return new ChangedEvent( p.Resources, string.Empty );
                }
            }
            else if( delta >= 0 && path.StartsWith( p.LocalPath, StringComparison.Ordinal ) )
            {
                return new ChangedEvent( p.Resources, path.Substring( p.LocalPath.Length ) );
            }

        }
        return null;
    }
}
