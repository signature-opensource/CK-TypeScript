using CK.Core;
using CK.Setup;
using CK.TypeScript.Engine;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular.Engine;

public partial class NgProviderAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    public NgProviderAttributeImpl( NgProviderAttribute attr, Type type )
        : base( attr, type )
    {
    }

    public new NgProviderAttribute Attribute => Unsafe.As<NgProviderAttribute>( base.Attribute );

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
