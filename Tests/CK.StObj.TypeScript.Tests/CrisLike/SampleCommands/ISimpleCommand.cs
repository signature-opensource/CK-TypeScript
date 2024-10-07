using CK.CrisLike;

namespace CK.StObj.TypeScript.Tests.CrisLike;

/// <summary>
/// Simple command.
/// </summary>
[TypeScript( Folder = "Cmd/Some" )]
public interface ISimpleCommand : ICommand
{
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    string? Action { get; set; }
}
