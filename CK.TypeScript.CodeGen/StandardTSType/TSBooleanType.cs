using System.Globalization;
using System.Numerics;

namespace CK.TypeScript.CodeGen
{
    class TSBooleanType : TSBasicType
    {
        public TSBooleanType()
            : base( "Boolean", null, "false" )
        {
        }

        protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
        {
            if( value is bool v )
            {
                writer.Append( v ? "true" : "false" );
                return true;
            }
            return false;
        }
    }
}

