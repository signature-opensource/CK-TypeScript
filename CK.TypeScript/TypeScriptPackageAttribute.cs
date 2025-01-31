using CK.Core;
using CK.Setup;
using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript;

/// <summary>
/// Required attribute for <see cref="TypeScriptPackage"/>.
/// <para>
/// A package is associated to embedded resources from <see cref="ResourceFolderPath"/> ("./Res" by default)
/// and is often decorated with other attributes:
/// <list type="bullet">
///     <item>
///     <see cref="TypeScriptResourceAttribute"/> enables a file from the <see cref="ResourceFolderPath"/> to
///     be extracted to another location than the package's <see cref="TypeScriptFolder"/> by specifying a
///     <see cref="TypeScriptResourceAttribute.TargetFolder"/>.
///     </item>
///     <item>
///     <see cref="TypeScriptFileAttribute"/> is like <see cref="TypeScriptResourceAttribute"/> but in addition
///     can declare exported type names from the file.
///     </item>
///     <item>
///     <see cref="TypeScriptImportLibraryAttribute"/> enables a package to import an external npm library.
///     </item>
/// </list>
/// </para>
/// <para>
/// This attribute defines a root package that doesn't belong to any other package. The <see cref="TypeScriptPackageAttribute{T}"/>
/// must be used to define a subordinated package.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public class TypeScriptPackageAttribute : ContextBoundDelegationAttribute, IEmbeddedResourceTypeAttribute
{
    /// <summary>
    /// Initializes a new <see cref="TypeScriptPackageAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public TypeScriptPackageAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TypeScript.Engine.TypeScriptPackageAttributeImpl, CK.TypeScript.Engine" )
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
    /// Gets the package to which this package belongs.
    /// </summary>
    public virtual Type? Package => null;

    /// <summary>
    /// Gets or sets the folder's path where embedded resources for this package should be loaded from.
    /// <para>
    /// When let to null, an automatic resolution is done that defaults to the "./Res" folder
    /// where the "." is the folder of the type that declares this attribute.
    /// </para>
    /// </summary>
    public string? ResourceFolderPath { get; set; }

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
    /// Gets or sets whether embedded resources must be explicitly declared by <see cref="TypeScriptFileAttribute"/>
    /// or <see cref="TypeScriptResourceAttribute"/>.
    /// <para>
    /// Defaults to false: by default all files in <see cref="ResourceFolderPath"/> are considered. 
    /// </para>
    /// </summary>
    public bool ConsiderExplicitResourceOnly { get; set; }

    /// <summary>
    /// Gets the folder path of the type that declares this attribute.
    /// See <see cref="CallerFilePathAttribute"/>.
    /// </summary>
    public string? CallerFilePath { get; }
}
