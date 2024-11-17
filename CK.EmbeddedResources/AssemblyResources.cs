using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CK.Core;

public sealed partial class AssemblyResources
{
    readonly Assembly _assembly;
    readonly ImmutableOrdinalSortedStrings _allResourceNames;
    readonly ReadOnlyMemory<string> _ckResourceNames;
    DateTimeOffset _lastModified;

    internal AssemblyResources( Assembly a )
    {
        _assembly = a;
        _allResourceNames = ImmutableOrdinalSortedStrings.UnsafeCreate( a.GetManifestResourceNames(), mustSort: true );
        _ckResourceNames = _allResourceNames.GetPrefixedStrings( "ck@" );
        _lastModified = Util.UtcMinValue;
    }

    /// <summary>
    /// Gets the assembly.
    /// </summary>
    public Assembly Assembly => _assembly;

    /// <summary>
    /// Gets all the resource names.
    /// </summary>
    public ImmutableOrdinalSortedStrings AllResourceNames => _allResourceNames;

    /// <summary>
    /// Gets the sorted resource names that start with "ck@".
    /// </summary>
    public ReadOnlyMemory<string> CKResourceNames => _ckResourceNames;

    public bool IsValid => throw new NotImplementedException();

    public string DisplayName => throw new NotImplementedException();

    /// <summary>
    /// Gets a resource content.
    /// <para>
    /// If a stream cannot be obtained, a detailed <see cref="IOException"/> is raised.
    /// </para>
    /// </summary>
    /// <param name="resourceName">The reource name.</param>
    /// <returns>The resource's content stream.</returns>
    public Stream OpenResourceStream( string resourceName ) => _assembly.OpenResourceStream( resourceName );

    /// <summary>
    /// Tries to load the content of a "ck@" prefixed resource as a string, logging any error and returning null on error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourceName">The resource name with the "ck@" prefix.</param>
    /// <param name="logLevel">The log level when the resource cannot be found or loaded.</param>
    /// <returns>The string or null on error.</returns>
    public string? TryGetCKResourceString( IActivityMonitor monitor, string resourceName, LogLevel logLevel = LogLevel.Error )
    {
        return AssemblyExtensions.TryGetCKResourceString( _assembly, monitor, resourceName, logLevel, this );
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
        return AssemblyExtensions.TryOpenCKResourceStream( _assembly, monitor, resourceName, logLevel, this );
    }

    /// <summary>
    /// Creates a <see cref="IFileProvider"/>.
    /// This returns a provider of all the "ck@" prefixed resources when <paramref name="subpath"/> is null or empty.
    /// <para>
    /// If the path doesn't start with "ck@", the prefix is automatically added.
    /// Path separator is '/' (using a '\' will fail to find the content).
    /// It can be empty: all the <see cref="CKResourceNames"/> will be reachable.
    /// </para>
    /// </summary>
    /// <param name="subpath">
    /// The path of the directory. May start with "ck@" or not.
    /// Should almost always end with "Res/" as it's the standard directory names that automatically
    /// embeds the "ck@" resources. 
    /// </param>
    /// <returns>A file provider. May be <see cref="NullFileProvider"/>.</returns>
    public IFileProvider CreateFileProvider( string? subpath = null )
    {
        if( string.IsNullOrEmpty( subpath ) )
        {
            return _ckResourceNames.Length == 0
                        ? EmptyResourceContainer.Default.GetFileProvider()
                        : new FileProvider( this, "ck@", _ckResourceNames );
        }
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
        var (idx, len) = ImmutableOrdinalSortedStrings.GetPrefixedRange( subpath, _ckResourceNames.Span );
        return len == 0
                ? EmptyResourceContainer.Default.GetFileProvider()
                : new FileProvider( this, subpath, _ckResourceNames.Slice( idx, len ) );
    }

