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
        //
        // Doing this has an impact: the resource moved to the <App> code becomes reachable from any
        // package.
        // To minimize this, we hide the resource from its container and publish it from the code container
        // only if the attribute specifies a TargetFolder.
        // When no target folder is specified, we only register its TS type names and the resource "stays"
        // in its container.
        //
        bool isPublishedByCodeContainer = Attribute.TargetFolder != null;
        EmbeddedResources.ResourceLocator resource;
        NormalizedPath targetPath;
        if( isPublishedByCodeContainer )
        {
            if( !d.RemoveExpectedCodeHandledResource( monitor, Attribute.ResourcePath, out resource ) )
            {
                return false;
            }
            targetPath = Attribute.TargetFolder;
            targetPath = targetPath.ResolveDots();
            targetPath = targetPath.Combine( Path.GetFileName( Attribute.ResourcePath ) );
        }
        else
        {
            if( !d.Resources.TryGetExpectedResource( monitor, Attribute.ResourcePath, out resource, d.AfterResources ) )
            {
                return false;
            }
            targetPath = tsPackage.TypeScriptFolder.Combine( Attribute.ResourcePath );
        }
        var file = context.Root.Root.FindOrCreateResourceFile( resource, targetPath, isPublishedByCodeContainer );
        foreach( var tsType in Attribute.TypeNames )
        {
            if( string.IsNullOrWhiteSpace( tsType ) ) continue;
            file.DeclareType( tsType );
        }
        return true;
    }

}
