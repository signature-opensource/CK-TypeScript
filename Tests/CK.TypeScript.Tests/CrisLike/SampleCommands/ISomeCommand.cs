using CK.CrisLike;
using System;

namespace CK.TypeScript.Tests.CrisLike;

/// <summary>
/// Some command requires a regular authentication level.
/// </summary>
[TypeScriptType( Folder = "Cmd/Some" )]
public interface ISomeCommand : ICommand, ICommandAuthNormal
{
    /// <summary>
    /// Gets or sets the action identifier.
    /// </summary>
    Guid ActionId { get; set; }
}

/// <summary>
/// Secondary definition that makes SomeCommand require a critical authentication level and return
/// a integer.
/// </summary>
[TypeScriptType( Folder = "Cmd/Some" )]
public interface ISomeIsCriticalAndReturnsIntCommand : ISomeCommand, ICommand<int>, ICommandAuthCritical
{
}
