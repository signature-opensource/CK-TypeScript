using CK.Core;
using CK.CrisLike;

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    /// <summary>
    /// A result object with an integer.
    /// </summary>
    [TypeScript( Folder = "Cmd/WithObject" )]
    public interface IResult : IPoco
    {
        /// <summary>
        /// Gets or sets the result value.
        /// </summary>
        int Result { get; set; }
    }

    /// <summary>
    /// Specializes <see cref="IResult"/> to add a string.
    /// </summary>
    [TypeScript( Folder = "Cmd/WithObject" )]
    public interface ISuperResult : IResult
    {
        /// <summary>
        /// Gets or sets a string result.
        /// </summary>
        string SuperResult { get; set; }
    }

    /// <summary>
    /// Specializes <see cref="IWithObjectCommand"/> to return a <see cref="IResult"/>.
    /// </summary>
    [TypeScript( Folder = "Cmd/WithObject" )]
    public interface IWithObjectSpecializedAsPocoCommand : IWithObjectCommand, ICommandAuthDeviceId, ICommand<IResult>
    {
        /// <summary>
        /// Gets or sets the power of the Poco.
        /// </summary>
        int PowerPoco { get; set; }
    }

    /// <summary>
    /// Specializes <see cref="IWithObjectSpecializedAsPocoCommand"/> to return a <see cref="ISuperResult"/>.
    /// </summary>
    [TypeScript( Folder = "Cmd/WithObject" )]
    public interface IWithObjectSpecializedAsSuperPocoCommand : IWithObjectSpecializedAsPocoCommand, ICommand<ISuperResult>
    {
    }

}
