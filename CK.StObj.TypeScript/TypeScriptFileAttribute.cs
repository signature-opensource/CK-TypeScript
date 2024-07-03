using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Decorates a <see cref="TypeScriptPackage"/> to declare an embedded resource files
    /// that will be generated in the /ck-gen/src folder.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public sealed class TypeScriptFileAttribute : ContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new TypeScriptFileAttribute with a .ts file
        /// embedded as resources that must be copied in the /ck-gen/src folder.
        /// </summary>
        /// <param name="resourcePath">
        /// The embedded file path (typically including "Res/" folder).
        /// The file extension must be ".ts" otherwise a setup error will occur.
        /// </param>
        /// <param name="typeName">Declares 0 or more TypeScript type names that are exported by this file.</param>
        public TypeScriptFileAttribute( string resourcePath, params string[] typeName )
            : base( "CK.StObj.TypeScript.Engine.TypeScriptFileAttributeImpl, CK.StObj.TypeScript.Engine" )
        {
            ResourcePath = resourcePath;
            TypeNames = typeName.ToImmutableArray();
        }

        /// <summary>
        /// Gets the resource file path.
        /// </summary>
        public string ResourcePath { get; }

        /// <summary>
        /// Gets the TypeScript type names that this file exports.
        /// </summary>
        public ImmutableArray<string> TypeNames { get; }

        /// <summary>
        /// Gets or sets a target path in /ck-gen/src that overrides the default path that uses
        /// the decorated type namespace.
        /// <para>
        /// By default, when this is let to null, the resource files are copied to "/ck-gen/src/The/Decorated/Type/Namespace"
        /// (the dots of the namespace are replaced with a '/').
        /// </para>
        /// </summary>
        public string? TargetFolderName { get; set; }
    }

}
