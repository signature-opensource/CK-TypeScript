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
via `TokenErrorNode` that carries an error token type (negative integer), an error message
and the location of the error.

However, nothing prevents exceptions to be (mis-)used and this is the spirit of this library: it tries
to not impose a single way to parse stuff, it aims to be a lego that can be used to implement multiple
kind of parsers.

## The basics: Tokens & Trivias

## The Analyzer abstraction

## Error handling: the hard way
The `TokenErrorNode` acts as an exception: when encountered, it should bubble up accross the parser
functions and becomes the top-level result.

This behavior is simple to implement and understand. This is eveything but "fault tolerant" because
the source code to transform is necessarily (or must be) syntaxically valid. This library is not 
a linter, interpreter or compiler, its sole purpose is to transform a valid fragment of code into
another valid fragment of code.




