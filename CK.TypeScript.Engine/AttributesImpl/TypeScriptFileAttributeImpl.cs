using CK.Core;
using CK.EmbeddedResources;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

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
    protected internal override bool OnConfiguredDescriptor( IActivityMonitor monitor, TypeScriptContext context, TypeScriptGroupOrPackageAttributeImpl tsPackage, ResPackageDescriptor d, ResSpaceConfiguration spaceBuilder )
    {
        // There is currently no way to define a target path for a specific resource in reource container
        // (resource containers have no notion of "target folder").
        // So we use the TypeScript CodeGen model for this: we transfer the resource to the code by
        // removing/hiding the resource from its container and registering the resource as a published one.
        if( !d.RemoveExpectedCodeHandledResource( monitor, Attribute.ResourcePath, out var resource ) )
        {
            return false;
        }
        NormalizedPath targetPath = Attribute.TargetFolder ?? tsPackage.TypeScriptFolder;
        targetPath = targetPath.ResolveDots().AppendPart( Path.GetFileName( Attribute.ResourcePath ) );
        var file = context.Root.Root.FindOrCreateResourceFile( resource, targetPath, publishedResource: true );
        foreach( var tsType in Attribute.TypeNames )
        {
            if( string.IsNullOrWhiteSpace( tsType ) ) continue;
            Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( tsType );
        }
        return true;
    }

}
