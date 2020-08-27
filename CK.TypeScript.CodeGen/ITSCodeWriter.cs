using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Most basic interface: a simple string fragment collector.
    /// </summary>
    public interface ITSCodeWriter
    {
        /// <summary>
        /// Adds a raw string to this writer.
        /// </summary>
        /// <param name="code">Raw type script code. Can be null or empty.</param>
        void DoAdd( string? code );

    }
}
