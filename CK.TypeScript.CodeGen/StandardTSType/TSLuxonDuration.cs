using System;

namespace CK.TypeScript.CodeGen;

sealed class TSLuxonDuration : TSBasicType
{
    public TSLuxonDuration( TSTypeManager typeManager, LibraryImport luxonLib )
        : base( typeManager, "Duration", i => i.ImportFromLibrary( luxonLib, "Duration" ), "Duration.fromMillis(0)" )
    {
    }

    protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
    {
        if( value is TimeSpan v )
        {
            writer.Append( "Duration.fromMillis(" ).Append( v.TotalMilliseconds ).Append( ")" );
            return true;
        }
        return false;
    }
}

