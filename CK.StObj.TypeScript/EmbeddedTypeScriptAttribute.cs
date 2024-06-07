using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Decorates a class (that can be static) to support embedded resource files that will
    /// be generated in the /ck-gen/src folder.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public sealed class EmbeddedTypeScriptAttribute : ContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new EmbeddedTypeScriptAttribute with one or more TypeScript file
        /// embedded as resources that must be copied in the /ck-gen/src folder.
        /// </summary>
        /// <param name="resourceName">One or more files names. The file extension </param>
        public EmbeddedTypeScriptAttribute( params string[] resourceName )
            : base( "CK.StObj.TypeScript.Engine.EmbeddedTypeScriptAttributeImpl, CK.StObj.TypeScript.Engine" )
        {
            ResourceNames = resourceName.ToImmutableArray();
        }

        /// <summary>
        /// Gets the resource names to load.
        /// </summary>
        public ImmutableArray<string> ResourceNames { get; }

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
        /// Gets or sets a path that overrdes the default path that uses the decorated type namespace.
        /// <para>
        /// By default, when this is let to null, the resource files are copied to "/ck-gen/The/Decorated/Type/Namespace"
        /// (the dots of the namespace are replaced with a '/').
        /// </para>
        /// </summary>
        public string? TargetFolderName { get; set; }
    }

}
