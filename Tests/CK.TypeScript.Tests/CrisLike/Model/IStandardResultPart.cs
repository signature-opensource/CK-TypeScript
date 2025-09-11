using CK.Core;
using System.Collections.Generic;
using System.ComponentModel;

namespace CK.CrisLike;

/// <summary>
/// Defines a standard result part with a <see cref="Success"/> flag and <see cref="UserMessage"/>.
/// </summary>
[CKTypeDefiner]
public interface IStandardResultPart : IPoco
{
    /// <summary>
    /// Gets or sets whether the command succeeded or failed.
    /// Defaults to true.
    /// </summary>
    [DefaultValue( true )]
    bool Success { get; set; }

    /// <summary>
    /// Gets a mutable list of user messages.
    /// </summary>
    IList<UserMessage> UserMessages { get; }
}
