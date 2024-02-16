using CK.StObj.TypeScript;
using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// Captures the optional TypeScriptAttribute, type generators and/or IPocoType for a configured type.
    /// </summary>
    public readonly struct RegisteredType
    {
        internal RegisteredType( IReadOnlyList<ITSCodeGeneratorType>? generators, IPocoType? pocoType, TypeScriptAttribute? attr )
        {
            Generators = generators;
            PocoType = pocoType;
            Attribute = attr;
        }

        /// <summary>
        /// Gets the attribute that configures the TSType. This is null if the type:
        /// <list type="bullet">
        /// <item>Doesn't appear in the <see cref="TypeScriptAspectBinPathConfiguration.Types"/> configuration.</item>
        /// <item>And has only one or more attribute that implement <see cref="ITSCodeGeneratorType"/> type specific generators.</item>
        /// </list>
        /// </summary>
        public readonly TypeScriptAttribute? Attribute;

        /// <summary>
        /// Optional list of <see cref="ITSCodeGeneratorType"/> type specific generators.
        /// </summary>
        public readonly IReadOnlyList<ITSCodeGeneratorType>? Generators;

        /// <summary>
        /// Gets the IPocoType if the type is an exchangeable Poco type.
        /// </summary>
        public readonly IPocoType? PocoType;
    }
}
