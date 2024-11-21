using System;

namespace CK.TypeScript.CodeGen;

sealed class FileBodyCodePart : RawCodePart, ITSFileBodySection
{
    public FileBodyCodePart( TypeScriptFile f )
        : base( f, String.Empty )
    {
    }
}
