using CK.Core;
using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Models a purely TypeScript type.
    /// Such type may correspond to a more than one or no C# <see cref="Type"/>.
    /// </summary>
    public interface ITSType
    {
        /// <summary>
        /// Gets the type name.
        /// For a C# nullable type (<see cref="IsNullable"/> is true), this type name is
        /// the <see cref="NonNullable"/> one with "|undefined".
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets the optional, question marked, type name.
        /// If <see cref="IsNullable"/> is true, this is <c>NonNullable.TypeName?</c>.
        /// </summary>
        string OptionalTypeName { get; }

        /// <summary>
        /// Gets whether this type is nullable ("undefinable" in TypeScript).
        /// </summary>
        bool IsNullable { get; }

        /// <summary>
        /// Gets whether this is the "Number", "String", "Boolean" or "BigInt" type.
        /// These types are handled automatically by the <see cref="TypeScriptRoot"/>.
        /// </summary>
        bool IsPrimitive => ReferenceEquals( TypeName, "String" )
                            || ReferenceEquals( TypeName, "Number" )
                            || ReferenceEquals( TypeName, "Boolean" )
                            || ReferenceEquals( TypeName, "BigInt" );

        /// <summary>
        /// Gets a unique index for this TS type.
        /// When <see cref="IsNullable"/> is true this is the negative non nullable index.
        /// The value 0 is invalid.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets this type model in the TSType file.
        /// <para>
        /// <see cref="TypeScriptRoot.ReflectTS"/> must be true for for this to be not null.
        /// </para>
        /// <para>
        /// This part is an extension point after the fields "tsName" and "index" that are automatically
        /// written for all types. Other information can be defined such as a fields descriptor for composite
        /// types like:
        /// <code>
        /// "fields": { "name": TSType["String"], "isOn": TSType["Boolean"], "uid": TSType["Guid"] }
        /// </code>
        /// </para>
        /// </summary>
        ITSCodePart? TSTypeModel { get; }

        /// <summary>
        /// Gets the nullable associated type.
        /// </summary>
        ITSType Nullable { get; }

        /// <summary>
        /// Gets the non nullable type.
        /// </summary>
        ITSType NonNullable { get; }

        /// <summary>
        /// Gets the TypeScript code source that initializes
        /// a default value of this type.
        /// <para>
        /// Null when this type has no default: this should concern only composites
        /// (means that at least one of its field must be explcitely provided) or abstractions
        /// (no concrete type can be selected).
        /// </para>
        /// <para>
        /// When <see cref="IsNullable"/> is true, this is "undefined".
        /// </para>
        /// </summary>
        string? DefaultValueSource { get; }

        /// <summary>
        /// Ensures that all imports required to use this <see cref="ITSType"/> are declared
        /// in the target section.
        /// </summary>
        /// <param name="section">The import section target.</param>
        void EnsureRequiredImports( ITSFileImportSection section );

        /// <summary>
        /// Attempts to write a value.
        /// This must be overridden based on the actual type that can be handled by this <see cref="ITSType"/>.
        /// </summary>
        /// <param name="writer">The target writer.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if this type has been able to write the value, false otherwise.</returns>
        bool TryWriteValue( ITSCodeWriter writer, object? value );

    }
}
