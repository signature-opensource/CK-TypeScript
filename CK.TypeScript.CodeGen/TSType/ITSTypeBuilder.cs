using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Builder for <see cref="TSType"/>.
    /// They can be obtained thanks to <see cref="TypeScriptRoot.GetTSTypeBuilder()"/> and must be used
    /// only once: calling <see cref="Build"/> twice throws.
    /// </summary>
    public interface ITSTypeBuilder
    {
        /// <summary>
        /// Gets whether this builder has been already used and cannot be reused anymore.
        /// To build another type, call <see cref="TypeScriptRoot.GetTSTypeBuilder"/> again.
        /// </summary>
        bool BuiltDone { get; }

        /// <summary>
        /// Gets the part (a <see cref="ITSCodeWriter"/>) to use to build the <see cref="TSType.TypeName"/>.
        /// </summary>
        ITSCodePart TypeName { get; }

        /// <summary>
        /// Gets the part (a <see cref="ITSCodeWriter"/>) to use to build the <see cref="TSType.DefaultValueSource"/>.
        /// </summary>
        ITSCodePart DefaultValue { get; }

        /// <summary>
        /// Creates the resulting <see cref="TSType"/>.
        /// <see cref="BuiltDone"/> must be false otherwise a <see cref="InvalidOperationException"/> is thrown. 
        /// </summary>
        /// <param name="typeNameIsDefaultValueSource">True to use the type name as the <see cref="ITSType.DefaultValueSource"/>.</param>
        /// <returns>The built type.</returns>
        TSType Build( bool typeNameIsDefaultValueSource = false );
    }
}
