using CK.Core;
using CK.CrisLike;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// A result object with an integer.
/// </summary>
[TypeScriptType( Folder = "Cmd/WithObject" )]
public interface IResult : IPoco
{
    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    int Result { get; set; }
}

/// <summary>
/// Secondary definition that adds a string to <see cref="IResult"/>.
/// </summary>
[TypeScriptType( Folder = "Cmd/WithObject" )]
public interface ISuperResult : IResult
{
    /// <summary>
    /// Gets or sets a string result.
    /// </summary>
    string SuperResult { get; set; }
}

/// <summary>
/// Secondary definition that makes <see cref="IWithObjectCommand"/> return a <see cref="IResult"/> and requires
/// the device identifier.
/// </summary>
[TypeScriptType( Folder = "Cmd/WithObject" )]
public interface IWithObjectSpecializedAsPocoCommand : IWithObjectCommand, ICommandAuthDeviceId, ICommand<IResult>
{
    /// <summary>
    /// Gets or sets the power of the Poco.
    /// </summary>
    int PowerPoco { get; set; }
}

/// <summary>
/// Secondary definition that makes <see cref="IWithObjectSpecializedAsPocoCommand"/> return a <see cref="ISuperResult"/>.
/// </summary>
[TypeScriptType( Folder = "Cmd/WithObject" )]
public interface IWithObjectSpecializedAsSuperPocoCommand : IWithObjectSpecializedAsPocoCommand, ICommand<ISuperResult>
{
}
