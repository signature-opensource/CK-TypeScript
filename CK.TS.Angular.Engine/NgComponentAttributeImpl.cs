using CK.Core;
using CK.Setup;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CK.EmbeddedResources;
using System.Linq;
using CK.Engine.TypeCollector;
using System.Collections.Generic;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgModuleAttribute"/>.
/// </summary>
public partial class NgComponentAttributeImpl : TypeScriptGroupOrPackageAttributeImpl
{
    readonly string _snakeName;

    /// <summary>
    /// Initializes a new <see cref="NgComponentAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgComponentAttributeImpl( IActivityMonitor monitor, NgComponentAttribute attr, Type type )
        : base( monitor, attr, type )
    {
        if( !typeof( INgComponent ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[NgComponent] can only decorate a NgComponent: '{type:N}' is not a NgComponent." );
        }
        _snakeName = CheckComponentName( monitor, type, "Component" );
        SetTypeScriptFolder( TypeScriptFolder.AppendPart( _snakeName ) );
    }

    internal static string CheckComponentName( IActivityMonitor monitor, Type type, string kind )
    {
        if( !type.IsSealed )
        {
            monitor.Error( $"Ng{kind} must be sealed (specialization is not supported): '{type:N}' must be sealed." );
        }
        var n = type.Name;
        if( n.Length <= kind.Length || !n.EndsWith( kind ) )
        {
            monitor.Error( $"'{type:N}' is a Ng{kind}, its type name must end with \"{kind}\"." );
        }
        else
        {
            n = n.Substring( 0, n.Length - kind.Length );
        }
        return ToSnakeCase().Replace( n, "$1-$2" ).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the component name that is the C# <see cref="TypeScriptGroupOrPackageAttributeImpl.DecoratedType"/> name (with the "Component" suffix).
    /// </summary>
    public string ComponentName => DecoratedType.Name;

    /// <summary>
    /// Gets the component name (snake-case) without "Component" suffix.
    /// </summary>
    public string FileComponentName => _snakeName;

    /// <summary>
    /// Gets whether this is the <see cref="AppComponent"/>.
    /// </summary>
    public bool IsAppComponent => DecoratedType == typeof( AppComponent );

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new NgComponentAttribute Attribute => Unsafe.As<NgComponentAttribute>( base.Attribute );

    /// <summary>
    /// Overridden to skip the <see cref="IsAppComponent"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="spaceBuilder">The resource space builder.</param>
    /// <returns>True on success, false on error.</returns>
    protected override bool CreateResPackageDescriptor( IActivityMonitor monitor, TypeScriptContext context, ResSpaceConfiguration spaceBuilder )
    {
        // Skip the AppComponent.
        // It has no resources, we don't create a ResPackage for it.
        if( IsAppComponent )
        {
            return true;
        }
        return base.CreateResPackageDescriptor( monitor, context, spaceBuilder );
    }

    /// <summary>
    /// Overridden to handle the component "*.component.ts" file name: it must exist
    /// and is created as a <see cref="ResourceTypeScriptFile"/> (but still published
    /// by the resource container).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="spaceBuilder">The resource space builder.</param>
    /// <param name="d">The package descriptor for this package.</param>
    /// <returns>True on success, false on error.</returns>
    protected override bool OnCreateResPackageDescriptor( IActivityMonitor monitor,
                                                          TypeScriptContext context,
                                                          ResSpaceConfiguration spaceBuilder,
                                                          ResPackageDescriptor d )
    {
        Throw.DebugAssert( !IsAppComponent );

        // Temporary
        var typeCache = context.CodeContext.CurrentRun.ConfigurationGroup.TypeCache;
        var decoratedType = typeCache.Get( DecoratedType );
        var ngPageComponent = typeCache.Get( typeof( INgPageComponent ) );

        // INgPageComponent is a kind of [CKAbstractType] that implies a kind of [IsSingle].
        //
        // Thoughts:
        // Is a [CKAbstractSingleType] a good idea to generalize this?
        // Is a [CKAbstractMultipleType] can replace (and generalize) [IsMultiple]?
        // Is a pure [CKAbstract] necessarily multiple? Or this doesn't exist?
        // Or is simply ignored?
        // For Single cardinality, a concrete type can be [IsSingle] but a multiple concrete
        // type is not obvious.
        //
        bool success = HandlePageComponent( monitor, d, decoratedType, ngPageComponent, out ICachedType? pageComponent );

        var fName = _snakeName + ".component.ts";
        if( !d.Resources.TryGetExpectedResource( monitor, fName, out var res ) )
        {
            return false;
        }
        var file = context.Root.Root.FindOrCreateResourceFile( res, TypeScriptFolder.AppendPart( fName ) );
        ITSDeclaredFileType tsType = Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( ComponentName );
        return success
               && base.OnCreateResPackageDescriptor( monitor, context, spaceBuilder, d )
               && context.GetAngularCodeGen().ComponentManager.RegisterComponent( monitor, this, tsType );

        static bool HandlePageComponent( IActivityMonitor monitor,
                                         ResPackageDescriptor d,
                                         ICachedType decoratedType,
                                         ICachedType ngPageComponent,
                                         out ICachedType? pageComponent )
        {
            bool success = true;
            pageComponent = null;
            if( decoratedType.Interfaces.Contains( ngPageComponent ) )
            {
                // If the type directly implements INgPageComponent, there's nothing to map.
                // 
                if( !decoratedType.DirectInterfaces.Contains( ngPageComponent ) )
                {
                    // What is the interface that is a INgPageComponent?
                    List<string>? ambiguities = null;
                    foreach( var i in decoratedType.Interfaces.Where( i => i.DirectInterfaces.Contains( ngPageComponent ) ) )
                    {
                        if( pageComponent == null )
                        {
                            pageComponent = i;
                        }
                        else
                        {
                            ambiguities ??= new List<string>() { pageComponent.CSharpName };
                            ambiguities.Add( i.CSharpName );
                        }
                    }
                    if( ambiguities != null )
                    {
                        monitor.Error( $"Ambiguous INgPageComponent for '{decoratedType.CSharpName}': interfaces '{ambiguities.Concatenate( "', '" )}' are all INgPageComponent. Only one can exist." );
                        success = false;
                    }
                    if( pageComponent != null && !d.AddSingleMapping( monitor, pageComponent ) )
                    {
                        success = false;
                    }
                }
            }
            return success;
        }
    }

    [GeneratedRegex( "([a-z])([A-Z])", RegexOptions.CultureInvariant )]
    private static partial Regex ToSnakeCase();
}
