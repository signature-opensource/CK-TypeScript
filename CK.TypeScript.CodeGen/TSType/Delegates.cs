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

    /// <summary>
    /// A value writer function is in charge of writing a value of a <see cref="ITSGeneratedType"/>.
    /// </summary>
    /// <param name="writer">The target writer.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>True if the type can write the value, false otherwise.</returns>
    public delegate bool TSValueWriter( ITSCodeWriter writer, ITSGeneratedType type, object value );

    /// <summary>
    /// A deferred function to compute the <see cref="ITSType.GetDefaultValueSource(IActivityMonitor)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The <see cref="ITSType"/> for which the code must be generated.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>True if the type can write the value, false otherwise.</returns>
    public delegate string? DefaultValueSourceProvider( IActivityMonitor monitor, ITSGeneratedType type );

}
