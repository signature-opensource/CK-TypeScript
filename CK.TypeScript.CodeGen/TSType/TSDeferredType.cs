using CK.Core;
using System;
using System.Data;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// This class is internal: only the ITSFileCSharpType is exposed.
/// </summary>
sealed class TSDeferredType : TSType, ITSFileCSharpType
{
    readonly TSValueWriter? _tryWriteValue;
    internal readonly TSCodeGenerator? _codeGenerator;
    readonly ITSKeyedCodePart _part;
    readonly Type _type;
    readonly TypeScriptFile _file;
    string? _defaultValueSource;
    readonly bool _hasError;

    internal TSDeferredType( TSTypeManager typeManager,
                              Type t,
                              string typeName,
                              TypeScriptFile file,
                              string? defaultValue,
                              TSValueWriter? tryWriteValue,
                              TSCodeGenerator? codeGenerator,
                              string partCloser,
                              bool hasError )
        : base( typeManager, typeName )
    {
        Throw.DebugAssert( t != null );
        Throw.DebugAssert( file != null );
        _type = t;
        _file = file;
        _tryWriteValue = tryWriteValue;
        _codeGenerator = codeGenerator;
        _defaultValueSource = defaultValue;
        _part = file.Body.FindOrCreateKeyedPart( this, partCloser );
        _hasError = hasError;
    }

    public Type Type => _type;

    IMinimalTypeScriptFile ITSDeclaredFileType.File => _file;

    public override TypeScriptFile File => _file;

    public override string? DefaultValueSource => _defaultValueSource;

    internal void SetDefaultValueSource( string? v ) => _defaultValueSource = v;


    public bool HasError => _hasError;

    public override void EnsureRequiredImports( ITSFileImportSection section )
    {
        section.EnsureImport( _file, TypeName );
    }

    protected override bool DoTryWriteValue( ITSCodeWriter writer, object value ) => _tryWriteValue?.Invoke( writer, this, value ) ?? false;

    public ITSCodePart TypePart => _part;

}

