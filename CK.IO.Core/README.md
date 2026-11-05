# CK.IO.Core

An exception is carried as data by a serializable representation rather than by the `Exception` object: this package
defines it, as an `IPoco`, along with the log levels as a flags enum. Both are in the `CK.IO.Core` namespace.

## ICKExceptionData

[`ICKExceptionData`](ICKExceptionData.cs) is an `IPoco` (see
[CK.Abstractions](https://github.com/signature-opensource/CK-Abstractions/blob/stable/CK.Abstractions/README.md)): an
immutable and serializable representation of an exception.

| Property | Content |
|---|---|
| `Message` | The message of the exception. Never null but can be empty. |
| `ExceptionTypeAssemblyQualifiedName` | The assembly qualified name of the exception type. |
| `ExceptionTypeName` | The name of the exception type. |
| `StackTrace` | The stack trace, or null. |
| `InnerException` | The inner exception, if any. For an `AggregateException`, the first of the `AggregatedExceptions`. |
| `AggregatedExceptions` | The `AggregateException.InnerExceptions` of an `AggregateException`, null for any other exception. |

## LogLevel

[`LogLevel`](LogLevel.cs) is a `[Flags]` enum. Its levels, in increasing order, are `Debug` (1), `Trace` (2), `Info`
(4), `Warn` (8), `Error` (16) and `Fatal` (32), with `None` (0). `Mask` (63) covers the levels, `IsFiltered` (64) marks
a log that went through the monitor's filters, and `NumberOfBits` is 7.

## Requires.

- [CK.Abstractions](https://github.com/signature-opensource/CK-Abstractions/blob/stable/CK.Abstractions/README.md) for
  `IPoco`.
