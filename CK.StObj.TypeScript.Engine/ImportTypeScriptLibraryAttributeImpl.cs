using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;

namespace CK.StObj.TypeScript.Engine;

/// <summary>
/// Registers the TypeScript library in the TypeScriptContext's LibraryManager.
/// This is stateless: the factory is the code generator.
/// </summary>
public sealed class ImportTypeScriptLibraryAttributeImpl : ITSCodeGeneratorFactory, ITSCodeGenerator
{
    readonly ImportTypeScriptLibraryAttribute _attr;
    readonly Type _target;

    public ImportTypeScriptLibraryAttributeImpl( ImportTypeScriptLibraryAttribute attr, Type target )
    {
        _attr = attr;
        _target = target;
    }

    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        return this;
    }

    bool ITSCodeGenerator.StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        var lib = context.Root.LibraryManager.RegisterLibrary( monitor,
                                                               _attr.Name,
                                                               _attr.Version,
                                                               (CK.TypeScript.CodeGen.DependencyKind)_attr.DependencyKind,
                                                               definitionSource: _target.ToCSharpName() );
        if( lib == null ) return false;
        lib.IsUsed = _attr.ForceUse;
        return true;
    }

    bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;
}
