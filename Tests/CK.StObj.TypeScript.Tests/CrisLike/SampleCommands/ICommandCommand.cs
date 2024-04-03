using CK.Core;
using CK.CrisLike;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    /// <summary>
    /// Concrete command where <see cref="ICommandAbsWithNullableKey"/> works with any command.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface ICommandCommand : ICommand, ICommandAbsWithNullableKey
    {
        /// <summary>
        /// Gets or sets the command key.
        /// Here, the command must be nullable otherwise we would be
        /// unable to have a default value for it. 
        /// </summary>
        new ICommand? Key { get; set; }

        /// <summary>
        /// Gets the mutable list of command.
        /// </summary>
        new IList<ICommand> KeyList { get; }

        /// <summary>
        /// The mutable set of command is not possible: a set must have read-only compliant key and a poco is
        /// everything but read-only compliant.
        /// </summary>
        new ISet<ExtendedCultureInfo> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of command.
        /// </summary>
        new IDictionary<string, ICommand> KeyDictionary { get; }
    }


}