    DateTimeOffset GetLastModified()
    {
        if( _lastModified == Util.UtcMinValue )
        {
            var assemblyLocation = _assembly.Location;
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

    /// <summary>
    /// Creates a <see cref="IResourceContainer"/> for a type that must be decorated with at least
    /// one <see cref="IEmbeddedResourceTypeAttribute"/> attribute.
    /// <para>
    /// On success, the container is bound to the corresponding embedded ressources "Res/" folder.
    /// It may not be <see cref="IsValid"/> (an error has been logged).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The declaring type. Its <see cref="Type.Assembly"/> MUST be this <see cref="Assembly"/>.</param>
    /// <param name="containerDisplayName">When null, the <see cref="DisplayName"/> defaults to "Assemby embedded resources of 'type'".</param>
    public IResourceContainer CreateResourcesContainerForType( IActivityMonitor monitor, Type type, string? containerDisplayName = null )
    {
        return GetCallerInfo( monitor, type, type.GetCustomAttributes().OfType<IEmbeddedResourceTypeAttribute>(), out var callerPath, out var callerSource )
                ? CreateResourcesContainerForType( monitor, callerPath, type, callerSource.GetType().Name, containerDisplayName )
                : new AssemblySubContainer( this, "", containerDisplayName, type, ReadOnlyMemory<string>.Empty );

        static bool GetCallerInfo( IActivityMonitor monitor,
                                   Type type,
                                   IEnumerable<IEmbeddedResourceTypeAttribute> attributes,
                                   [NotNullWhen( true )] out string? callerPath,
                                   [NotNullWhen( true )] out IEmbeddedResourceTypeAttribute? callerSource )
        {
            var eA = attributes.GetEnumerator();
            callerPath = null;
            callerSource = null;
            if( !eA.MoveNext() )
            {
                monitor.Error( $"Type '{type:N}' has no IEmbeddedResourceTypeAttribute. Unable to resolve a resource container." );
                return false;
            }
            callerSource = eA.Current;
            callerPath = callerSource.CallerFilePath ?? "";
            while( eA.MoveNext() )
            {
                var otherSource = eA.Current;
                var otherPath = otherSource.CallerFilePath ?? "";
                if( callerPath.Length == 0 )
                {
                    callerPath = otherPath;
                    callerSource = otherSource;
                }
                else if( otherPath.Length != 0 && callerPath != otherPath )
                {
                    monitor.Error( $"""
                                    Type '{type:N}' has more than one IEmbeddedResourceTypeAttribute with different CallerFilePath.
                                    Attribute '{callerSource.GetType().Name}' provides '{callerPath}' and '{otherSource.GetType().Name}' provides '{otherPath}'.
                                    """ );
                    return false;
                }
            }
            if( callerPath.Length == 0 )
            {
                monitor.Error( $"Type '{type:N}' has IEmbeddedResourceTypeAttribute but non provide a non null or empty CallerFilePath. Unable to resolve a resource container." );
                return false;
            }
            return true;
        }

    }

    /// <summary>
    /// Creates a <see cref="IResourceContainer"/> for a type based on a CallerFilePath (<see cref="CallerFilePathAttribute"/>) captured
    /// by an attribute on the <paramref name="type"/> (typically a <see cref="IEmbeddedResourceTypeAttribute"/>).
    /// <para>
    /// On success, the container is bound to the corresponding embedded ressources "Res/" folder.
    /// It may not be <see cref="IsValid"/> (an error has been logged).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="callerFilePath">The caller file path. This is required: when null or empty, an error is logged and an invalid container is returned.</param>
    /// <param name="type">The declaring type. Its <see cref="Type.Assembly"/> MUST be this <see cref="Assembly"/>. (This is used for logging).</param>
    /// <param name="attributeName">The attribute name that declares the resource (used for logging).</param>
    /// <param name="containerDisplayName">When null, the <see cref="DisplayName"/> defaults to "Assemby embedded resources of 'type'".</param>
    /// <returns>The resources (<see cref="IsValid"/> may be false).</returns>
    public IResourceContainer CreateResourcesContainerForType( IActivityMonitor monitor,
                                                               string? callerFilePath,
                                                               Type type,
                                                               string attributeName,
                                                               string? containerDisplayName = null )
    {
        Throw.CheckNotNullArgument( type );
        Throw.CheckNotNullArgument( attributeName );
        Throw.CheckArgument( type.Assembly == Assembly );
        NormalizedPath p = callerFilePath;
        if( p.IsEmptyPath )
        {
            monitor.Error( $"[{attributeName}] on '{type:N}' has no CallerFilePath. Unable to resolve relative embedded resource paths." );
        }
        else
        {
            var n = type.Assembly.GetName().Name;
            Throw.DebugAssert( n != null );
            int idx;
            for( idx = p.Parts.Count - 2; idx >= 0; --idx )
            {
                if( p.Parts[idx] == n ) break;
            }
            if( idx < 0 )
            {
                monitor.Error( $"Unable to resolve relative embedded resource paths: the assembly name '{n}' parent folder of type '{type:N}' not found in caller file path '{p}'." );
            }
            else
            {
                // TODO: miss a NormalizedPath.SubPath( int start, int len )...
                p = p.RemoveFirstPart( idx + 1 ).With( NormalizedPathRootKind.None ).RemoveLastPart();
                var prefix = $"ck@{p.Path}/Res/";
                return new AssemblySubContainer( this, prefix, containerDisplayName, type, ImmutableOrdinalSortedStrings.GetPrefixedStrings( prefix, _ckResourceNames ) );
            }
        }
        return new AssemblySubContainer( this, "", containerDisplayName, type, ReadOnlyMemory<string>.Empty );
    }
}
