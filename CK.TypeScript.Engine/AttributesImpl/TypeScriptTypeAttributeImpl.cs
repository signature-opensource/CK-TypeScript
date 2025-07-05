using CK.Setup;
using System;

namespace CK.TypeScript.Engine;

/// <summary>
/// Implementation class of the <see cref="TypeScriptTypeAttribute"/>.
/// This is not a <see cref="ITSCodeGeneratorType"/>: it just captures the
/// optional configuration of TypeScript code generation (<see cref="TypeScriptTypeAttribute.FileName"/>,
/// <see cref="TypeScriptTypeAttribute.Folder"/>, etc.).
/// </summary>
/// <remarks>
/// This class can be specialized, typically to implement a ITSCodeGeneratorType.
/// </remarks>
public class TypeScriptTypeAttributeImpl : ITSCodeGeneratorAutoDiscovery
{
    /// <summary>
    /// Initializes a new <see cref="TypeScriptTypeAttributeImpl"/>.
    /// </summary>
    /// <param name="a">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptTypeAttributeImpl( TypeScriptTypeAttribute a, Type type )
    {
        Attribute = a;
        Type = type;
    }

    /// <summary>
    /// Gets the decorated type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public TypeScriptTypeAttribute Attribute { get; }

}
