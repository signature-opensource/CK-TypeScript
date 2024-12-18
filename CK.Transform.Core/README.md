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

A Parser cound be any `Func<ReadOnlyMemory<char>,AbstractNode?>`. Having no interface or base class
that defines it enforces the fact that a Parser must be stateless... However we introduce a stateful
object, the `Analyzer` that can support reusable builders and offer contextual information if needed.

Exceptions are not used for the control flow (they remain exceptional): errors are returned
thanks to the `TokenErrorNode` that carries an an error token type (negative integer), an error message
and the location of the error.

A parser can return a "true" error when it can determine for sure that a syntax error exists or a
null `AbstractNode?` that states that the parser failed to recognize its language.

## Error handling: the hard way
The `TokenErrorNode` acts as an exception: when encountered, it should bubble up accross the parser
functions and becomes the top-level result.

This behavior is simple to implement and understand. This is eveything but "fault tolerant" because
the source code to transform is necessarily (or must be) syntaxically valid. This library is not 
a linter, interpreter or compiler, its sole purpose is to transform a valid fragment of code into
another valid fragment of code.




