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

## The basics: Tokens & Trivias

## The Analyzer abstraction

## Error handling: inlined errors
The collected tokens can contain `TokenError` that is a specialized `Token`.
This behavior is simple to implement and understand. This enables any analyzer to be
"fault tolearant" but analyze can also be stopped on the first error.





