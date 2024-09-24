using CK.CrisLike;

namespace CK.StObj.TypeScript.Tests.CrisLike;

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
/// Secondary definition that makes <see cref="IWithObjectCommand"/> return a string (instead of object).
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
