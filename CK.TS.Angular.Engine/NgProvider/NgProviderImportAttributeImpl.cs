using CK.Core;
using CK.Setup;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Runtime.CompilerServices;
using System.Linq;

namespace CK.TS.Angular.Engine;

public partial class NgProviderImportAttributeImpl : TypeScriptPackageAttributeImplExtension
{
    public NgProviderImportAttributeImpl( NgProviderImportAttribute attr, Type type )
        : base( attr, type )
    {
    }

    public new NgProviderImportAttribute Attribute => Unsafe.As<NgProviderImportAttribute>( base.Attribute );

    protected override bool OnConfiguredPackage( IActivityMonitor monitor,
                                                 TypeScriptPackageAttributeImpl tsPackage,
                                                 TypeScriptContext context,
                                                 ResPackageDescriptor d,
                                                 ResourceSpaceConfiguration resourcesConfiguration )
    {
        var ckGen = context.GetAngularCodeGen().CKGenAppModule;

        ITSFileImportSection imports = ckGen.File.Imports;
        if( Attribute.LibraryName == "@local/ck-gen" )
        {
            imports.ImportFromLocalCKGen( Attribute.SymbolNames );
        }
        else
        {
            // Note: npm packages follow a naming convention:
            // - if name starts with "@" then, it can only be: "@orgName/packageName" (ex: @angular/core)
            // - else it can only be "ng-zorro-antd" (no "/")
            var realLibraryName = Attribute.LibraryName;
            var subPath = "";
            var parts = realLibraryName.Split( '/' );
            if( parts.Length > 1 )
            {
                if( parts.Length > 2 )
                {
                    if( realLibraryName[0] == '@' )
                    {
                        realLibraryName = string.Join( '/', parts[0], parts[1] );
                        subPath = string.Join( '/', parts.Skip( 2 ) );
                    }
                }
                else
                {
                    Throw.DebugAssert( parts.Length == 2 );
                    if( realLibraryName[0] != '@' )
                    {
                        subPath = realLibraryName.Substring( parts[0].Length + 1 );
                        realLibraryName = parts[0];
                    }
                }
            }
            if( !context.Root.LibraryManager.LibraryImports.TryGetValue( realLibraryName, out var lib ) )
            {
                lib = context.Root.LibraryManager.RegisterLibrary( monitor,
                                                                   realLibraryName,
                                                                   TypeScript.CodeGen.DependencyKind.DevDependency,
                                                                   $"[{AttributeName}] on '{Type:C}'." );
            }
            imports.ImportFromLibrary( (lib,subPath), Attribute.SymbolNames );
        }
        return true;
    }
}
