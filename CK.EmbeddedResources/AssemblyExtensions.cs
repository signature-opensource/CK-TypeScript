using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace CK.Core;

public static class AssemblyExtensions
{
    static readonly ConcurrentDictionary<Assembly, ImmutableOrdinalSortedStrings> _cache = new();

    /// <summary>
    /// Gets all resource names contained in the assembly (calls <see cref="Assembly.GetManifestResourceNames"/>)
    /// as a sorted ascending (thanks to <see cref="StringComparer.Ordinal"/>) cached list of strings.
    /// </summary>
    /// <param name="assembly">Assembly </param>
    /// <returns>An ordered list of the resource names.</returns>
    static public ImmutableOrdinalSortedStrings GetSortedResourceNames2( this Assembly assembly )
    {
        // We don't care about duplicate computation and set. "Out of lock" Add in GetOrAdd is okay.
        return _cache.GetOrAdd( assembly, a =>
        {
            var l = a.GetManifestResourceNames();
            return ImmutableOrdinalSortedStrings.UnsafeCreate( l, mustSort: true );
        } );
    }

    /// <summary>
    /// Tries to load the content of a "ck@" prefixed resource as a string, logging any error and returning null on error.
    /// </summary>
    /// <param name="assembly">This assembly.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourceName">The resource name with the "ck@" prefix.</param>
    /// <returns>The string or null on error.</returns>
    public static string? TryGetCKResourceString( this Assembly assembly, IActivityMonitor monitor, string resourceName, LogLevel logLevel = LogLevel.Error )
    {
        using var s = TryOpenCKResourceStream( assembly, monitor, resourceName );
        try
        {
            return s == null
                    ? null
                    : new StreamReader( s, detectEncodingFromByteOrderMarks: true, leaveOpen: true ).ReadToEnd();
        }
        catch( Exception ex )
        {
            LogLoadError( assembly, monitor, resourceName, logLevel, ex );
            return null;
        }
    }

    /// <summary>
    /// Tries to load the content of a "ck@" prefixed resource, logging any error and returning null on error.
    /// </summary>
    /// <param name="assembly">This assembly.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourceName">The resource name with the "ck@" prefix.</param>
    /// <returns>The Stream or null on error.</returns>
    public static Stream? TryOpenCKResourceStream( this Assembly assembly, IActivityMonitor monitor, string resourceName, LogLevel logLevel = LogLevel.Error )
    {
        if( resourceName is null
            || resourceName.Length <= 3
            || !resourceName.StartsWith( "ck@" )
            || resourceName.Contains( '\\' ) )
        {
            Throw.ArgumentException( $"Invalid resource name '{resourceName}'. It must start with \"ck@\" and must not contain '\\'." );
        }
        try
        {
            var s = assembly.GetManifestResourceStream( resourceName );
            if( s == null )
            {
                var resName = resourceName.AsSpan().Slice( 3 );
                var fName = Path.GetFileName( resName );
                string? shouldBe = null;
                var resNames = assembly.GetSortedResourceNames2();
                foreach( string c in resNames.GetPrefixedStrings( "ck@" ).Span )
                {
                    if( c.AsSpan().EndsWith( fName, StringComparison.OrdinalIgnoreCase ) )
                    {
                        shouldBe = c;
                        if( c[c.Length - fName.Length - 1] == '/' )
                        {
                            break;
                        }
                    }
                }
                monitor.Log( logLevel, $"CK resource not found: '{resName}' in assembly '{assembly.GetName().Name}'.{(shouldBe == null ? string.Empty : $" It seems to be '{shouldBe.AsSpan().Slice( 3 )}'.")}" );
            }
            return s;
        }
        catch( Exception ex )
        {
            LogLoadError( assembly, monitor, resourceName, logLevel, ex );
            return null;
        }
    }

    static void LogLoadError( Assembly a, IActivityMonitor monitor, string resourceName, LogLevel logLevel, Exception ex )
    {
        monitor.Log( logLevel, $"While loading '{resourceName}' from '{a.GetName().Name}'.", ex );
    }
}
