using CK.Core;
using CK.Setup;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Runtime.CompilerServices;
using CK.EmbeddedResources;
using System.Linq;
using CK.Engine.TypeCollector;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgModuleAttribute"/>.
/// </summary>
public partial class NgComponentAttributeImpl : TypeScriptGroupOrPackageAttributeImpl
{
    readonly string _kebabName;
    readonly string _typeScriptName;

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
        (_kebabName, _typeScriptName) = CheckComponentName( monitor, type, "Component" );
        SetTypeScriptFolder( TypeScriptFolder.AppendPart( _kebabName ) );
    }

    internal static (string Kebab, string TypeScript) CheckComponentName( IActivityMonitor monitor, Type type, string kind )
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
        return (System.Text.Json.JsonNamingPolicy.KebabCaseLower.ConvertName( n ), n);
    }

    /// <summary>
    /// Gets the component name (kebab-case) without "component" suffix.
    /// </summary>
    public string FileComponentName => _kebabName;

    /// <summary>
    /// Gets whether this is the <see cref="AppComponent"/>.
    /// </summary>
    public bool IsAppComponent => DecoratedType == typeof( AppComponent );

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new NgComponentAttribute Attribute => Unsafe.As<NgComponentAttribute>( base.Attribute );

    /// <summary>
    /// Gets the typescript type name (without "Component" suffix).
    /// </summary>
    public string TypeScriptName => _typeScriptName;

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
    /// Overridden to handle discovery and mapping of a possible <see cref="NgSingleAbstractComponentAttribute"/>
    /// interface.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="spaceConfiguration">The space configuration.</param>
    /// <param name="package">The package descriptor for this package.</param>
    /// <returns>True on success, false on error.</returns>
    protected override bool OnCreateResPackageDescriptor( IActivityMonitor monitor,
                                                          TypeScriptContext context,
                                                          ResSpaceConfiguration spaceConfiguration,
                                                          ResPackageDescriptor package )
    {
        Throw.DebugAssert( !IsAppComponent );
        // Temporary
        Throw.DebugAssert( package.Type != null && package.Type.Type == DecoratedType );

        if( !FindSingleAbstract( monitor, package, out var singleInterface, out var expectedComponentName ) )
        {
            return false;
        }
        if( singleInterface != null )
        {
            Throw.DebugAssert( !string.IsNullOrWhiteSpace( expectedComponentName ) );
            if( expectedComponentName != _kebabName )
            {
                monitor.Error( $"""
                    Invalid single Angular abstract component. '{package.Type}' is a '{singleInterface}'.
                    Its component name must be '{_kebabName}', not '{expectedComponentName}'.
                    """ );
                return false;
            }
            if( !package.AddSingleMapping( monitor, singleInterface ) )
            {
                return false;
            }
        }
        return base.OnCreateResPackageDescriptor( monitor, context, spaceConfiguration, package );

        static bool FindSingleAbstract( IActivityMonitor monitor,
                                        ResPackageDescriptor package,
                                        out ICachedType? singleInterface,
                                        out string? expectedComponentName )
        {
            Throw.DebugAssert( package.Type != null );

            expectedComponentName = null;
            singleInterface = null;
            var attrType = typeof( NgSingleAbstractComponentAttribute );
            foreach( var i in package.Type.Interfaces )
            {
                var attr = i.AttributesData.FirstOrDefault( a => a.AttributeType == attrType );
                if( attr != null ) 
                {
                    if( singleInterface != null )
                    {
                        var multiple = package.Type.Interfaces
                                                   .Where( i => i.AttributesData.Any( a => a.AttributeType == attrType ) )
                                                   .Select( c => c.ToString() )
                                                   .Concatenate( "', '" );
                        monitor.Error( $"Type '{package.Type}' implements multiple single Angular abstract component: '{multiple}'." );
                        return false;
                    }
                    singleInterface = i;
                    expectedComponentName = (string?)attr.ConstructorArguments[0].Value;
                    if( string.IsNullOrWhiteSpace( expectedComponentName ) )
                    {
                        monitor.Error( $"Invalid empty component name in [NgSingleAbstractComponent()] on '{i}'." );
                        return false;
                    }
                }
            }
            return true;
        }
    }


    /// <summary>
    /// Overridden to handle the component "*.ts" file name: it must exist
    /// and is created as a <see cref="ResourceTypeScriptFile"/> (but still published
    /// by the resource container).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="spaceData">The space data.</param>
    /// <param name="package">The package descriptor for this package.</param>
    /// <returns>True on success, false on error.</returns>
    protected override bool OnResPackageAvailable( IActivityMonitor monitor,
                                                   TypeScriptContext context,
                                                   ResSpaceData spaceData,
                                                   ResPackage package )
    {
        Throw.DebugAssert( !IsAppComponent );
        var fName = _kebabName + ".ts";
        if( !package.Resources.Resources.TryGetExpectedResource( monitor, fName, out var res ) )
        {
            return false;
        }
        var file = context.Root.Root.FindOrCreateResourceFile( res, TypeScriptFolder.AppendPart( fName ) );
        ITSDeclaredFileType tsType = Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( TypeScriptName );
        return base.OnResPackageAvailable( monitor, context, spaceData, package )
               && context.GetAngularCodeGen().ComponentManager.RegisterComponent( monitor, this, tsType );
    }
}
