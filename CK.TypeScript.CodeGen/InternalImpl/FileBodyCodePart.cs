using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    class FileBodyCodePart : RawCodePart, ITSFileBodySection
    {
        public FileBodyCodePart( TypeScriptFile f )
            : base( f, String.Empty )
        {
        }
    }
}
