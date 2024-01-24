using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Engine
{
    public sealed class TSUnionType : TSBasicType
    {
        readonly IReadOnlyList<ITSType> _types;

        public TSUnionType( string typeName, Action<ITSFileImportSection>? imports, IReadOnlyList<ITSType> types )
            : base( typeName, imports, null )
        {
            _types = types;
        }

        public IReadOnlyList<ITSType> Types => _types;

        protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
        {
            foreach( var t in _types )
            {
                if( t.TryWriteValue( writer, value ) ) return true;
            }
            return false;
        }
    }
}
