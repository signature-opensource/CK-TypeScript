using CK.Core;
using CK.Setup;
using CK.TypeScript.Engine;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgProviderAttribute"/>.
/// </summary>
public partial class NgProviderAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    /// <summary>
    /// Initializes a new implementation.
    /// </summary>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgProviderAttributeImpl( NgProviderAttribute attr, Type type )
        : base( attr, type )
    {
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new NgProviderAttribute Attribute => Unsafe.As<NgProviderAttribute>( base.Attribute );

    /// <inheritdoc />
    protected override bool OnConfiguredDescriptor( IActivityMonitor monitor,
                                                    TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                    TypeScriptContext context,
                                                    ResPackageDescriptor d,
                                                    ResSpaceConfiguration resourcesConfiguration )
    {
        var angular = context.GetAngularCodeGen();
        var source = Type.ToCSharpName() + Attribute.SourceNameSuffix;
        angular.AddNgProvider( Attribute.ProviderCode, source );
        return true;
    }
}
