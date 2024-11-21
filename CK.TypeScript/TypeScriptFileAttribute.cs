using CK.Setup;
using System;
using System.Collections.Immutable;

namespace CK.TypeScript;

/// <summary>
/// Decorates a <see cref="TypeScriptPackage"/> to declare an embedded resource '.ts' file with
/// exported <see cref="TypeNames"/> that will be generated in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
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
        : base( "CK.TypeScript.Engine.TypeScriptFileAttributeImpl, CK.TypeScript.Engine" )
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
    /// Gets or sets a target path in <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/>.
    /// <para>
    /// By default, when this is let to null, the resource file is copied to the <see cref="TypeScriptPackageAttribute.TypeScriptFolder"/>.
    /// </para>
    /// </summary>
    public string? TargetFolder { get; set; }
}
