using CK.Core;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// A code generator function is in charge of generating the TypeScript implementation
    /// of a <see cref="ITSGeneratedType"/> by calling <see cref="ITSGeneratedType.EnsureTypePart(string, bool)"/>
    /// and append the code into the <see cref="ITSKeyedCodePart"/>.
    /// <para>
    /// The generator function has access to the <see cref="ITSGeneratedType.File"/> that hosts the code
    /// (with its <see cref="TypeScriptFile.Imports"/> section) and to the whole generation context
    /// thanks to <see cref="TypeScriptFile.Root"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The <see cref="ITSGeneratedType"/> for which the code must be generated.</param>
    /// <returns>True on success, false on error (errors must be logged).</returns>
    public delegate bool TSCodeGenerator( IActivityMonitor monitor, ITSGeneratedType type );
}
