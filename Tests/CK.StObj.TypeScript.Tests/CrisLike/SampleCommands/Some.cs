using CK.Core;
using CK.CrisLike;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    /// <summary>
    /// This command requires authentication and is device dependent.
    /// It returns an optional object as its result.
    /// </summary>
    [TypeScript( Folder = "Cmd/Some" )]
    public interface IWithObjectCommand : ICommandAuthDeviceId, ICommand<object?>
    {
        /// <summary>
        /// Gets the power of this command.
        /// </summary>
        int? Power { get; set; }
    }

    /// <summary>
    /// This command extends <see cref="IWithObjectCommand"/> to return a string (instead of object).
    /// </summary>
    [TypeScript( Folder = "Cmd/Some" )]
    public interface IWithObjectSpecializedAsStringCommand : IWithObjectCommand, ICommand<string>
    {
        /// <summary>
        /// Gets the power of the string.
        /// <para>
        /// The string has a great power!
        /// </para>
        /// </summary>
        int PowerString { get; set; }
    }

    /// <summary>
    /// Some command requires a regular authentication level.
    /// </summary>
    [TypeScript( Folder = "Cmd/Some" )]
    public interface ISomeCommand : ICommandAuthNormal
    {
        /// <summary>
        /// Gets or sets the action identifier.
        /// </summary>
        Guid ActionId { get; set; }
    }

    /// <summary>
    /// Specializes Some command to require a critical authentication level and return
    /// a integer.
    /// </summary>
    [TypeScript( Folder = "Cmd/Some" )]
    public interface ISomeCommandIsCriticalAndReturnsInt : ISomeCommand, ICommand<int>, ICommandAuthCritical
    {
    }
}
