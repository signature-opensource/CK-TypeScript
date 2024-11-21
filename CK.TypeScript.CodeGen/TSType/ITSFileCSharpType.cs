using System;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Defines a C# type that is locally implemented in a file.
/// </summary>
public interface ITSFileCSharpType : ITSFileType
{
    /// <summary>
    /// Gets the C# type.
    /// <para>
    /// Note that nullability is not enforced for value types: when this <see cref="ITSType.IsNullable"/>
    /// is true, this is always the non nullable type, even for value type.
    /// </para>
    /// </summary>
    Type Type { get; }
}

