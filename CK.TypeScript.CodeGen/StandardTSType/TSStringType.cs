using System;
using System.Globalization;

namespace CK.TypeScript.CodeGen
{
    sealed class TSStringType : TSBasicType
    {
        public TSStringType( TSTypeManager typeManager )
            : base( typeManager, "String", null, "''" )
        {
        }

        protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
        {
            if( value is string v )
            {
                writer.AppendSourceString( v );
                return true;
            }
            return false;
        }
    }
}

