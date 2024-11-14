using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.TypeScript.Engine;

public sealed class PackageResources
{
    static readonly NormalizedPath _defaultTypeFolderSubPath = "Res";

    readonly Type _type;
    readonly NormalizedPath _resourceTypeFolder;
    readonly string _attributeName;
    readonly string _prefix;
    ImmutableArray<ResourceTypeLocator> _allRes;

    PackageResources( Type type, NormalizedPath resourceTypeFolder, string attributeName )
    {
        _type = type;
        _resourceTypeFolder = resourceTypeFolder;
        _attributeName = attributeName;
        _prefix = $"ck@{resourceTypeFolder}/";
    }

    /// <summary>
    /// Gets whether this <see cref="PackageResources"/> is valid.
    /// </summary>
    public bool IsValid => !_resourceTypeFolder.IsEmptyPath;

    /// <summary>
    /// Gets the type from which resources are located.
    /// </summary>
    public Type LocatorType => _type;

    /// <summary>
    /// Gets the prefix of all the resources from this set.
    /// </summary>
    public string ResourcePrefix => _prefix;

    /// <summary>
    /// Gets all the existing resources from the <see cref="TypeScriptPackageAttribute.ResourceFolderPath"/>.
    /// </summary>
    public ImmutableArray<ResourceTypeLocator> AllResources
    {
        get
        {
            if( _allRes.IsDefault )
            {
                if( IsValid )
                {
                    var resNames = _type.Assembly.GetSortedResourceNames2().GetPrefixedStrings( _prefix );
                    if( resNames.Length == 0 )
                    {
                        _allRes = ImmutableArray<ResourceTypeLocator>.Empty;
                    }
                    else
                    {
                        var b = ImmutableArray.CreateBuilder<ResourceTypeLocator>( resNames.Length );
                        foreach( var n in resNames.Span )
                        {
                            b.Add( new ResourceTypeLocator( _type, n ) );
                        }
                        _allRes = b.MoveToImmutable();
                    }
                }
                else
                {
                    _allRes = ImmutableArray<ResourceTypeLocator>.Empty;
                }
            }
            return _allRes;
        }
    }

    /// <summary>
    /// Computes the final resource name (prefixed by "ck@") to use for a <paramref name="resourcePath"/> in these resources.
    /// </summary>
    /// <param name="resourcePath">The relative resource path.</param>
    /// <returns>The final "ck@" prefixed resource name to use.</returns>
    public string GetCKResourceName( string resourcePath )
    {
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( resourcePath ) );
        // Fast path when rooted.
        // Don't check any //, http:/ or c:/ root: the resource will simply not be found.
        if( resourcePath[0] == '/' )
        {
            return string.Concat( "ck@".AsSpan(), resourcePath.AsSpan( 1 ) );
        }
        return "ck@" + _resourceTypeFolder.Combine( resourcePath ).ResolveDots();
    }

    readonly struct Finder : IComparable<ResourceTypeLocator>
    {
        readonly string _n;

        public Finder( string n ) => _n = n;

        public int CompareTo( ResourceTypeLocator other ) => StringComparer.Ordinal.Compare( _n, other.ResourceName );
    }

    /// <summary>
    /// Tries to get an existing resource.
    /// </summary>
    /// <param name="resourcePath">The resource path.</param>
    /// <param name="locator">The resulting locator.</param>
    /// <returns>True if the resource exists, false otherwise.</returns>
    public bool TryGetResource( string resourcePath, out ResourceTypeLocator locator )
    {
        var name = GetCKResourceName( resourcePath );
        ReadOnlySpan<ResourceTypeLocator> all = AllResources.AsSpan();
        int idx = all.BinarySearch( new Finder( name ) );
        if( idx >= 0 )
        {
            locator = all[idx];
            return true;
        }
        locator = default;
        return false;
    }

    /// <summary>
    /// Tries to get an existing resource and logs an error if it is not found.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourcePath">The resource path.</param>
    /// <param name="locator">The resulting locator.</param>
    /// <returns>True if the resource exists, false otherwise.</returns>
    public bool TryGetResource( IActivityMonitor monitor, string resourcePath, out ResourceTypeLocator locator )
    {
        if( !TryGetResource( resourcePath, out locator ) )
        {
            monitor.Error( $"Unable to find expected resource '{resourcePath}' for [{_attributeName}] on type '{_type:N}'." );
            return false;
        }
        return true;
    }

    /// <summary>
    /// Creates a <see cref="PackageResources"/> that may not be <see cref="IsValid"/> (an error has been logged).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The <see cref="LocatorType"/>.</param>
    /// <param name="resourceFolderPath">The resource folder relative to the <paramref name="type"/> and <paramref name="callerFilePath"/> to consider. Defaults to the "Res" folder.</param>
    /// <param name="callerFilePath">The caller file path. This is required: when null or empty, an error is logged and null is returned.</param>
    /// <param name="attributeName">The attribute name that declares the resource (used for logging).</param>
    /// <returns>The resources (<see cref="IsValid"/> may be false).</returns>
    public static PackageResources Create( IActivityMonitor monitor,
                                           Type type,
                                           string? resourceFolderPath,
                                           string? callerFilePath,
                                           string attributeName )
    {
        if( GetResourcePath( monitor, type, resourceFolderPath, out var resPath, attributeName ) )
        {
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
                    monitor.Error( $"Unable to resolve relative embedded resource paths: assembly name '{n}' folder of type '{type:N}' not found in '{p}'." );
                }
                else
                {
                    // TODO: miss a NormalizedPath.SubPath( int start, int len )...
                    p = p.RemoveFirstPart( idx + 1 ).With( NormalizedPathRootKind.None ).RemoveLastPart();
                    return new PackageResources( type, p.Combine( resPath ).ResolveDots(), attributeName );
                }
            }
        }
        return new PackageResources( type, default, attributeName );

        static bool GetResourcePath( IActivityMonitor monitor, Type declaredType, string? resourceFolderPath, out NormalizedPath resourcePath, string attributeName )
        {
            if( resourceFolderPath == null ) resourcePath = _defaultTypeFolderSubPath;
            else
            {
                resourcePath = resourceFolderPath;
                if( resourcePath.RootKind is NormalizedPathRootKind.RootedBySeparator )
                {
                    resourcePath = resourcePath.With( NormalizedPathRootKind.None );
                }
                else if( resourcePath.RootKind is NormalizedPathRootKind.RootedByDoubleSeparator
                                             or NormalizedPathRootKind.RootedByFirstPart
                                             or NormalizedPathRootKind.RootedByURIScheme )
                {
                    monitor.Error( $"[{attributeName}] on '{declaredType:N}' has invalid ResourceFolderPath: '{resourceFolderPath}'. Path must be rooted by '/' or be relative." );
                    return false;
                }
                else
                {
                    Throw.DebugAssert( "We are left with a regular relative path.", resourcePath.RootKind is NormalizedPathRootKind.None );
                }
            }
            return true;
        }

    }
}
