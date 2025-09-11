using CK.CrisLike;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// Simple command.
/// </summary>
[TypeScriptType( Folder = "Cmd/Some" )]
public interface ISimpleCommand : ICommand
{
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    string? Action { get; set; }
}
