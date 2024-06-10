using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Decorates any class (that can be static) to declare an embedded resource files
    /// that will be generated in the /ck-gen/src folder.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public sealed class TypeScriptFileAttribute : ContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new TypeScriptFileAttribute with a .ts file
        /// embedded as resources that must be copied in the /ck-gen/src folder.
        /// </summary>
        /// <param name="resourceName">The embedded file name. The file extension must be ".ts" otherwise a setup error will occur.</param>
        /// <param name="typeName">Declares 0 or more TypeScript type names that are exported by this file.</param>
        public TypeScriptFileAttribute( string resourceName, params string[] typeName )
            : base( "CK.StObj.TypeScript.Engine.TypeScriptFileAttributeImpl, CK.StObj.TypeScript.Engine" )
        {
            ResourceName = resourceName;
            TypeNames = typeName.ToImmutableArray();
        }

        /// <summary>
        /// Gets the resource name to load.
        /// </summary>
        public string ResourceName { get; }

        /// <summary>
        /// Gets the TypeScript type names that this file exports.
        /// </summary>
        public ImmutableArray<string> TypeNames { get; }

        /// <summary>
        /// Gets or sets a resource path that overrides the default "Res" path suffix.
        /// <para>
        /// To take full control of path (ignoring the "Decorated.Type.Namespace" prefix,
        /// sets a path that starts with "~".
        /// </para>
        /// <para>
        /// By default, when this is let to null, the resource files are looked up with the
        /// decorated "<see cref="Type.Namespace"/>.Res" prefix.
        /// </para>
        /// </summary>
        public string? ResourcePath { get; set; }

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
