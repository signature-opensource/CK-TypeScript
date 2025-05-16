using CK.Core;
using CK.Setup;
using CK.TypeScript.Engine;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgAppStyleImportAttribute"/>.
/// </summary>
public partial class NgAppStyleImportAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    /// <summary>
    /// Initializes a new implementation.
    /// </summary>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgAppStyleImportAttributeImpl( NgAppStyleImportAttribute attr, Type type )
        : base( attr, type )
    {
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new NgAppStyleImportAttribute Attribute => Unsafe.As<NgAppStyleImportAttribute>( base.Attribute );

    /// <inheritdoc />
    protected override bool OnConfiguredDescriptor( IActivityMonitor monitor,
                                                    TypeScriptContext context,
                                                    TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                    ResPackageDescriptor d,
                                                    ResSpaceConfiguration resourcesConfiguration )
    {
        return true;
    }

    protected override bool OnResPackageAvailable( IActivityMonitor monitor,
                                                   TypeScriptContext context,
                                                   TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                   ResPackage resPackage )
    {
        context.GetAngularCodeGen().AddAppStyle( Attribute.AfterContent
                                                      ? resPackage.AfterResources.Index
                                                      : resPackage.Resources.Index,
                                                 Attribute.ImportPath );
        return true;
    }
}



