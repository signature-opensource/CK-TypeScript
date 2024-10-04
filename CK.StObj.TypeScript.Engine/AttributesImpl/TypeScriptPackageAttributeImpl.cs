using CK.Core;
using CK.Setup;
using System;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine;

/// <summary>
/// Implementation class of the <see cref="TypeScriptPackageAttribute"/>.
/// <para>
/// This must be used as the base class of specialized TypeScriptPackageAttribute implementations.
/// </para>
/// </summary>
public class TypeScriptPackageAttributeImpl : IAttributeContextBoundInitializer, IStObjStructuralConfigurator
{
    readonly TypeScriptPackageAttribute _attr;
    readonly Type _type;
    readonly TypeScriptAspect _aspect;

    static readonly NormalizedPath _defaultTypeFolderSubPath = "Res";

    NormalizedPath _resourceTypeFolder;
    bool? _resourceTypeFolderComputed;

    public TypeScriptPackageAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type, TypeScriptAspect aspect )
    {
        _attr = attr;
        _type = type;
        _aspect = aspect;
        if( !typeof( TypeScriptPackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScriptPackage] can only decorate a TypeScriptPackage: '{type:N}' is not a TypeScriptPackage." );
        }
    }

    public virtual void Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        //var transformers = owner.GetTypeCustomAttributes<TypeScriptTransformerAttributeImpl>().ToList();
        //_aspect.RegisterTransfomers( _type, transformers );
    }

    public virtual void Configure( IActivityMonitor monitor, IStObjMutableItem o )
    {
        if( _attr.Package != null )
        {
            if( o.Container.Type == null ) o.Container.Type = _attr.Package;
            else if( o.Container.Type != _attr.Package )
            {
                monitor.Error( $"{o.ToString()}: [TypeScriptPackage] sets Package to be '{_attr.Package.Name}' but it is already '{o.Container.Type:N}'." );
            }
        }
    }

    /// <summary>
    /// Tries to resolve the path to the resources based on the type assembly and the <see cref="TypeScriptPackageAttribute.CallerFilePath"/>.
    /// This path can be empty (when the type is defined in the root folder of its project and the <see cref="TypeScriptPackageAttribute.ResourceFolderPath"/>
    /// is the empty string).
    /// <para>
    /// The resulting path doesn't contain the "ck@" prefix.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use. Errors will be logged on the first call only.</param>
    /// <param name="resourceTypePath">The resulting path on success.</param>
    /// <returns>True on success, false if the path cannot be computed.</returns>
    public bool TryGetResourceTypePath( IActivityMonitor monitor, out NormalizedPath resourceTypePath )
    {
        if( !_resourceTypeFolderComputed.HasValue )
        {
            NormalizedPath subPath = HandleResourceFolderPath( monitor );
            if( !_resourceTypeFolderComputed.HasValue )
            {
                ResolveResourceTypePath( monitor, subPath );
            }
            Throw.DebugAssert( _resourceTypeFolderComputed.HasValue );
        }
        resourceTypePath = _resourceTypeFolder;
        return _resourceTypeFolderComputed.Value;

        NormalizedPath HandleResourceFolderPath( IActivityMonitor monitor )
        {
            NormalizedPath subPath;
            if( _attr.ResourceFolderPath == null ) subPath = _defaultTypeFolderSubPath;
            else
            {
                subPath = _attr.ResourceFolderPath;
                if( subPath.RootKind is NormalizedPathRootKind.RootedBySeparator )
                {
                    _resourceTypeFolder = subPath.With( NormalizedPathRootKind.None );
                    _resourceTypeFolderComputed = true;
                }
                else if( subPath.RootKind is NormalizedPathRootKind.RootedByDoubleSeparator
                                             or NormalizedPathRootKind.RootedByFirstPart
                                             or NormalizedPathRootKind.RootedByURIScheme )
                {
                    monitor.Error( $"[TypeScriptPackage] on '{_type:N}' has invalid ResourceFolderPath: '{_attr.ResourceFolderPath}'. Path must be rooted by '/' or be relative." );
                    _resourceTypeFolderComputed = false;
                }
                else
                {
                    Throw.DebugAssert( "We are left with a regular relative path.", subPath.RootKind is NormalizedPathRootKind.None );
                }
            }

            return subPath;
        }

        void ResolveResourceTypePath( IActivityMonitor monitor, NormalizedPath subPath )
        {
            NormalizedPath p = _attr.CallerFilePath;
            if( p.IsEmptyPath )
            {
                monitor.Error( $"[TypeScriptPackage] on '{_type:N}' has no CallerFilePath. Unable to resolve relative embedded resource paths." );
                _resourceTypeFolderComputed = false;
            }
            else
            {
                var n = _type.Assembly.GetName().Name;
                Throw.DebugAssert( n != null );
                int idx;
                for( idx = p.Parts.Count - 2; idx >= 0; --idx )
                {
                    if( p.Parts[idx] == n ) break;
                }
                if( idx < 0 )
                {
                    monitor.Error( $"Unable to resolve relative embedded resource paths: assembly name '{n}' folder of type '{_type:N}' not found in '{p}'." );
                    _resourceTypeFolderComputed = false;
                }
                else
                {
                    // TODO: miss a NormalizedPath.SubPath( int start, int len )...
                    p = p.RemoveFirstPart( idx + 1 ).With( NormalizedPathRootKind.None ).RemoveLastPart();
                    _resourceTypeFolder = p.Combine( subPath ).ResolveDots();
                    _resourceTypeFolderComputed = true;
                }
            }
        }
    }

    /// <summary>
    /// Gets the final resource name (prefixed by "ck@") to use for a <paramref name="resourcePath"/> relative
    /// to this <see cref="TypeScriptPackageAttributeImpl"/>, considering its <see cref="TypeScriptPackageAttribute.CallerFilePath"/>
    /// and <see cref="TypeScriptPackageAttribute.ResourceFolderPath"/>.
    /// <para>
    /// This can fail: null is returned and errors are logged.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourcePath">The resource path.</param>
    /// <returns>The final "ck@" prefixed resource name to use or null on error.</returns>
    public string? GetCKResourceName( IActivityMonitor monitor, string resourcePath )
    {
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( resourcePath ) );
        // Fast path when rooted.
        // Don't check any //, http:/ or c:/ root: the resource will simply not be found.
        if( resourcePath[0] == '/' )
        {
            return string.Concat( "ck@".AsSpan(), resourcePath.AsSpan( 1 ) );
        }
        if( !TryGetResourceTypePath( monitor, out var resourceTypeFolder ) )
        {
            return null;
        }
        return "ck@" + resourceTypeFolder.Combine( resourcePath ).ResolveDots();
    }

}
