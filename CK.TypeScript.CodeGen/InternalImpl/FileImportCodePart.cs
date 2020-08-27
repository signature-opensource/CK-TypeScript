using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    class FileImportCodePart : BaseCodeWriter, ITSFileImportSection
    {
        public FileImportCodePart( TypeScriptFile f )
        {
            File = f;
        }

        public TypeScriptFile File { get; }
    }
}
