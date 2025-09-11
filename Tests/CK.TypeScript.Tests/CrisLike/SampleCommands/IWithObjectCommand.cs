using CK.CrisLike;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// This command requires authentication and is device dependent.
/// It returns an optional object as its result.
/// </summary>
[TypeScriptType( Folder = "Cmd/Some" )]
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
[TypeScriptType( Folder = "Cmd/Some" )]
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
