using CSemVer;

namespace CK.Core
{
    static class TEMPExtensions
    {
        public static string ToNpmString( this SVersionBound b )
        {
            Throw.DebugAssert( "Time to remove this ToNpmString local helper.",
                               typeof( SVersionBound ).GetMethod( "ToNpmString" ) == null );

            if( b.Base.IsPrerelease && b.MinQuality != PackageQuality.Stable )
            {
                return $"^{b.Base}";
            }
            // If we are locked, use the "=".
            if( b.Lock == SVersionLock.Lock )
            {
                return $"={b.Base}";
            }
            if( b.Lock == SVersionLock.LockMajor )
            {
                if( b.Base.Patch == 0 )
                {
                    if( b.Base.Minor == 0 )
                    {
                        return $"^{b.Base.Major}";
                    }
                    return $"^{b.Base.Major}.{b.Base.Minor}";
                }
                return $"^{b.Base.Major}.{b.Base.Minor}.{b.Base.Patch}";
            }
            if( b.Lock == SVersionLock.LockMinor )
            {
                if( b.Base.Patch == 0 )
                {
                    if( b.Base.Minor == 0 )
                    {
                        return $"~{b.Base.Major}";
                    }
                    return $"~{b.Base.Major}.{b.Base.Minor}";
                }
                return $"~{b.Base.Major}.{b.Base.Minor}.{b.Base.Patch}";
            }
            return $">={b.Base}";
        }

    }
}
