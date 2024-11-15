using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace CK.Core;

public sealed partial class AssemblyResources : IFileProvider
{
    readonly Assembly _a;
    readonly ImmutableOrdinalSortedStrings _allResourceNames;
    readonly ReadOnlyMemory<string> _ckResourceNames;
    DateTimeOffset _lastModified;

    internal AssemblyResources( Assembly a )
    {
        _a = a;
        _allResourceNames = ImmutableOrdinalSortedStrings.UnsafeCreate( a.GetManifestResourceNames(), mustSort: true );
        _ckResourceNames = _allResourceNames.GetPrefixedStrings( "ck@" );
        _lastModified = Util.UtcMinValue;
    }

    /// <summary>
    /// Gets all the resource names.
    /// </summary>
    public ImmutableOrdinalSortedStrings AllResourceNames => _allResourceNames;

    /// <summary>
    /// Gets the sorted resource names that start with "ck@".
    /// </summary>
    public ReadOnlyMemory<string> CKResourceNames => _ckResourceNames;

    /// <summary>
    /// Tries to load the content of a "ck@" prefixed resource as a string, logging any error and returning null on error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourceName">The resource name with the "ck@" prefix.</param>
    /// <param name="logLevel">The log level when the resource cannot be found or loaded.</param>
    /// <returns>The string or null on error.</returns>
    public string? TryGetCKResourceString( IActivityMonitor monitor, string resourceName, LogLevel logLevel = LogLevel.Error )
    {
        return AssemblyExtensions.TryGetCKResourceString( _a, monitor, resourceName, logLevel, this );
    }

    /// <summary>
    /// Tries to load the content of a "ck@" prefixed resource, logging any error and returning null on error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourceName">The resource name with the "ck@" prefix.</param>
    /// <param name="logLevel">The log level when the resource cannot be found or loaded.</param>
    /// <returns>The Stream or null on error.</returns>
    public Stream? TryOpenCKResourceStream( IActivityMonitor monitor, string resourceName, LogLevel logLevel = LogLevel.Error )
    {
        return AssemblyExtensions.TryOpenCKResourceStream( _a, monitor, resourceName, logLevel, this );
    }

    /// <summary>
    /// Implements <see cref="IFileProvider.GetDirectoryContents(string)"/>.
    /// <para>
    /// If the path doesn't start with "ck@", the prefix is automatically added.
    /// Path separator is '/' (using a '\' will fail to find the content).
    /// It can be empty: all the <see cref="CKResourceNames"/> will be reachable.
    /// </para>
    /// </summary>
    /// <param name="subpath">The path of the directory. May start with "ck@" or not.</param>
    /// <returns>The directory content (<see cref="IDirectoryContents.Exists"/> may be false).</returns>
    public IDirectoryContents GetDirectoryContents( string subpath )
    {
        Throw.CheckNotNullArgument( subpath );
        Throw.CheckArgument( !subpath.Contains( '\\' ) );
        bool needPrefix = !subpath.StartsWith( "ck@", StringComparison.Ordinal );
        bool needSuffix = subpath.Length > (needPrefix ? 3 : 0) && subpath[^1] != '/';
        if( needPrefix )
        {
            subpath = needSuffix
                        ? "ck@" + subpath + '/'
                        : "ck@" + subpath;
        }
        else if( needSuffix )
        {
            subpath = subpath + '/';
        }
        int idx = ImmutableOrdinalSortedStrings.GetPrefixedStart( subpath, _ckResourceNames.Span );
        return idx >= 0
                ? new DirectoryContents( this, subpath )
                : NotFoundDirectoryContents.Singleton;
    }

    /// <summary>
    /// Implements <see cref="IFileProvider.GetFileInfo(string)"/>.
    /// <para>
    /// If the path doesn't start with "ck@", the prefix is automatically added.
    /// Path separator is '/' (using a '\' will fail to find the content).
    /// </para>
    /// </summary>
    /// <param name="subpath">The pth to find. Must not be empty.</param>
    /// <returns>The file info (<see cref="IFileInfo.Exists"/> may be false).</returns>
    public IFileInfo GetFileInfo( string subpath )
    {
        Throw.CheckNotNullOrEmptyArgument( subpath );
        Throw.CheckArgument( !subpath.Contains( '\\' ) );
        if( !subpath.StartsWith( "ck@", StringComparison.Ordinal ) )
        {
            subpath = "ck@" + subpath;
        }

        int idx = ImmutableOrdinalSortedStrings.IndexOf( subpath, _ckResourceNames.Span );
        return idx >= 0
                ? new FileInfo( this, subpath )
                : new NullFileInfo( subpath );
    }

    IChangeToken IFileProvider.Watch( string pattern ) => NullChangeToken.Singleton;

    DateTimeOffset GetLastModified()
    {
        if( _lastModified == Util.UtcMinValue )
        {
            var assemblyLocation = _a.Location;
            if( !string.IsNullOrEmpty( assemblyLocation ) )
            {
                try
                {
                    _lastModified = File.GetLastWriteTimeUtc( assemblyLocation );
                }
                catch( PathTooLongException )
                {
                }
                catch( UnauthorizedAccessException )
                {
                }
            }
        }
        return _lastModified;
    }

}
