using CK.Core;
using System;

namespace CK.TypeScript;

/// <summary>
/// Extends the <see cref="RegisterPocoTypeAttribute"/> to also register an external
/// type as a TypeScript type.
/// </summary>
public sealed class RegisterTypeScriptType : RegisterPocoTypeAttribute
{
    public RegisterTypeScriptType( Type type )
        : base( type )
    {
    }
}
