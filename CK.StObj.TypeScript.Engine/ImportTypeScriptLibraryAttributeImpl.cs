using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;

namespace CK.StObj.TypeScript.Engine;

public sealed class ImportTypeScriptLibraryAttributeImpl : ITSCodeGenerator
{
    readonly ImportTypeScriptLibraryAttribute _attr;
    readonly Type _target;

    public ImportTypeScriptLibraryAttributeImpl( ImportTypeScriptLibraryAttribute attr, Type target )
    {
        _attr = attr;
        _target = target;
    }

    public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer ) => true;

    public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        var lib = context.Root.LibraryManager.RegisterLibrary( monitor,
                                                               _attr.Name,
                                                               _attr.Version,
                                                               (CK.TypeScript.CodeGen.DependencyKind)_attr.DependencyKind,
                                                               _target.ToCSharpName() );
        if( lib == null ) return false;
        lib.IsUsed = _attr.ForceUse;
        return true;
    }
}
