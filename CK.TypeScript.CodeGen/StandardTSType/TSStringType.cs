using System;
using System.Globalization;

namespace CK.TypeScript.CodeGen
{
    class TSStringType : TSBasicType
    {
        public TSStringType()
            : base( "String", null, "''" )
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

