using CK.CrisLike;
using System.Collections.Generic;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// Concrete command where <see cref="ICommandAbsWithResult"/> works with <see cref="NamedRecord"/>.
/// </summary>
[TypeScriptType( SameFolderAs = typeof( ICommandAbs ) )]
public interface INamedRecordWithResultCommand : ICommand, ICommandAbsWithResult
{
    /// <summary>
    /// Gets the record.
    /// </summary>
    new ref NamedRecord Key { get; }

    /// <summary>
    /// Gets the mutable list of record.
    /// </summary>
    new IList<NamedRecord> KeyList { get; }

    /// <summary>
    /// Gets the mutable set of record.
    /// </summary>
    new ISet<NamedRecord> KeySet { get; }

    /// <summary>
    /// Gets the mutable dictionary of record.
    /// </summary>
    new IDictionary<string, NamedRecord> KeyDictionary { get; }

    /// <summary>
    /// Gets the mutable list of record.
    /// </summary>
    IList<NamedRecord?> KeyListOfNullable { get; }

}
