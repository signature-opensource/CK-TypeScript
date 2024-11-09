using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular.Engine;

public partial class NgProviderAttributeImpl : TypeScriptPackageAttributeImplExtension
{
    public NgProviderAttributeImpl( NgProviderAttribute attr, Type type )
        : base( attr, type )
    {
    }

    public new NgProviderAttribute Attribute => Unsafe.As<NgProviderAttribute>( base.Attribute );


    protected override void OnInitialize( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, ITypeAttributesCache owner )
    {
    }

    protected override bool GenerateCode( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, TypeScriptContext context )
    {
        var angular = context.GetAngularCodeGen();
        var source = Type.ToCSharpName() + Attribute.SourceNameSuffix;
        angular.AddNgProvider( Attribute.ProviderCode, source );
        return true;
    }
}
