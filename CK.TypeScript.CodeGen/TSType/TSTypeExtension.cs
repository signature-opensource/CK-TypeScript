using CK.Core;
using System;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Extends <see cref="TSType"/>.
/// </summary>
public static class TSTypeExtension
{
    /// <summary>
    /// Calls <see cref="ITSType.TryWriteValue(ITSCodeWriter, object?)"/> and thows an <see cref="InvalidOperationException"/>
    /// if the <paramref name="value"/> cannot be written.
    /// </summary>
    /// <param name="type">This TypeScript type.</param>
    /// <param name="writer">The target writer.</param>
    /// <param name="value">The value to write.</param>
    public static void WriteValue( this ITSType type, ITSCodeWriter writer, object? value )
    {
        if( !type.TryWriteValue( writer, value ) )
        {
            Throw.InvalidOperationException( $"Type '{type.TypeName}' cannot write the value object '{value}'." );
        }
    }
}

