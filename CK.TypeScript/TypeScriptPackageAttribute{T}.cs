using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript;

/// <summary>
/// Specialized <see cref="TypeScriptPackageAttribute{T}"/> that belongs to another package.
/// </summary>
/// <typeparam name="T">The package that contains this package.</typeparam>
public class TypeScriptPackageAttribute<T> : TypeScriptPackageAttribute where T : TypeScriptPackage
{
    /// <summary>
    /// Initializes a new <see cref="TypeScriptPackageAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public TypeScriptPackageAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( callerFilePath )
    {
    }

    /// <summary>
    /// Initializes a new specialized <see cref="TypeScriptPackageAttribute"/>.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
    /// <param name="finalCallerFilePath">Specialized types must provide the <c>[CallerFilePath]string? callerFilePath = null</c>.</param>
    protected TypeScriptPackageAttribute( string actualAttributeTypeAssemblyQualifiedName, string? finalCallerFilePath )
        : base( actualAttributeTypeAssemblyQualifiedName, finalCallerFilePath )
    {
    }

    /// <summary>
    /// Gets the package to which this package belongs.
    /// </summary>
    public override Type? Package => typeof( T );
}
