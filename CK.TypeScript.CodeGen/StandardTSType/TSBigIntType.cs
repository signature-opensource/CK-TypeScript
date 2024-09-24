using System.Globalization;
using System.Numerics;

namespace CK.TypeScript.CodeGen;

sealed class TSBigIntType : TSBasicType
{
    public TSBigIntType( TSTypeManager typeManager )
        : base( typeManager, "bigint", null, "0n" )
    {
    }

    protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
    {
        switch( value )
        {
            case long v: WriteValue( writer, v.ToString( CultureInfo.InvariantCulture ) ); break;
            case ulong v: WriteValue( writer, v.ToString( CultureInfo.InvariantCulture ) ); break;
            case decimal v: WriteValue( writer, v.ToString( CultureInfo.InvariantCulture ) ); break;
            case BigInteger v: WriteValue( writer, v.ToString( CultureInfo.InvariantCulture ) ); break;
            default: return false;
        }
        return true;

        static void WriteValue( ITSCodeWriter writer, string num )
        {
            writer.Append( num ).Append( "n" );
        }

    }

}

