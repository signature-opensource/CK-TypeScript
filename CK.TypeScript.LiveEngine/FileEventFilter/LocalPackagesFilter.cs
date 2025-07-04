using CK.Core;
using System;
using System.Collections.Immutable;
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
        PathChangedEvent? e = null;
        foreach( var p in _packages )
        {
            e = p.Resources.LocalPath != null ? CreateEvent( path, p.Resources ) : null;
            if( e != null ) break;
            e = p.AfterResources.LocalPath != null ? CreateEvent( path, p.AfterResources ) : null;
            if( e != null ) break;
        }
        return e;

        static PathChangedEvent? CreateEvent( string path, IResPackageResources r )
        {
            Throw.DebugAssert( r.LocalPath != null );
            // If the folder itself changes, emit an event with an empty sub path.
            int delta = path.Length - r.LocalPath.Length;
            if( delta == -1 )
            {
                if( path.AsSpan().Equals( r.LocalPath.AsSpan( 0, r.LocalPath.Length - 1 ), StringComparison.Ordinal ) )
                {
                    return new PathChangedEvent( r, r.LocalPath );
                }
            }
            else if( delta >= 0 && path.StartsWith( r.LocalPath, StringComparison.Ordinal ) )
            {
                return new PathChangedEvent( r, path );
            }
            return null;
        }
    }
}
