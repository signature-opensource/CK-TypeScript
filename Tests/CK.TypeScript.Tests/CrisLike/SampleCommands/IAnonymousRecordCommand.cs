using CK.CrisLike;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// Concrete command where <see cref="ICommandAbs"/> works with an anonymous record.
/// </summary>
[TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
public interface IAnonymousRecordCommand : ICommand, ICommandAbs
{
    /// <summary>
    /// Gets an anonymous record key with field names. In TS it is {value: number, id: Guid}.
    /// </summary>
    new ref (int Value, Guid Id) Key { get; }

    /// <summary>
    /// Gets an anonymous record key without field names. In TS it is [number,Guid].
    /// </summary>
    ref (int, Guid) KeyTuple { get; }

    /// <summary>
    /// Gets an hybrid anonymous record key. In TS it is {item1: number, id: Guid}.
    /// </summary>
    ref (int, Guid Id) KeyHybrid { get; }

    /// <summary>
    /// Gets the mutable list of anonymous record.
    /// </summary>
    new IList<(int Value, Guid Id)> KeyList { get; }

    /// <summary>
    /// Gets the mutable set of anonymous record.
    /// </summary>
    new ISet<(int Value, Guid Id)> KeySet { get; }

    /// <summary>
    /// Gets the mutable dictionary of anonymous record.
    /// </summary>
    new IDictionary<string, (int Value, Guid Id)> KeyDictionary { get; }
}
