using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Extends <see cref="Assembly"/>.
/// </summary>
public static class AssemblyExtensions
{
    static readonly ConcurrentDictionary<Assembly, AssemblyResources> _cache = new();
    static readonly object _lock = new();

    /// <summary>
    /// Gets all resource names contained in the assembly (calls <see cref="Assembly.GetManifestResourceNames"/>)
    /// as a sorted ascending (thanks to <see cref="StringComparer.Ordinal"/>) cached list of strings (see <see cref="ImmutableOrdinalSortedStrings"/>).
    /// </summary>
    /// <param name="assembly">Assembly </param>
    /// <returns>An ordered list of the resource names.</returns>
    static public AssemblyResources GetResources( this Assembly assembly )
    {
        return _cache.GetOrAdd( assembly, a =>
        {
            var assemblyName = assembly.GetName().Name;
            Throw.CheckArgument( "Cannot handle dynamic assembly.", assemblyName != null );
            // We DO care about duplicate computation.
            // "Out of lock" GetOrAdd must be protected.
            lock( _lock )
            {
                return new AssemblyResources( a, assemblyName );
            }
        } );
    }

    static AssemblyResources GetResources( Assembly assembly, string assemblyName )
    {
        return _cache.GetOrAdd( assembly, a =>
        {
            // We DO care about duplicate computation.
            // "Out of lock" GetOrAdd must be protected.
            lock( _lock )
            {
                return new AssemblyResources( a, assemblyName );
            }
        } );
    }

    /// <summary>
    /// Creates a <see cref="AssemblyResourceContainer"/> or <see cref="FileSystemResourceContainer"/> if this assembly
    /// is a local one for a type that must be decorated with at least one <see cref="IEmbeddedResourceTypeAttribute"/>
    /// attribute.
    /// <para>
    /// On success, the container is bound to the corresponding embedded ressources "Res/" folder.
    /// It may not be <see cref="IResourceContainer.IsValid"/> (an error has been logged).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The declaring type. Its <see cref="Type.Assembly"/> MUST be this <see cref="Assembly"/>.</param>
    /// <param name="containerDisplayName">When null, the <see cref="IResourceContainer.DisplayName"/> defaults to "resources of '...' type".</param>
    /// <returns>The resources (<see cref="IResourceContainer.IsValid"/> may be false).</returns>
    public static IResourceContainer CreateResourcesContainerForType( this Assembly assembly,
                                                                      IActivityMonitor monitor,
                                                                      Type type,
                                                                      string? containerDisplayName = null )
    {
        return AssemblyResources.GetCallerInfo( monitor,
                                                type,
                                                type.GetCustomAttributes().OfType<IEmbeddedResourceTypeAttribute>(),
                                                out var callerPath,
                                                out var callerSource )
                ? CreateResourcesContainerForType( assembly, monitor, callerPath, type, callerSource.GetType().Name, containerDisplayName )
                : new EmptyResourceContainer( AssemblyResourceContainer.MakeDisplayName( containerDisplayName, type ),
                                              resourcePrefix: "",
                                              isValid: false );

    }

    /// <summary>
    /// Creates a <see cref="AssemblyResourceContainer"/> or <see cref="FileSystemResourceContainer"/> if this assembly
    /// is a local one for a type based on a CallerFilePath (<see cref="CallerFilePathAttribute"/>)
    /// captured by an attribute on the <paramref name="type"/> (typically a <see cref="IEmbeddedResourceTypeAttribute"/>).
    /// <para>
    /// On success, the container is bound to the corresponding embedded ressources "Res/" folder or to the file system
    /// "Res\" folder.
    /// It may not be <see cref="IResourceContainer.IsValid"/> (an error has been logged).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="callerFilePath">The caller file path. This is required: when null or empty, an error is logged and an invalid container is returned.</param>
    /// <param name="type">The declaring type. Its <see cref="Type.Assembly"/> MUST be this <see cref="Assembly"/>. (This is used for logging).</param>
    /// <param name="attributeName">The attribute name that declares the resource (used for logging).</param>
    /// <param name="containerDisplayName">When null, the <see cref="IResourceContainer.DisplayName"/> defaults to "resources of '...' type".</param>
    /// <returns>The resources (<see cref="IResourceContainer.IsValid"/> may be false).</returns>
    static public IResourceContainer CreateResourcesContainerForType( this Assembly assembly,
                                                                      IActivityMonitor monitor,
                                                                      string? callerFilePath,
                                                                      Type type,
                                                                      string attributeName,
                                                                      string? containerDisplayName = null )
    {
        var assemblyName = assembly.GetName().Name;
        Throw.CheckArgument( "Cannot handle dynamic assembly.", assemblyName != null );
        if( LocalDevSolution.LocalProjectPaths.TryGetValue( assemblyName, out var projectPath ) )
        {
            containerDisplayName = AssemblyResourceContainer.MakeDisplayName( containerDisplayName, type );
            if( AssemblyResources.ValidateContainerForType( monitor,
                                                            assembly,
                                                            assemblyName,
                                                            callerFilePath,
                                                            type,
                                                            attributeName,
                                                            out var subPath ) )
            {
                return new FileSystemResourceContainer( projectPath.Combine( subPath ).AppendPart( "Res" ), containerDisplayName );
            }
            return new EmptyResourceContainer( containerDisplayName, resourcePrefix: projectPath, isValid: false );
        }
        AssemblyResources aR = _cache.GetOrAdd( assembly, a =>
        {
            lock( _lock )
            {
                return new AssemblyResources( a, assemblyName );
            }
        } );
        return aR.CreateResourcesContainerForType( monitor,
                                                   callerFilePath,
                                                   type,
                                                   attributeName,
                                                   containerDisplayName );
    }

