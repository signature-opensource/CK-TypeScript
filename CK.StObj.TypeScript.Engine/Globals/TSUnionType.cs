using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Engine
{
    public sealed class TSUnionType : TSBasicType
    {
        readonly IReadOnlyList<(IPocoType,ITSType)> _types;

        public TSUnionType( TSTypeManager typeManager, string typeName, Action<ITSFileImportSection>? imports, IReadOnlyList<(IPocoType, ITSType)> types )
            : base( typeManager, typeName, imports, null )
        {
            _types = types;
        }

        public IReadOnlyList<(IPocoType PocoType, ITSType TSType)> Types => _types;

        protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
        {
            foreach( var (_,ts) in _types )
            {
                if( ts.TryWriteValue( writer, value ) ) return true;
            }
            return false;
        }
    }
}
