using CK.Core;
using System;

namespace CK.TypeScript;

/// <summary>
/// Extends the <see cref="RegisterPocoTypeAttribute"/> to also register an external
/// type as a TypeScript type.
/// <para>
/// This attribute can only decorate a TypeScriptPackage: when set on any other object, this is simply ignored.
/// </para>
/// </summary>
public sealed class RegisterTypeScriptTypeAttribute : RegisterPocoTypeAttribute, ITypeScriptTypeDecorationAttribute
{
    TypeScriptTypeDecorationImpl _impl;

    public RegisterTypeScriptTypeAttribute( Type type )
        : base( type )
    {
    }

    /// <inheritdoc />
    public string? Folder
    {
        get => _impl.Folder;
        set => _impl.Folder = value;
    }

    /// <inheritdoc />
    public string? FileName
    {
        get => _impl.FileName;
        set => _impl.FileName = value;
    }

    /// <inheritdoc />
    public string? TypeName
    {
        get => _impl.TypeName;
        set => _impl.TypeName = value;
    }

    /// <inheritdoc />
    public Type? SameFolderAs
    {
        get => _impl.SameFolderAs;
        set => _impl.SameFolderAs = value;
    }

    /// <inheritdoc />
    public Type? SameFileAs
    {
        get => _impl.SameFileAs;
        set => _impl.SameFileAs = value;
    }

}
