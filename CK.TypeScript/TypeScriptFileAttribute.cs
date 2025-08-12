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
    /// Initializes a new TypeScriptFileAttribute with a .ts file embedded as resources
    /// that must be copied in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
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
    /// <para>
    /// The resource must exist in the "Res/" or "Res[After]/" folder.
    /// </para>
    /// </summary>
    public string ResourcePath { get; }

    /// <summary>
    /// Gets the TypeScript type names that this file exports.
    /// </summary>
    public ImmutableArray<string> TypeNames { get; }

    /// <summary>
    /// Gets or sets a target path in the <c>ck-gen/</c> folder.
    /// <para>
    /// By default, when this is let to null, the resource file is copied to the <see cref="TypeScriptPackageAttribute.TypeScriptFolder"/>
    /// with its intermediate folders if any.
    /// </para>
    /// <para>
    /// When this is specified (not null or empty string), the resource file is copied to the specified path with
    /// its file name only, intermediate folders are ignored.
    /// In this case, the resource is logically moved to the &lt;App&gt; code container and hidden from its origin "Res/"
    /// or "Res[AFter]" resource container.
    /// </para>
    /// </summary>
    public string? TargetFolder { get; set; }
}
