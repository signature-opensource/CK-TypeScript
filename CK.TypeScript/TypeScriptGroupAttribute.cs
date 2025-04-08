using CK.Core;
using CK.Setup;
using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript;

/// <summary>
/// Required attribute for <see cref="TypeScriptGroup"/>.
/// <para>
/// A package is associated to embedded resources in "Res/" and/or "Res[After]/" folders.
/// and is often decorated with other attributes:
/// <list type="bullet">
///     <item>
///     <see cref="TypeScriptFileAttribute"/> can be use to specify one or more exported symbol names for a
///     TypeScript file and can be used to install it to another location than the group's <see cref="TypeScriptFolder"/>
///     by specifying a <see cref="TypeScriptFileAttribute.TargetFolder"/>.
///     </item>
///     <item>
///     <see cref="TypeScriptImportLibraryAttribute"/> enables a group to import an external npm library.
///     </item>
/// </list>
/// </para>
/// <para>
/// This attribute defines an independent group.
/// A owning package can be specified thanks to the [Package&lt;T&gt;] attribute. [Groups&lt;T1,...&gt;] attribute
/// can specify the groups that contain this group.
/// </para>
/// <para>
/// The [Children&lt;T1,...&gt;] attribute can be used to define the groups or packages contained in this group.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public class TypeScriptGroupAttribute : ContextBoundDelegationAttribute, IEmbeddedResourceTypeAttribute
{
    /// <summary>
    /// Initializes a new <see cref="TypeScriptGroupAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public TypeScriptGroupAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TypeScript.Engine.TypeScriptGroupOrPackageAttributeImpl, CK.TypeScript.Engine" )
    {
        CallerFilePath = callerFilePath;
    }

    /// <summary>
    /// Initializes a new specialized <see cref="TypeScriptGroupAttribute"/>.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
    /// <param name="finalCallerFilePath">Specialized types must provide the <c>[CallerFilePath]string? callerFilePath = null</c>.</param>
    protected TypeScriptGroupAttribute( string actualAttributeTypeAssemblyQualifiedName, string? finalCallerFilePath )
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
}
