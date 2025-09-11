using CK.Core;
using CK.CrisLike;
using System.Collections.Generic;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// ICommandAbs is a part with an object key and
/// a list, a set and a dictionary of objects.
/// <para>
/// The properties here can only be "Abstract Read Only" properties, if they were
/// "concrete properties", they would impose the type.
/// </para>
/// </summary>
[TypeScriptType( Folder = "Cmd/Abstract" )]
public interface ICommandAbs : ICommandPart
{
    /// <summary>
    /// Gets an object key.
    /// </summary>
    object Key { get; }

    /// <summary>
    /// Gets a list of key as objects.
    /// </summary>
    IReadOnlyList<object> KeyList { get; }

    /// <summary>
    /// Gets a set of key as objects.
    /// </summary>
    IReadOnlySet<object> KeySet { get; }

    /// <summary>
    /// Gets a dictionary of key as objects.
    /// </summary>
    IReadOnlyDictionary<string, object> KeyDictionary { get; }
}

/// <summary>
/// <see cref="ICommandAbs"/> has a non nullable object key: this can
/// be used only with types that has a devault value. An Abstract IPoco has no
/// default value: to reference an Abstract IPoco, the property MUST be nullable.
/// <para>
/// This is not the case for the collections since non null objects can be added and
/// the empty collection is the default value.
/// </para>
/// </summary>
[TypeScriptType( SameFolderAs = typeof( ICommandAbs ) )]
public interface ICommandAbsWithNullableKey : ICommandPart
{
    /// <summary>
    /// Gets a nullable object key.
    /// </summary>
    object? Key { get; }

    /// <summary>
    /// Gets a list of key as objects.
    /// </summary>
    IReadOnlyList<object> KeyList { get; }

    /// <summary>
    /// Gets a set of key as objects.
    /// </summary>
    IReadOnlySet<object> KeySet { get; }

    /// <summary>
    /// Gets a dictionary of key as objects.
    /// </summary>
    IReadOnlyDictionary<string, object> KeyDictionary { get; }
}

/// <summary>
/// Specializes the <see cref="ICommandAbs"/> to return an object.
/// </summary>
[CKTypeDefiner]
[TypeScriptType( SameFolderAs = typeof( ICommandAbs ) )]
public interface ICommandAbsWithResult : ICommandAbs, ICommand<object>
{
}
