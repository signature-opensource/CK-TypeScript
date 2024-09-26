using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.StObj.TypeScript;

/// <summary>
/// Decorates a <see cref="TypeScriptPackage"/> to declare multiple files of any type (any extension)
/// that will be generated in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> folder.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class TypeScriptResourceFilesAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new TypeScriptResourceFilesAttribute.
    /// </summary>
    public TypeScriptResourceFilesAttribute()
        : base( "CK.StObj.TypeScript.Engine.TypeScriptResourceFilesAttributeImpl, CK.StObj.TypeScript.Engine" )
    {
    }

    /// <summary>
    /// Gets or sets a target path in <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> that overrides the default target path that
    /// is based on the decorated type namespace.
    /// <para>
    /// By default, when this is let to null, the resource files are copied to "/ck-gen/The/Decorated/Type/Namespace"
    /// (the dots of the namespace are replaced with a '/').
    /// </para>
    /// </summary>
    public string? TargetFolderName { get; set; }

}
