# CK.Transform.Core

The goal of this library is to provide a common basis to as many kind of parsers as possible in order
to factorize "transformer languages". Parsers can have very different implementations depending on
their purpose(s). Defining a common infrastructure is not a trivial task and is unfortunately
oriented towards some usage. Our primary usage is to ease transformer implementations for multiple
target languages, with at least a decent support for:
- T-Sql (Transact Sql Server).
- Html.
- Angular templates.
- TypeScript.
- Transformer language itself.

Exceptions should not be used for the control flow (they remain exceptional): errors should be emitted
via `TokenError` that carries an error token type (negative integer), an error message
and the location of the error.

## CR and CRLF handling
Only '\n' (Unix) and '\r\n' (Windows) are handled ('\r' alone should not be used!).
There is no normalization and normalization is useless.

1. Tokens (leading and trailing trivias) and RawString transparently handle both '\n' and '\r\n' end of line characters.
2. When a transformer injects a new line, it uses `Environment.NewLine`: the
result of a transformation can contain both end of line characters and can be safely transformed again (because of 1).

This design is the most efficient regardless of the platform: the inputs (the source and the code of the transfomers)
don't have to be normalized. If outputs need to be normalized (because other participants require normalized end of lines),
this is not the concern of this library.

## The basics: Tokens & Trivias

## The Analyzer abstraction

## Error handling: inlined errors
The collected tokens can contain `TokenError` that is a specialized `Token`.
This behavior is simple to implement and understand. This enables any analyzer to be
"fault tolearant" but analyze can also stop on the first error.





