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
    [TypeScript( Folder = "" )]
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

    [TypeScript( Folder = "" )]
    public interface ISomeCommand : ICommandAuthNormal
    {
        Guid ActionId { get; set; }
    }

    [TypeScript( Folder = "" )]
    public interface ISomeCommandIsCriticalAndReturnsInt : ISomeCommand, ICommand<int>, ICommandAuthCritical
    {
    }

    [TypeScript( Folder = "" )]
    public interface IResult : IPoco
    {
        int Result { get; set; }
    }

    [TypeScript( Folder = "" )]
    public interface ISuperResult : IResult
    {
        string SuperResult { get; set; }
    }

    [TypeScript( Folder = "" )]
    public interface IWithObjectSpecializedAsPocoCommand : IWithObjectCommand, ICommandAuthDeviceId, ICommand<IResult>
    {
        int PowerPoco { get; set; }
    }

    [TypeScript( Folder = "" )]
    public interface IWithObjectSpecializedAsSuperPocoCommand : IWithObjectCommand, ICommand<ISuperResult>
    {
    }
}
