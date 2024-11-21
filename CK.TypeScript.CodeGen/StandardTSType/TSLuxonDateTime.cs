using System;

namespace CK.TypeScript.CodeGen;

sealed class TSLuxonDateTime : TSBasicType
{
    public TSLuxonDateTime( TSTypeManager typeManager, LibraryImport luxonLib )
        : base( typeManager, "DateTime", i => i.ImportFromLibrary( luxonLib, "DateTime" ), "DateTime.utc(1,1,1)" )
    {
    }

    protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
    {
        switch( value )
        {
            case DateTime v: WriteValue( writer, v ); break;
            case DateTimeOffset v: WriteValue( writer, v ); break;
            default: return false;
        }
        return true;
    }

    static void WriteValue( ITSCodeWriter writer, in DateTime d )
    {
        writer.Append( "DateTime.utc(" )
              .Append( d.Year ).Append( "," )
              .Append( d.Month ).Append( "," )
              .Append( d.Day ).Append( "," )
              .Append( d.Hour ).Append( "," )
              .Append( d.Minute ).Append( "," )
              .Append( d.Second ).Append( "," )
              .Append( d.Millisecond ).Append( ")" );
    }

    static void WriteValue( ITSCodeWriter writer, in DateTimeOffset d )
    {
        writer.Append( "DateTime.fromISO(" )
              .AppendSourceString( d.ToString( "O" ) )
              .Append( ", {setZone:true})" );
    }
}

