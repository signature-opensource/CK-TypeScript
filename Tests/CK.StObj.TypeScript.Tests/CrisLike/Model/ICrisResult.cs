using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.CrisLike
{
    /// <summary>
    /// Describes the final result of a command.
    /// </summary>
    [ExternalName( "CrisResult" )]
    public interface ICrisResult : IPoco
    {
        /// <summary>
        /// Gets or sets the error or result object (if any).
        /// <list type="bullet">
        ///   <item>
        ///     This is null when the command has been processed synchronously, successfully, and doesn't expect any result
        ///     (the command is not a <see cref="ICommand{TResult}"/>).
        ///   </item>
        ///   <item>
        ///     This is also always null if the command is a <see cref="ICommand"/> (without result).
        ///   </item>
        ///   <item>
        ///     On error this should contain a description of
        ///     the error, typically a <see cref="ICrisResultError"/>, a simple string, a value tuple, or any combination of
        ///     types that are easily serializable.
        ///   </item>
        /// </list>
        /// </summary>
        object? Result { get; set; }

        /// <summary>
        /// Gets or sets an optional identifier that identifies the handling of the command.
        /// <para>
        /// This identifier must of course be unique.
        /// </para>
        /// </summary>
        string? CorrelationId { get; set; }


    }
}
