using CK.Core;
using CK.Setup;
using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.Engine;

/// <summary>
/// Implements <see cref="NgAppStyleImportAttribute"/>.
/// </summary>
public partial class AppStyleImportAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    /// <summary>
    /// Initializes a new implementation.
    /// </summary>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public AppStyleImportAttributeImpl( AppStyleImportAttribute attr, Type type )
        : base( attr, type )
    {
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new AppStyleImportAttribute Attribute => Unsafe.As<AppStyleImportAttribute>( base.Attribute );

    internal protected override bool OnResPackageAvailable( IActivityMonitor monitor,
                                                            TypeScriptContext context,
                                                            TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                            ResSpaceData spaceData,
                                                            ResPackage package )
    {
        context.AddAppStyle( Attribute.AfterContent
                                ? package.AfterResources.Index
                                : package.Resources.Index,
                             Attribute.ImportPath );
        return true;
    }
}



