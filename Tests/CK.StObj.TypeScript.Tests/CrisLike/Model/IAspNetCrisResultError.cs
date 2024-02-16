using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.CrisLike
{
    /// <summary>
    /// Simplified <see cref="ICrisResultError"/>: messages are <see cref="SimpleUserMessage"/>.
    /// </summary>
    [ExternalName( "AspNetCrisResultError" )]
    public interface IAspNetCrisResultError : IPoco
    {
        /// <summary>
        /// Gets or sets whether the command failed during validation or execution.
        /// </summary>
        bool IsValidationError { get; set; }

        /// <summary>
        /// Gets the list of user messages.
        /// At least one of them should be a <see cref="UserMessageLevel.Error"/> but this is not checked.
        /// </summary>
        IList<SimpleUserMessage> Messages { get; }

        /// <summary>
        /// Gets or sets a <see cref="ActivityMonitor.LogKey"/> that enables to locate the logs of the command execution.
        /// It may not always be available.
        /// </summary>
        string? LogKey { get; set; }
    }
}
