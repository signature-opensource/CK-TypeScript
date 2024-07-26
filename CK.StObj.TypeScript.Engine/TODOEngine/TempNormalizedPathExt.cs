using CK.Core;
using System;
using System.Formats.Tar;
using System.Xml;

namespace CK.Core
{
    public static class TempNormalizedPathExt
    {

        public static bool TryGetRelativePathTo( this NormalizedPath source, NormalizedPath target, out NormalizedPath relative )
        {
            relative = default;
            var kind = source.RootKind;
            if( kind == NormalizedPathRootKind.None || kind != target.RootKind )
            {
                return false;
            }
            if( source == target ) return true;
            if( !source.HasParts )
            {
                // The kind cannot be RootedByURIScheme or NormalizedPathRootKind.RootedByFirstPart otherwise
                // at least one part would be there.
                Throw.DebugAssert( kind is not NormalizedPathRootKind.RootedByURIScheme and not NormalizedPathRootKind.RootedByFirstPart );
                // Source is a root, the relative is simply the relative target.
                Throw.DebugAssert( "Same root kind: they would have been equal.", target.HasParts );
                relative = target.With( NormalizedPathRootKind.None );
                return true;
            }
            if( !target.HasParts )
            {
                // The kind cannot be RootedByURIScheme or NormalizedPathRootKind.RootedByFirstPart otherwise
                // at least one part would be there.
                Throw.DebugAssert( kind is not NormalizedPathRootKind.RootedByURIScheme and not NormalizedPathRootKind.RootedByFirstPart );
                // Target is the root: it is a ../../.. relative path.
                Throw.DebugAssert( "Same root kind: they would have been equal.", source.HasParts );
                relative = CreateBackPath( source.Parts.Count );
                return true;
            }
            int maxCommon = Math.Min( source.Parts.Count, target.Parts.Count );
            int common = 0;
            do
            {
                if( source.Parts[common] != target.Parts[common] ) break;
                ++common;
            }
            while( common < maxCommon );

            int backCount = source.Parts.Count - common;
            relative = CreateBackPath( backCount );

            if( target.RootKind is NormalizedPathRootKind.RootedByFirstPart )
            {
                // When the first part is the root, it must be the same.
                if( common == 0 ) return false;
                target = target.RemoveFirstPart();
                --common;
            }
            else if( target.RootKind is NormalizedPathRootKind.RootedByURIScheme )
            {
                // For URI the 2 first parts must be the same.
                if( common < 2 ) return false;
                target = target.RemoveFirstPart( 2 );
                common -= 2;
            }
            else
            {
                // For any other kind of root, we forget the root kind.
                target = target.With( NormalizedPathRootKind.None );
            }
            relative = relative.Combine( target.RemoveFirstPart( common ) );
            return true;

            static NormalizedPath CreateBackPath( int backCount )
            {
                // TODO: Optimize inside NormalizedPath (array with same interned "..").
                var p = new string[backCount];
                for( int i = 0; i < backCount; i++ ) p[i] = "..";
                return new NormalizedPath( string.Join( NormalizedPath.DirectorySeparatorChar, p ) );
            }
        }
    }
}
