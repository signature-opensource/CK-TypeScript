using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Describes a mutable method parameter, property or variable: a name bound to a type that
    /// may be optional and may have a default value.
    /// </summary>
    public class TypeScriptVarType
    {
        /// <summary>
        /// Initializes a new TypeScript parameter, property or variable with its type.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="type">The parameter's type.</param>
        public TypeScriptVarType( string name, string type )
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the associated comment.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the parameter, property or variable name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether this is optional (a '?' should
        /// be added to the <see cref="Name"/> declaration).
        /// <para>
        /// Note that if <see cref="HasDefaultValue"/> is true, the parameter name
        /// can also be considered as optional (since the <see cref="DefaultValue"/> can
        /// be set), but this Optional is only bound to the nullability of the property.
        /// </para>
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Gets or sets parameter, property or variable type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets an optional default value.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Gets whether this <see cref="DefaultValue"/> is not null or empty.
        /// </summary>
        public bool HasDefaultValue => !String.IsNullOrEmpty( DefaultValue );

        /// <summary>
        /// Returns this declaration.
        /// </summary>
        /// <returns>A readable string.</returns>
        public override string ToString() => $"{Name}{(Optional?"?: " : ": ")}{Type}{(DefaultValue != null ? " = " + DefaultValue : string.Empty)}";

    }

}

