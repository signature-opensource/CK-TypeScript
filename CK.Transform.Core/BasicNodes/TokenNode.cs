using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Base class for tokens. A token is 1-<see cref="Width"/> node with a non zero or error <see cref="NodeType"/>.
/// <para>
/// To simplify implementation, a token always holds its <see cref="Text"/>, even for terminal tokens. This has
/// no real negative impact on the performance (as it is a <c>ReadOnlyMemory&lt;char&gt;</c> of the original text).
/// </para>
/// <para>
/// This class is not sealed. A language can introduce specialized token types with more behavior if needed.
/// </para>
/// </summary>
public class TokenNode : AbstractNode, IEnumerable<TokenNode>
{
    readonly ReadOnlyMemory<char> _text;
    readonly NodeType _tokenType;

    /// <summary>
    /// Initializes a new <see cref="TokenNode"/>. <paramref name="tokenType"/> must be strictly positive (not an error) and not a trivia
    /// and <paramref name="text"/> cannot be empty. 
    /// </summary>
    /// <param name="tokenType">Type of the token.</param>
    /// <param name="text">The token text. Cannot be empty.</param>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    public TokenNode( NodeType tokenType, ReadOnlyMemory<char> text, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default )
        : base( leading.IsDefault ? [] : leading, trailing.IsDefault ? [] : trailing )
    {
        Throw.CheckArgument( !tokenType.IsError() && !tokenType.IsTrivia() );
        Throw.CheckArgument( text.Length > 0 );
        _tokenType = tokenType;
        _text = text;
    }

    /// <summary>
    /// Special internal constructor for error and special tokens: <paramref name="tokenType"/> is not checked, <paramref name="text"/> can be empty.
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <param name="tokenType">Unchecked token type.</param>
    /// <param name="text">May be empty or not from the source text.</param>
    internal TokenNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, NodeType tokenType, ReadOnlyMemory<char> text )
        : base( leading, trailing )
    {
        _tokenType = tokenType;
        _text = text;
    }

    /// <summary>
    /// Creates a marker token. <see cref="Text"/> is empty and there is no trivias.
    /// <para>
    /// The <paramref name="type"/> is free to be any value, including error (negative value).
    /// </para>
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <returns>A token.</returns>
    public static TokenNode CreateMarker( NodeType type = NodeType.GenericMarkerToken ) => new TokenNode( ImmutableArray<Trivia>.Empty, ImmutableArray<Trivia>.Empty, type, default );

    /// <summary>
    /// The token type. It can be an error if this is a <see cref="TokenErrorNode"/>.
    /// </summary>
    public override sealed NodeType NodeType => _tokenType;

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
    /// <see cref="NodeType"/> is not a trivia is checked by the constructor.
    /// </summary>
    protected override void DoCheckInvariants()
    {
    }

    IEnumerator<TokenNode> IEnumerable<TokenNode>.GetEnumerator() => new CKEnumeratorMono<TokenNode>( this );

    IEnumerator IEnumerable.GetEnumerator() => new CKEnumeratorMono<TokenNode>( this );

    /// <summary>
    /// Overridden to append the <see cref="Text"/>.
    /// </summary>
    /// <param name="b">The target builder.</param>
    protected override void WriteWithoutTrivias( StringBuilder b )
    {
        b.Append( _text );
    }

    /// <summary>
    /// Gets the <see cref="Text"/> as a string.
    /// This should be used in debug session only (this allocates a string).
    /// <para>
    /// This is sealed: the ToString() of a Token must always be its Text.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public override sealed string ToString() => _text.ToString();
}
