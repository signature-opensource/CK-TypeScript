using CK.CodeGen;
using CK.Core;
using CK.EmbeddedResources;
using CK.Setup;
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
    protected internal override bool OnResPackageAvailable( IActivityMonitor monitor,
                                                             TypeScriptContext context,
                                                             TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                             ResSpaceData spaceData,
                                                             ResPackage package )
    {
        // There is currently no way to define a target path for a specific resource in resource container
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
        // It seems that we miss an explicit "ResourceMapping" that would be a Dictionary<ResouceLocator,NormalizedPath>
        // to support resource relocation...  But what about the resource in the StoreContainer? Does it appear or is it
        // "hidden" like a code handled one?
        // If it doesn't, it will be missed by all lookup in package's Reachables' resources. And it should definitly not.
        // If it does, all accesses to "target folder" MUST systematically apply the "ResourceMapping" relocator... and
        // that's a little bit dangerous.
        //
        // A third option would be that package's StoreContainer are NOT IResourceContainer but containers
        // of (ResourceLocator,TargetPath). This would be a mess: this absolutely doesn't apply to <Code> and
        // <App> packages, breaking the uniform model.
        //
        // A fourth option would be that each and every ResourceLocator has an optional TargetPath. This introduces yet
        // another complexity in the model, impacts performances and make some IResourceContainer implementations
        // really complex because ResourceLocators would have to be cached to remmember their TargetPath.
        //
        // Eventually, the true question is: is this TargetFolder relocation useful?
        // I think (14th of Aug, 2025) that we're lack hindsight on this.
        // So, we [Obsolete] warning the TargetFolder and emits a ToBeInvestigated wanring if it is used...

#pragma warning disable CS0618 // Type or member is obsolete
        bool hasTargetFolder = Attribute.TargetFolder != null;
        bool hasExportedTypes = Attribute.TypeNames.Length > 0;
        EmbeddedResources.ResourceLocator resource;
        NormalizedPath targetPath;
        if( hasTargetFolder )
        {
            monitor.Warn( ActivityMonitor.Tags.ToBeInvestigated, $"Type '{Type:N}' uses [TypeScriptFile] with TargetFolder = \"{Attribute.TargetFolder}\". Rationales should be mailed to 'olivier.spinelli@signature.one'." );
            if( !spaceData.RemoveExpectedCodeHandledResource( monitor, package, Attribute.ResourcePath, out resource ) )
            {
                return false;
            }
            // TargetFolder + Name only.
            targetPath = Attribute.TargetFolder;
            targetPath = targetPath.ResolveDots();
            targetPath = targetPath.Combine( Path.GetFileName( Attribute.ResourcePath ) );
        }
        else
        {
            if( !package.Resources.Resources.TryGetExpectedResource( monitor,
                                                                     Attribute.ResourcePath,
                                                                     out resource,
                                                                     package.AfterResources.Resources ) )
            {
                return false;
            }
            if( !hasExportedTypes )
            {
                // No TargetFolder nor TypeNames: this only check that the file exists...
                return true;
            }
            // Regular case.
            targetPath = tsPackage.TypeScriptFolder.Combine( Attribute.ResourcePath );
        }
#pragma warning restore CS0618 // Type or member is obsolete
        bool success = true;
        var file = context.Root.Root.FindOrCreateResourceFile( resource, targetPath, hasTargetFolder );
        foreach( var tsType in Attribute.TypeNames )
        {
            if( string.IsNullOrWhiteSpace( tsType ) ) continue;
            if( tsType.AsSpan().ContainsAny( ',', ';' ) )
            {
                monitor.Error( $"[TypeScriptFile( \"{Attribute.ResourcePath}\", \"{tsType}\" )] on '{Type:N}': expected single type name. Commas or semicolons are not supported." );
                success = false;
            }
            else
            {
                file.DeclareType( tsType );
            }
        }
        return success;
    }
}
