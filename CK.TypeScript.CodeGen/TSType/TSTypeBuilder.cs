using CK.Core;
using System.Text;

namespace CK.TypeScript.CodeGen;

sealed class TSTypeBuilder : ITSTypeSignatureBuilder
{
    readonly RawCodePart _typeName;
    readonly RawCodePart _defaultValue;
    readonly TypeScriptRoot _root;
    internal TSTypeBuilder? _nextFree;

    internal TSTypeBuilder( TypeScriptFolder hiddenRoot, int tsBuilderCount )
    {
        var workFile = new TypeScriptFile( hiddenRoot, $"builder{tsBuilderCount}.ts" );
        _root = hiddenRoot.Root;
        _typeName = new RawCodePart( workFile, string.Empty );
        _defaultValue = new RawCodePart( workFile, string.Empty );
    }

    public bool BuiltDone => _root.IsInPool( this );

    public ITSCodePart TypeName => _typeName;

    public ITSCodePart DefaultValue => _defaultValue;

    public ITSType Build( bool typeNameIsDefaultValueSource = false )
    {
        Throw.CheckState( !BuiltDone );
        var b = new SmarterStringBuilder( new StringBuilder() );
        _typeName.Build( ref b, false );
        var tName = b.ToString().Trim();
        var ts = _root.TSTypes.FindByTypeName( tName );
        if( ts == null )
        {
            string? defaultValueSource = tName;
            if( !typeNameIsDefaultValueSource )
            {
                b.Reset();
                _defaultValue.Build( ref b, false );
                defaultValueSource = b.ToString().Trim();
                if( defaultValueSource.Length == 0 ) defaultValueSource = null;
            }
            var imports = _typeName.File._imports.CreateImportSnapshot();
            ts = new TSBasicType( _root.TSTypes, tName, imports, defaultValueSource );
        }
        _typeName.File._imports.ClearImports();
        _typeName.Clear();
        _defaultValue.Clear();
        _root.Return( this );
        return ts;
    }
}

