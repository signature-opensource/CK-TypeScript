using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Extends <see cref="ITSCodePart"/> with a key that identifies it and
    /// an object tag that can be used freely.
    /// </summary>
    public interface ITSKeyedCodePart : ITSCodePart
    {
        /// <summary>
        /// Gets the key that identifies this part in its parent.
        /// </summary>
        object Key { get; }
    }
}
