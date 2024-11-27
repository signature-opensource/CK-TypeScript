using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml;

namespace CK.Transform.Core;

/// <summary>
/// Base class for tokens. A token is 1-<see cref="Width"/> node with a non zero or error <see cref="TokenType"/>.
/// <para>
/// To simplify implementation, a token always holds its <see cref="Text"/>, even for terminal tokens. This has
/// no real negative impact on the performance (as it is a <c>ReadOnlyMemory&lt;char&gt;</c> of the original text).
/// </para>
/// <para>
/// This class is not sealed. A language can introduced specialized token types with more behavior if needed.
/// When specialized <see cref="DoClone"/> MUST be overridden.
/// </para>
/// </summary>
public class TokenNode : AbstractNode, IEnumerable<TokenNode>
{
    readonly ReadOnlyMemory<char> _text;
    readonly TokenType _tokenType;

    /// <summary>
    /// Initializes a new <see cref="TokenNode"/>. <paramref name="tokenType"/> must be strictly positive (not an error) and not a trivia.
    /// </summary>
    /// <param name="tokenType">Type of the token.</param>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    public TokenNode( TokenType tokenType, ReadOnlyMemory<char> text, ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
        : base( leading, trailing )
    {
        Throw.CheckArgument( !tokenType.IsError() && !tokenType.IsTrivia()  );
        _tokenType = tokenType;
        _text = text;
    }

    public TokenNode( ref Analyzer.Head head, TokenType tokenType = TokenType.GenericText )
    {

    }

    // Constructor for error.
    private protected TokenNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, TokenType error, string message )
        : base( leading, trailing )
    {
        Throw.CheckArgument( error < 0 );
        _tokenType = error;
        _text = message.AsMemory();
    }

    /// <summary>
    /// The token type. It can be an error if this is a <see cref="TokenErrorNode"/>.
    /// </summary>
    public override sealed TokenType TokenType => _tokenType;

    /// <summary>
    /// Gets this token's <see cref="ISqlNode.LeadingTrivias"/>.
    /// </summary>
    public override sealed IEnumerable<Trivia> FullLeadingTrivias => LeadingTrivias;

    /// <summary>
    /// Gets this token's <see cref="ISqlNode.TrailingTrivias"/>.
    /// </summary>
    public override sealed IEnumerable<Trivia> FullTrailingTrivias => TrailingTrivias;

    /// <summary>
    /// Always empty since a token has no children.
    /// </summary>
    public override sealed IEnumerable<AbstractNode> LeadingNodes => Array.Empty<AbstractNode>();

    /// <summary>
    /// Always empty since a token has no children.
    /// </summary>
    public override sealed IEnumerable<AbstractNode> TrailingNodes => Array.Empty<AbstractNode>();

    /// <summary>
    /// Gets always 1: the width of a token.
    /// </summary>
    public override sealed int Width => 1;

    /// <summary>
    /// Gets the text.
    /// </summary>
    public ReadOnlyMemory<char> Text => _text;

    /// <summary>
    /// Tests token value equality: the reference equality still applies to tokens.
    /// This tests that the <see cref="Text"/> is the same.
    /// </summary>
    /// <param name="t">Token to compare to.</param>
    /// <returns>True if the this token has the same <see cref="Text"/> (ordinal comparison) as the other one.</returns>
    public bool TextEquals( TokenNode t ) => _text.Span.SequenceEqual( t._text.Span );

    /// <summary>
    /// Always empty since a token has no children.
    /// </summary>
    public override sealed IReadOnlyList<AbstractNode> ChildrenNodes => ImmutableArray<AbstractNode>.Empty;

    /// <summary>
    /// Gets a enumerable with only this token inside.
    /// </summary>
    public override sealed IEnumerable<TokenNode> AllTokens => this;

    /// <summary>
    /// Returns a basic <see cref="AbstractNodeMutator"/>. This cannot be specialized: a token has no children,
    /// there's nothing to mutate other than the trivias.
    /// </summary>
    /// <returns>A mutator.</returns>
    public override sealed AbstractNodeMutator CreateMutator() => new AbstractNodeMutator( this );

    /// <summary>
    /// Does nothing ath this level: the fact that the <see cref="Text"/> is necessarily non empty and the
    /// <see cref="TokenType"/> is not a trivia is checked by the constructor.
    /// </summary>
    protected override void DoCheckInvariants()
    {
    }

    IEnumerator<TokenNode> IEnumerable<TokenNode>.GetEnumerator() => new CKEnumeratorMono<TokenNode>( this );

    IEnumerator IEnumerable.GetEnumerator() => new CKEnumeratorMono<TokenNode>( this );

    /// <summary>
    /// Gets the <see cref="Text"/> as a string.
    /// This should be used in debug seesion only (this allocates a string).
    /// <para>
    /// This is sealed: the ToString() of a Token must always be its Text.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public override sealed string ToString() => _text.ToString();
}
