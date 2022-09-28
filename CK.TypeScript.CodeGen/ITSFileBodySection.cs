using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// The body part of a <see cref="TypeScriptFile"/>.
    /// <para>
    /// This body doesn't expose the file to which it belongs and this is intended. Code generators must work with <see cref="TypeScriptFile"/>
    /// and use parts locally, keeping this relationship explicit.
    /// </para>
    /// </summary>
    public interface ITSFileBodySection : ITSCodePart
    {
    }
}
