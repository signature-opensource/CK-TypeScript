using CK.Core;
using CK.Setup;
using CK.TypeScript;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgModuleAttribute"/>.
/// </summary>
public class NgModuleAttributeImpl : TypeScriptPackageAttributeImpl
{
    readonly string _snakeName;

    /// <summary>
    /// Initializes a new <see cref="NgModuleAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgModuleAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type )
        : base( monitor, attr, type )
    {
        if( !typeof( NgModule ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[NgModule] can only decorate a NgModule: '{type:N}' is not a NgModule." );
        }
        _snakeName = NgComponentAttributeImpl.CheckComponentName( monitor, type, "Module" );
        SetTypeScriptFolder( TypeScriptFolder.AppendPart( _snakeName ) );
    }

    /// <summary>
    /// Gets the module name that is the C# <see cref="TypeScriptFileAttributeImpl.DecoratedType"/> name (with "Module" suffix).
    /// </summary>
    public string ModuleName => DecoratedType.Name;

    protected override bool OnConfiguredPackage( IActivityMonitor monitor, TypeScriptContext context, ResourceSpaceCollector spaceBuilder, ResPackageDescriptor d )
    {
        var fName = _snakeName + ".module.ts";
        if( !d.RemoveExpectedCodeHandledResource( monitor, fName, out var res ) )
        {
            return false;
        }
        var file = context.Root.Root.FindOrCreateResourceFile( in res, TypeScriptFolder.AppendPart( fName ) );
        Throw.DebugAssert( file is ResourceTypeScriptFile );
        ITSDeclaredFileType tsType = Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( ModuleName );

        return base.OnConfiguredPackage( monitor, context, spaceBuilder, d )
               && context.GetAngularCodeGen().RegisterModule( monitor, this, tsType );
    }

}
