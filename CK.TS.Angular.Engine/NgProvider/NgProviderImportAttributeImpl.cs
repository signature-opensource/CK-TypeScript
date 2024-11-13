using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular.Engine;

public partial class NgProviderImportAttributeImpl : TypeScriptPackageAttributeImplExtension
{
    public NgProviderImportAttributeImpl( NgProviderImportAttribute attr, Type type )
        : base( attr, type )
    {
    }

    public new NgProviderImportAttribute Attribute => Unsafe.As<NgProviderImportAttribute>( base.Attribute );


    protected override void OnInitialize( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, ITypeAttributesCache owner )
    {
    }

    protected override bool GenerateCode( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, TypeScriptContext context )
    {
        var ckGen = context.GetAngularCodeGen().CKGenAppModule;

        ITSFileImportSection imports = ckGen.File.Imports;
        if( Attribute.LibraryName == "@local/ck-gen" )
        {
            imports.ImportFromLocalCKGen( Attribute.SymbolNames );
        }
        else
        {
            if( !context.Root.LibraryManager.LibraryImports.TryGetValue( Attribute.LibraryName, out var lib ) )
            {
                lib = context.Root.LibraryManager.RegisterLibrary( monitor,
                                                                   Attribute.LibraryName,
                                                                   TypeScript.CodeGen.DependencyKind.DevDependency,
                                                                   $"[{AttributeName}] on '{Type:C}'." );
            }
            imports.ImportFromLibrary( lib, Attribute.SymbolNames );
        }
        return true;
    }
}
