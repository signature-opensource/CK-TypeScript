using CK.Core;
using CK.Setup;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Implementation class of the <see cref="TypeScriptAttribute"/>.
    /// This is not a <see cref="ITSCodeGeneratorType"/>: it just captures the
    /// optional configuration of TypeScript code generation (<see cref="TypeScriptAttribute.FileName"/>,
    /// <see cref="TypeScriptAttribute.Folder"/>, etc.).
    /// </summary>
    /// <remarks>
    /// This class can be specialized, typically to implement a ITSCodeGeneratorType.
    /// </remarks>
    public class TypeScriptAttributeImpl : ITSCodeGeneratorAutoDiscovery
    {
        /// <summary>
        /// Initializes a new <see cref="TypeScriptAttributeImpl"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        /// <param name="type">The decorated type.</param>
        public TypeScriptAttributeImpl( TypeScriptAttribute a, Type type )
        {
            Attribute = a;
            Type = type;
        }

        /// <summary>
        /// Gets the decorated type.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        public TypeScriptAttribute Attribute { get; }

    }
}
