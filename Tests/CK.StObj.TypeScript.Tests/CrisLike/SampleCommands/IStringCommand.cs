using CK.CrisLike;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    /// <summary>
    /// Concrete command where <see cref="ICommandAbs"/> works with strings.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface IStringCommand : ICommand, ICommandAbs
    {
        /// <summary>
        /// Gets or sets the string key.
        /// </summary>
        new string Key { get; set; }

        /// <summary>
        /// Gets the mutable list of string.
        /// </summary>
        new IList<string> KeyList { get; }

        /// <summary>
        /// Gets the mutable set of integer.
        /// </summary>
        new ISet<string> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of integer.
        /// </summary>
        new IDictionary<string, string> KeyDictionary { get; }
    }


}
