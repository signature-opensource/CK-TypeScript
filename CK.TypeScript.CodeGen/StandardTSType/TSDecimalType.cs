using System.Globalization;

namespace CK.TypeScript.CodeGen;

sealed class TSDecimalType : TSBasicType
{
    public TSDecimalType( TSTypeManager typeManager, LibraryImport decimalLib )
        : base( typeManager, "Decimal", i => i.EnsureImportFromLibrary( decimalLib, "Decimal" ), "new Decimal(0)" )
    {
    }

    protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
    {
        if( value is decimal d )
        {
            WriteValue( writer, d.ToString( CultureInfo.InvariantCulture ) );
            return true;
        }
        return false;

        static void WriteValue( ITSCodeWriter writer, string num )
        {
            writer.Append( "new Decimal('" ).Append( num ).Append( "')" );
        }

    }
}

