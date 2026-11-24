Serializable data shared by the applications and their clients: the representation of an exception and the log levels.

[`ICKExceptionData`](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.IO.Core/ICKExceptionData.cs)
is the `IPoco` representation of an exception (message, type names, stack trace, inner and aggregated exceptions), and
[`LogLevel`](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.IO.Core/LogLevel.cs) is a flags enum
(`Debug`, `Trace`, `Info`, `Warn`, `Error`, `Fatal`, and the `IsFiltered` flag).

See the [README](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.IO.Core/README.md).
