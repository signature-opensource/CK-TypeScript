using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Defines a C# type that is locally implemented in a <see cref="File"/>.
    /// </summary>
    public interface ITSGeneratedType : ITSType
    {
        /// <summary>
        /// Gets the C# type.
        /// <para>
        /// Note that nullability is not enforced for value types: when this <see cref="ITSType.IsNullable"/>
        /// is true, this is always the non nullable type, even for value type.
        /// </para>
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the local file that implements this type.
        /// </summary>
        new TypeScriptFile File { get; }

        /// <summary>
        /// Gets whether this type is on error. 
        /// </summary>
        bool HasError { get; }

        /// <summary>
        /// Gets the code part with the <see cref="Type"/> key if it has been created, null otherwise.
        /// The code part may have been created by <see cref="EnsureTypePart"/> or directly on
        /// the file's body. 
        /// </summary>
        ITSKeyedCodePart? TypePart { get; }

        /// <summary>
        /// Ensures that a code part with the key <see cref="Type"/> exists in this file's body.
        /// </summary>
        /// <param name="closer">
        /// By default, the type part will be closed with the "}" + <see cref="Environment.NewLine"/>: a closing
        /// bracket should not be generated and, more importantly, it means that the type part can
        /// easily be extended.
        /// </param>
        /// <param name="top">
        /// Optionally creates the new part at the start of the code instead of at the
        /// current writing position in the code.
        /// </param>
        /// <returns>The part for this type.</returns>
        ITSKeyedCodePart EnsureTypePart( string closer = "}\n", bool top = false );

        /// <inheritdoc cref="ITSType.Nullable" />
        new ITSGeneratedType Nullable { get; }

        /// <inheritdoc cref="ITSType.NonNullable"/>
        new ITSGeneratedType NonNullable { get; }
    }

}

