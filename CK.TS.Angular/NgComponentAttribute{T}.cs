using CK.StObj.TypeScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.TS.Angular;

/// <summary>
/// Required decoration of <see cref="NgComponent"/>.
/// <para>
/// A NgComponent is a <see cref="TypeScriptPackage"/> and belongs to a TypeScriptPackage.
/// </para>
/// </summary>
/// <typeparam name="T">The package to which this component belongs.</typeparam>
public class NgComponentAttribute<T> : NgComponentAttribute where T : TypeScriptPackage
{
    /// <summary>
    /// Initializes a new <see cref="NgComponentAttribute{T}"/>.
    /// </summary>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public NgComponentAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( callerFilePath )
    {
    }

    /// <summary>
    /// Initializes a new specialized <see cref="NgComponentAttribute{T}"/>.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
    /// <param name="finalCallerFilePath">Specialized types must provide the <c>[CallerFilePath]string? callerFilePath = null</c>.</param>
    protected NgComponentAttribute( string actualAttributeTypeAssemblyQualifiedName, string? finalCallerFilePath )
        : base( actualAttributeTypeAssemblyQualifiedName, finalCallerFilePath )
    {
    }

    /// <inheritdoc />
    public override Type Package => typeof( T );

}
