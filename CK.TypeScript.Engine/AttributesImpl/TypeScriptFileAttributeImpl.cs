using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.Engine;

/// <summary>
/// Creates a TypeScript resource file (from an embedded '.ts' resource).
/// </summary>
public sealed class TypeScriptFileAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    /// <summary>
    /// Initializes a new <see cref="TypeScriptFileAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptFileAttributeImpl( IActivityMonitor monitor, TypeScriptFileAttribute attr, Type type )
        : base( attr, type )
    {
        if( string.IsNullOrWhiteSpace( Attribute.ResourcePath ) || !Attribute.ResourcePath.EndsWith( ".ts" ) )
        {
            monitor.Error( $"[TypeScriptFile( \"{Attribute.ResourcePath}\" )] on '{type:N}': invalid resource path. It must end with \".ts\"." );
        }
    }

    /// <summary>
    /// Gets the attribute/
    /// </summary>
    public new TypeScriptFileAttribute Attribute => Unsafe.As<TypeScriptFileAttribute>( base.Attribute );

    /// <inheritdoc/>
    protected internal override bool OnConfiguredDescriptor( IActivityMonitor monitor, TypeScriptGroupOrPackageAttributeImpl tsPackage, TypeScriptContext context, ResPackageDescriptor d, ResSpaceConfiguration spaceBuilder )
    {
        if( !d.RemoveExpectedCodeHandledResource( monitor, Attribute.ResourcePath, out var resource ) )
        {
            return false;
        }
        NormalizedPath targetPath = Attribute.TargetFolder ?? tsPackage.TypeScriptFolder;
        targetPath = targetPath.ResolveDots().AppendPart( Path.GetFileName( Attribute.ResourcePath ) );
        var file = context.Root.Root.FindOrCreateResourceFile( in resource, targetPath );
        foreach( var tsType in Attribute.TypeNames )
        {
            if( string.IsNullOrWhiteSpace( tsType ) ) continue;
            Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( tsType );
        }
        return true;
    }

}
