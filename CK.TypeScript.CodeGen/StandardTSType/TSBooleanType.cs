using System.Globalization;
using System.Numerics;

namespace CK.TypeScript.CodeGen;

sealed class TSBooleanType : TSBasicType
{
    public TSBooleanType( TSTypeManager typeManager )
        : base( typeManager, "boolean", null, "false" )
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

