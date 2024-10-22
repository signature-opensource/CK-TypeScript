using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;

namespace CK.StObj.TypeScript.Engine;

/// <summary>
/// Implementation class of the <see cref="TypeScriptPackageAttribute"/>.
/// <para>
/// This must be used as the base class of specialized TypeScriptPackageAttribute implementations.
/// </para>
/// </summary>
public class TypeScriptPackageAttributeImpl : IAttributeContextBoundInitializer, IStObjStructuralConfigurator, ITSCodeGeneratorFactory, ITSCodeGenerator
{
    readonly TypeScriptPackageAttribute _attr;
    readonly Type _type;

    static readonly NormalizedPath _defaultTypeFolderSubPath = "Res";

    NormalizedPath _typeScriptFolder;
    NormalizedPath _resourceTypeFolder;
    ImmutableArray<ResourceTypeLocator> _allRes;

    /// <summary>
    /// Gets the folder of this package. Defaults to the namespace of the decorated type unless
    /// specified by <see cref="TypeScriptPackageAttribute.TypeScriptFolder"/>.
    /// </summary>
    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    /// <summary>
    /// Enables specializations to specify the <see cref="TypeScriptFolder"/>.
    /// </summary>
    /// <param name="folder"></param>
    protected void SetTypeScriptFolder( NormalizedPath folder ) => _typeScriptFolder = folder;

    /// <summary>
    /// Initializes a new <see cref="TypeScriptPackageAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptPackageAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type )
    {
        _attr = attr;
        _type = type;
        if( !typeof( TypeScriptPackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScriptPackage] can only decorate a TypeScriptPackage: '{type:N}' is not a TypeScriptPackage." );
        }
        Throw.DebugAssert( type.Namespace != null );
        if( string.IsNullOrWhiteSpace( attr.TypeScriptFolder ) )
        {
            _typeScriptFolder = type.Namespace.Replace( '.', '/' );
        }
        else
        {
            _typeScriptFolder = new NormalizedPath( attr.TypeScriptFolder );
            if( _typeScriptFolder.IsRooted )
            {
                monitor.Warn( $"[TypeScriptPackage] on '{type:C}': TypeScriptFolder is rooted, this is useless and removed." );
                _typeScriptFolder = _typeScriptFolder.With( NormalizedPathRootKind.None );
            }
        }
    }

    /// <summary>
    /// Implements <see cref="IAttributeContextBoundInitializer.Initialize(IActivityMonitor, ITypeAttributesCache, MemberInfo, Action{Type})"/>.
    /// Does nothing at this level.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="owner">The attribute cache of the decorated type.</param>
    /// <param name="m">The decorated type.</param>
    /// <param name="alsoRegister">Enables this method to register types.</param>
    public virtual void Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        if( GetResourcePath( monitor, _type, _attr.ResourceFolderPath, out var resPath ) )
        {
            NormalizedPath p = _attr.CallerFilePath;
            if( p.IsEmptyPath )
            {
                monitor.Error( $"[TypeScriptPackage] on '{_type:N}' has no CallerFilePath. Unable to resolve relative embedded resource paths." );
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
                }
                else
                {
                    // TODO: miss a NormalizedPath.SubPath( int start, int len )...
                    p = p.RemoveFirstPart( idx + 1 ).With( NormalizedPathRootKind.None ).RemoveLastPart();
                    _resourceTypeFolder = p.Combine( resPath ).ResolveDots();
                }
            }

        }

        static bool GetResourcePath( IActivityMonitor monitor, Type declaredType, string? resourceFolderPath, out NormalizedPath resourcePath )
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
                    monitor.Error( $"[TypeScriptPackage] on '{declaredType:N}' has invalid ResourceFolderPath: '{resourceFolderPath}'. Path must be rooted by '/' or be relative." );
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


    /// <summary>
    /// Implements <see cref="IStObjStructuralConfigurator.Configure(IActivityMonitor, IStObjMutableItem)"/>.
    /// Sets the <see cref="IStObjMutableItem.Container"/> to be the <see cref="TypeScriptPackage.TypeScriptPackage"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="o">The mutable item for this package.</param>
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
    /// Gets the final resource name (prefixed by "ck@") to use for a <paramref name="resourcePath"/> relative
    /// to this <see cref="TypeScriptPackageAttributeImpl"/>, considering its <see cref="TypeScriptPackageAttribute.CallerFilePath"/>
    /// and <see cref="TypeScriptPackageAttribute.ResourceFolderPath"/>.
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

    /// <summary>
    /// Gets all the associated resources from the <see cref="TypeScriptPackageAttribute.ResourceFolderPath"/>.
    /// </summary>
    /// <param name="monitor">The monitor (for the warning).</param>
    /// <param name="warnIfNone">True to warn if no resources were found.</param>
    /// <returns>The resources.</returns>
    public ImmutableArray<ResourceTypeLocator> GetAllResources( IActivityMonitor monitor, bool warnIfNone )
    {
        if( _allRes.IsDefault )
        {
            string prefix = $"ck@{_resourceTypeFolder}/";
            var resNames = _type.Assembly.GetSortedResourceNames2().GetPrefixedStrings( prefix );
            if( resNames.Length == 0 )
            {
                if( warnIfNone ) monitor.Warn( $"Unable to find at least one file for TypeScriptPackage '{_type:N}'." );
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
        return _allRes;
    }

    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        return _attr.ConsiderExplicitResourceOnly ? ITSCodeGenerator.Empty : this;
    }

    bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    bool ITSCodeGenerator.StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        foreach( ResourceTypeLocator o in GetAllResources( monitor, false ) )
        {
            // Skip prefix 'ck@{_resourceTypeFolder}/'.
            var targetFileName = _typeScriptFolder.Combine( o.ResourceName.Substring( _resourceTypeFolder.Path.Length + 4 ) );
            context.Root.Root.CreateResourceFile( o, targetFileName );
        }
        return true;
    }

}
