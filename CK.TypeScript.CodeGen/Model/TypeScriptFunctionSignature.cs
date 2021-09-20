using System.Collections.Generic;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Describes a mutable method signature.
    /// </summary>
    public class TypeScriptFunctionSignature
    {
        /// <summary>
        /// Gets or sets the associated optional comment.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the function's name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets whether a <see cref="Name"/> exists for this function.
        /// </summary>
        public bool IsAnonymous => string.IsNullOrEmpty( Name );

        /// <summary>
        /// Gets the mutable list of the parameters.
        /// </summary>
        public IList<TypeScriptVarType> Parameters = new List<TypeScriptVarType>();

        /// <summary>
        /// Gets or sets the optional return type.
        /// </summary>
        public string? ReturnType { get; set; }

    }

}

