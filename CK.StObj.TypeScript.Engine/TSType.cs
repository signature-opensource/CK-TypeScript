using CK.CodeGen;
using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Immutable representation of a mapping from a C# <see cref="NullableTypeTree"/> to a TypeScript <see cref="TypeName"/>
    /// to use. This covers basic types (available at the TypeScript language level) and generated types for which
    /// one or more <see cref="Imports"/> are needed.
    /// </summary>
    public sealed class TSType
    {
        /// <summary>
        /// Initializes a new <see cref="TSType"/> for a <see cref="NullableTypeTree"/> that must be "normally null".
        /// </summary>
        /// <param name="t">The type.</param>
        /// <param name="typeName">The TypeScript type name to use.</param>
        /// <param name="imports">Required imports if any.</param>
        public TSType( NullableTypeTree t, string typeName, IReadOnlyList<TSTypeFile> imports )
        {
            Throw.CheckArgument( "NullableTypeTree must be IsNormalNull.", t.IsNormalNull ); 
            Type = t;
            TypeName = typeName;
            Imports = imports;
        }

        /// <summary>
        /// Gets the <see cref="NullableTypeTree"/>.
        /// </summary>
        public NullableTypeTree Type { get; }

        /// <summary>
        /// Gets the TypeScript type name to use for this <see cref="Type"/>.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the TypeScript files that implement the <see cref="TypeName"/> if imports
        /// are required to use the <see cref="TypeName"/>.
        /// </summary>
        public IReadOnlyList<TSTypeFile> Imports { get; }

        /// <summary>
        /// Overridden to return this <see cref="TypeName"/>.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string ToString() => TypeName;
    }
}
