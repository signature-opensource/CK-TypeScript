using CK.Setup;
using System;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript;

/// <summary>
/// Decorates a <see cref="TypeScriptPackage"/> to declare an embedded resource file
/// that will be generated in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
/// <para>
/// This is only useful on a TypeScriptPackage when <see cref="TypeScriptPackageAttribute.ConsiderExplicitResourceOnly"/>
/// is true.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class TypeScriptResourceAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new TypeScriptResourceAttribute with an embedded resource file that must be
    /// copied in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
    /// </summary>
    /// <param name="resourcePath">
    /// The embedded file name or path relative to the <see cref="TypeScriptPackage"/> folder.
    /// </param>
    public TypeScriptResourceAttribute( string resourcePath )
        : base( "CK.StObj.TypeScript.Engine.TypeScriptResourceAttributeImpl, CK.StObj.TypeScript.Engine" )
    {
        ResourcePath = resourcePath;
    }

    /// <summary>
    /// Gets the resource file path.
    /// </summary>
    public string ResourcePath { get; }

    /// <summary>
    /// Gets or sets a target path in <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/>.
    /// <para>
    /// By default, when this is let to null, the resource file is copied to the <see cref="TypeScriptPackageAttribute.TypeScriptFolder"/>.
    /// </para>
    /// </summary>
    public string? TargetFolderName { get; set; }
}
