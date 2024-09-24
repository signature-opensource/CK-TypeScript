using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript;

/// <summary>
/// Decorates a <see cref="TypeScriptPackage"/> to declare multiple files
/// that will be generated in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class TypeScriptContentFilesAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new TypeScriptContentFilesAttribute.
    /// </summary>
    /// <param name="resourcePathPrefix">See <see cref="ResourcePathPrefix"/>.</param>
    public TypeScriptContentFilesAttribute( string resourcePathPrefix )
        : base( "CK.StObj.TypeScript.Engine.TypeScriptContentFilesAttributeImpl, CK.StObj.TypeScript.Engine" )
    {
        ResourcePathPrefix = resourcePathPrefix;
    }

    /// <summary>
    /// Gets or sets a required resource path prefix.
    /// </summary>
    public string ResourcePathPrefix { get; set; }

    /// <summary>
    /// Gets or sets a target path in <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> that overrides the default target path that uses
    /// the decorated type namespace.
    /// <para>
    /// By default, when this is let to null, the resource files are copied to "/ck-gen/The/Decorated/Type/Namespace"
    /// (the dots of the namespace are replaced with a '/').
    /// </para>
    /// </summary>
    public string? TargetFolderName { get; set; }

}
