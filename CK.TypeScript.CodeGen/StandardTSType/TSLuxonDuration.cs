using System;
using System.Globalization;

namespace CK.TypeScript.CodeGen
{
    class TSLuxonDuration : TSType
    {
        public TSLuxonDuration( LibraryImport luxonLib )
            : base( "Duration", i => i.EnsureImportFromLibrary( luxonLib, "Duration" ), "Duration.fromMillis(0)" )
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
}

