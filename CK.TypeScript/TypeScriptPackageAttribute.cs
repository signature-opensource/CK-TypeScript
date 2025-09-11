using CK.Core;
using CK.Setup;
using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript;

/// <summary>
/// Required attribute for <see cref="TypeScriptPackage"/>.
/// <para>
/// A package is associated to embedded resources in "Res/" and/or "Res[After]/" folders.
/// and is often decorated with other attributes:
/// <list type="bullet">
///     <item>
///     <see cref="TypeScriptFileAttribute"/> can be use to specify one or more exported symbol names for a
///     TypeScript file and can be used to install it to another location than the package's <see cref="TypeScriptFolder"/>
///     by specifying a <see cref="TypeScriptFileAttribute.TargetFolder"/>.
///     </item>
///     <item>
///     <see cref="TypeScriptImportLibraryAttribute"/> enables a package to import an external npm library.
///     </item>
/// </list>
/// </para>
/// <para>
/// This attribute defines a root package that doesn't belong to any other package.
/// To specify a owning package, use the [Package&lt;T&gt;] attribute.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public class TypeScriptPackageAttribute : ContextBoundDelegationAttribute, IEmbeddedResourceTypeAttribute, IOptionalResourceGroupAttribute
{
    /// <summary>
    /// Initializes a new <see cref="TypeScriptPackageAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public TypeScriptPackageAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TypeScript.Engine.TypeScriptGroupOrPackageAttributeImpl, CK.TypeScript.Engine" )
    {
        CallerFilePath = callerFilePath;
    }

    /// <summary>
    /// Initializes a new specialized <see cref="TypeScriptPackageAttribute"/>.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
    /// <param name="finalCallerFilePath">Specialized types must provide the <c>[CallerFilePath]string? callerFilePath = null</c>.</param>
    protected TypeScriptPackageAttribute( string actualAttributeTypeAssemblyQualifiedName, string? finalCallerFilePath )
        : base( actualAttributeTypeAssemblyQualifiedName )
    {
        CallerFilePath = finalCallerFilePath;
    }

    /// <summary>
    /// Gets or sets the target TypeScript folder in the <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/>
    /// that overrides the default path that is based on the decorated type namespace: the folder is
    /// "/ck-gen/The/Decorated/Type/Namespace" (the dots of the namespace are replaced with a '/').
    /// <para>
    /// This should be let to null when possible: using the namespace ease maintenance.
    /// </para>
    /// </summary>
    public string? TypeScriptFolder { get; set; }

    /// <summary>
    /// Gets the folder path of the type that declares this attribute.
    /// See <see cref="CallerFilePathAttribute"/>.
    /// </summary>
    public string? CallerFilePath { get; }

    /// <summary>
    /// Gets or sets whether this package is optional.
    /// Defaults to false: a package that belongs to the set of registered types is required by default.
    /// <para>
    /// When this is true and no other packages has a <see cref="RequiresAttribute{T}"/>, <see cref="RequiredByAttribute{T}"/>,
    /// <see cref="GroupsAttribute{T}"/>, <see cref="ChildrenAttribute{T}"/> or <see cref="PackageAttribute{T}"/> that targets it,
    /// this package is not handled and won't appear in the final "ck-gen/" folder.
    /// </para>
    /// </summary>
    public bool IsOptional { get; set; }
}
