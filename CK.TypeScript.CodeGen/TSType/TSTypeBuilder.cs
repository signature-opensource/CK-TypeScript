using CK.Core;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    sealed class TSTypeBuilder : ITSTypeSignatureBuilder
    {
        readonly TypeScriptFile _virtualFile;
        readonly RawCodePart _typeName;
        readonly RawCodePart _defaultValue;
        readonly TypeScriptRoot _root;
        internal TSTypeBuilder? _nextFree;

        internal TSTypeBuilder( TypeScriptRoot root )
        {
            _root = root;
            _virtualFile = _root.Root.FindOrCreateFile( TypeScriptFile._hiddenFileName );
            _typeName = new RawCodePart( _virtualFile, string.Empty );
            _defaultValue = new RawCodePart( _virtualFile, string.Empty );
        }

        public bool BuiltDone => _root.IsInPool( this );

        public ITSCodePart TypeName => _typeName;

        public ITSCodePart DefaultValue => _defaultValue;

        public ITSType Build( bool typeNameIsDefaultValueSource = false )
        {
            Throw.CheckState( !BuiltDone );
            var b = new SmarterStringBuilder( new StringBuilder() );
            var tName = _typeName.Build( b, false ).ToString().Trim();
            var ts = _root.TSTypes.FindByTypeName( tName );
            if( ts != null ) return ts;
            b.Reset();
            var defaultValueSource = typeNameIsDefaultValueSource ? tName : _defaultValue.Build( b, false ).ToString().Trim();
            if( defaultValueSource.Length == 0 ) defaultValueSource = null;
            b.Reset();
            var imports = _virtualFile._imports.CreateImportSnapshotAndClear();
            _typeName.Clear();
            _defaultValue.Clear();
            _root.Return( this );
            return new TSBasicType( _root.TSTypes, tName, imports, defaultValueSource );
        }
    }
}

