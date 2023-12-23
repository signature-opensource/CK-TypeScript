using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Models a purely TypeScript type.
    /// Such type doesn't necessarily correspond to a single C# <see cref="Type"/>.
    /// </summary>
    public interface ITSType
    {
        /// <summary>
        /// Gets the type name.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets whether this type is nullable ("undefinable").
        /// </summary>
        bool IsNullable { get; }

        /// <summary>
        /// Gets whether the <see cref="DefaultValueSource"/> is not null.
        /// Nullable types can have a default value even if "undefined" is always allowed
        /// and can be used as the ultimate default.
        /// </summary>
        bool HasDefaultValue { get; }

        /// <summary>
        /// Gets the type script code source that initializes
        /// a default value of this type.
        /// </summary>
        string? DefaultValueSource { get; }

        /// <summary>
        /// Gets the required imports.
        /// </summary>
        Action<ITSFileImportSection>? RequiredImports { get; }

        /// <summary>
        /// Gets the nullable associated type.
        /// </summary>
        ITSType Nullable { get; }

        /// <summary>
        /// Gets the non nullable type.
        /// </summary>
        ITSType NonNullable { get; }

        /// <summary>
        /// Attempts to write a value.
        /// This must be overridden based on the actual type that can be handled by this <see cref="ITSType"/>.
        /// </summary>
        /// <param name="writer">The target writer.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if this type has been able to write the value, false otherwise.</returns>
        bool TryWriteValue( ITSCodeWriter writer, object value );
    }
}
