using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript;

/// <summary>
/// Decorates a <see cref="TypeScriptPackage"/> to declare an embedded resource TypeScript file
/// that will be generated in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class TypeScriptFileAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new TypeScriptFileAttribute with a .ts file
    /// embedded as resources that must be copied in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
    /// </summary>
    /// <param name="resourcePath">
    /// The embedded file name or path relative to the <see cref="TypeScriptPackage"/> folder.
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
    /// Gets or sets a target path in <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> that overrides the default path that is
    /// based on the decorated type namespace.
    /// <para>
    /// By default, when this is let to null, the resource file is copied to "/ck-gen/The/Decorated/Type/Namespace"
    /// (the dots of the namespace are replaced with a '/').
    /// </para>
    /// </summary>
    public string? TargetFolderName { get; set; }
}
