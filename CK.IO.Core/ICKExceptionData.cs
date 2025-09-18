using CK.Core;
using System.Collections.Generic;

namespace CK.IO.Core;

/// <summary>
/// Immutable and serializable representation of an exception.
/// It contains specific data for some exceptions that, based on our experience, are actually interesting.
/// </summary>
public interface ICKExceptionData : IPoco
{
    /// <summary>
    /// Gets the message of the exception. Never null but can be empty.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets the assembly qualified exception type name. Never null nor empty.
    /// </summary>
    public string ExceptionTypeAssemblyQualifiedName { get; set; }

    /// <summary>
    /// Gets the exception type name. Never null nor empty.
    /// </summary>
    public string ExceptionTypeName { get; set; }

    /// <summary>
    /// Gets the stack trace. Can be null.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets the inner exception if it exists.
    /// If <see cref="AggregatedExceptions"/> is not null, it is the same as the first aggregated exceptions.
    /// </summary>
    public ICKExceptionData? InnerException { get; set; }

    /// <summary>
    /// Gets all the aggregated exceptions if the exception is a <see cref="System.AggregateException"/>.
    /// This corresponds to the <see cref="System.AggregateException.InnerExceptions"/> property.
    /// Null if this exception is not a an AggregatedException.
    /// </summary>
    public List<ICKExceptionData>? AggregatedExceptions { get; set; }
}
