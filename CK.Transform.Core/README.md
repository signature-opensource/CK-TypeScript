# CK.Transform.Core

A Parser is any `Func<ReadOnlyMemory<char>,AbstractNode>`. Having no interface or base class
that defines it enforces the fact that a Parser must be stateless.

Exceptions are not used for the control flow (they remain exceptional): errors are returned
thanks to the `TokenErrorNode` that carries an an error token type (negative integer), an error message
and the location of the error.

A parser can return a "true" error when it can determine for sure that a syntax error exists or the
special `TokenErrorNode.Unhandled` that states that the parser failed to recognize its language.
A generic multi-language parser can be 

