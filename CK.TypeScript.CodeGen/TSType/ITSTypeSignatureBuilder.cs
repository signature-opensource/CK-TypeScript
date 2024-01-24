using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Builder for <see cref="TSBasicType"/>.
    /// They can be obtained thanks to <see cref="TypeScriptRoot.GetTSTypeSignatureBuilder()"/> and must be used
    /// only once: calling <see cref="Build"/> twice throws.
    /// <para>
    /// This can be used for types that have dependencies that appears in their <see cref="TypeName"/>
    /// and/or <see cref="DefaultValue"/>: by using the exposed code parts, codes can be written as usual
    /// and the built type imports are automatically computed (typically by using <see cref="TSCodeWriterExtensions.AppendTypeName{T}(T, ITSType, bool)"/>).
    /// </para>
    /// <para>
    /// This is used to build generic interface and anonymous records types.
    /// </para>
    /// </summary>
    public interface ITSTypeSignatureBuilder
    {
        /// <summary>
        /// Gets whether this builder has been already used and cannot be reused anymore.
        /// To build another type, call <see cref="TypeScriptRoot.GetTSTypeSignatureBuilder"/> again.
        /// </summary>
        bool BuiltDone { get; }

        /// <summary>
        /// Gets the part (a <see cref="ITSCodeWriter"/>) to use to build the <see cref="TSBasicType.TypeName"/>.
        /// </summary>
        ITSCodePart TypeName { get; }

        /// <summary>
        /// Gets the part (a <see cref="ITSCodeWriter"/>) to use to build the <see cref="TSBasicType.DefaultValueSource"/>.
        /// </summary>
        ITSCodePart DefaultValue { get; }

        /// <summary>
        /// Creates the resulting <see cref="TSBasicType"/>.
        /// <see cref="BuiltDone"/> must be false otherwise a <see cref="InvalidOperationException"/> is thrown. 
        /// </summary>
        /// <param name="typeNameIsDefaultValueSource">True to use the type name as the <see cref="ITSType.DefaultValueSource"/>.</param>
        /// <returns>The built type.</returns>
        TSBasicType Build( bool typeNameIsDefaultValueSource = false );
    }
}
