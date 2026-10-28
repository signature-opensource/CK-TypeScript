using CK.Core;
using CSemVer;
using System;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Reads installed versions from the text of a Yarn Berry yarn.lock.
/// </summary>
internal static class YarnLockReader
{
    /// <summary>
    /// Gets the highest <c>version:</c> among the yarn.lock entries whose key holds a <c>&lt;packageName&gt;@npm:</c> descriptor.
    /// </summary>
    /// <param name="yarnLockText">The yarn.lock content.</param>
    /// <param name="packageName">The package name.</param>
    /// <returns>The installed version, or null when no entry describes the package.</returns>
    internal static SVersion? GetInstalledVersion( string yarnLockText, string packageName )
    {
        var prefix = packageName + "@npm:";
        SVersion? best = null;
        bool inEntry = false;
        foreach( var rawLine in yarnLockText.Split( '\n' ) )
        {
            var line = rawLine.TrimEnd( '\r' );
            if( line.Length == 0 || line[0] == '#' )
            {
                continue;
            }
            if( line[0] != ' ' )
            {
                inEntry = line.EndsWith( ':' ) && KeyHolds( line.AsSpan( 0, line.Length - 1 ), prefix );
            }
            else if( inEntry && line.StartsWith( "  version: ", StringComparison.Ordinal ) )
            {
                var v = SVersion.TryParse( line.Substring( 11 ).Trim().Trim( '"' ) );
                if( v.IsValid && (best == null || v > best) )
                {
                    best = v;
                }
            }
        }
        return best;

        static bool KeyHolds( ReadOnlySpan<char> key, string prefix )
        {
            foreach( var range in key.Split( ',' ) )
            {
                if( key[range].Trim().Trim( '"' ).StartsWith( prefix, StringComparison.Ordinal ) )
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Gets the version a migration starts from: the installed version read by <see cref="GetInstalledVersion(string, string)"/>,
    /// else <c>&lt;major&gt;.0.0</c> with a warning naming the package.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="yarnLockText">The yarn.lock content, null when there is no yarn.lock.</param>
    /// <param name="packageName">The package name.</param>
    /// <param name="major">The major to fall back to.</param>
    /// <returns>The version to give to <c>--from</c>.</returns>
    internal static SVersion GetFromVersion( IActivityMonitor monitor, string? yarnLockText, string packageName, int major )
    {
        var installed = yarnLockText != null ? GetInstalledVersion( yarnLockText, packageName ) : null;
        if( installed != null )
        {
            return installed;
        }
        var fallback = SVersion.Create( major, 0, 0 );
        var reason = yarnLockText == null ? "there is no yarn.lock" : $"yarn.lock has no entry for '{packageName}'";
        monitor.Warn( $"Unable to read the installed version of '{packageName}': {reason}. Its migrations start from '{fallback}', so migrations of '{packageName}' {major}.x that are already applied may run again." );
        return fallback;
    }
}