    /// <summary>
    /// Gets a resource content.
    /// <para>
    /// If a stream cannot be obtained, a detailed <see cref="IOException"/> is raised.
    /// </para>
    /// </summary>
    /// <param name="assembly">This assembly.</param>
    /// <param name="resourceName">The reource name.</param>
    /// <returns>The resource's content stream.</returns>
    public static Stream OpenResourceStream( this Assembly assembly, string resourceName )
    {
        var s = assembly.GetManifestResourceStream( resourceName );
        return s ?? ThrowDetailedError( assembly, resourceName );
    }

    [StackTraceHidden]
    static Stream ThrowDetailedError( Assembly assembly, string resourceName )
    {
        var b = new StringBuilder();
        b.AppendLine( $"Resource '{resourceName}' cannot be loaded from '{assembly.GetName().Name}'." );
        AssemblyExtensions.AppendDetailedError( b, assembly, resourceName );
        throw new IOException( b.ToString() );
    }

    /// <summary>
    /// Tries to load the content of a "ck@" prefixed resource as a string, logging any error and returning null on error.
    /// </summary>
    /// <param name="assembly">This assembly.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourceName">The resource name with the "ck@" prefix.</param>
    /// <param name="logLevel">The log level when the resource cannot be found or loaded.</param>
    /// <returns>The string or null on error.</returns>
    public static string? TryGetCKResourceString( this Assembly assembly,
                                                  IActivityMonitor monitor,
                                                  string resourceName,
                                                  LogLevel logLevel = LogLevel.Error )
    {
        return TryGetCKResourceString( assembly, monitor, resourceName, logLevel, null );
    }

    internal static string? TryGetCKResourceString( Assembly assembly,
                                                    IActivityMonitor monitor,
                                                    string resourceName,
                                                    LogLevel logLevel,
                                                    AssemblyResources? assemblyResources )
    {
        using var s = TryOpenCKResourceStream( assembly, monitor, resourceName, logLevel, assemblyResources );
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
    /// <param name="logLevel">The log level when the resource cannot be found or loaded.</param>
    /// <returns>The Stream or null on error.</returns>
    public static Stream? TryOpenCKResourceStream( this Assembly assembly,
                                                   IActivityMonitor monitor,
                                                   string resourceName,
                                                   LogLevel logLevel = LogLevel.Error )
    {
        return TryOpenCKResourceStream( assembly, monitor, resourceName, logLevel, null );
    }

    internal static Stream? TryOpenCKResourceStream( Assembly assembly,
                                                     IActivityMonitor monitor,
                                                     string resourceName,
                                                     LogLevel logLevel,
                                                     AssemblyResources? assemblyResources )
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
                assemblyResources ??= assembly.GetResources();
                foreach( string c in assemblyResources.CKResourceNames.Span )
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

    internal static void AppendDetailedError( StringBuilder b, Assembly assembly, string resourceName )
    {
        var info = assembly.GetManifestResourceInfo( resourceName );
        if( info == null )
        {
            b.AppendLine( "No information for this resource. The ResourceName may not exist at all. Resource names are:" );
            foreach( var n in assembly.GetManifestResourceNames() )
            {
                b.AppendLine( n );
            }
        }
        else
        {
            b.AppendLine( "ManifestResourceInfo:" )
             .Append( "ReferencedAssembly = " ).Append( info.ReferencedAssembly ).AppendLine()
             .Append( "FileName = " ).Append( info.FileName ).AppendLine()
             .Append( "ResourceLocation = " ).Append( info.ResourceLocation ).AppendLine();
        }
    }
}
