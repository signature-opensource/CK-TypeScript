using CK.CrisLike;
using System.Collections.Generic;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// Concrete command where <see cref="ICommandAbs"/> works with integers.
/// </summary>
[TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
public interface IIntCommand : ICommand, ICommandAbs
{
    /// <summary>
    /// Gets or sets the integer key.
    /// </summary>
    new int Key { get; set; }

    /// <summary>
    /// Gets the mutable list of integer.
    /// </summary>
    new IList<int> KeyList { get; }

    /// <summary>
    /// Gets the mutable set of integer.
    /// </summary>
    new ISet<int> KeySet { get; }

    /// <summary>
    /// Gets the mutable dictionary of integer.
    /// </summary>
    new IDictionary<string, int> KeyDictionary { get; }
}
