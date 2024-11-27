using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Parsing head for comment trivias. Micro parsers for <see cref="Trivia"/> are extension methods on this collector
/// by reference that call <see cref="Accept(TokenType, int)"/> or <see cref="Error(TokenType)"/>
/// and returns:
/// <list type="bullet">
///     <item>0 when no trivia starts at <see cref="Head"/>.</item>
///     <item>This <see cref="Result"/>:
///     <list type="bullet">
///         <item>That is positive on success (also returned by <see cref="Accept(TokenType, int)"/>.</item>
///         <item>A negative value that is the <see cref="TokenType.ErrorClassBit"/> combined with TokenType on error when the trivia is unterminated.</item>
///     </list>
///     </item>
/// </list>
/// Nothing prevents a micro parser to collect more than one consecutive trivia (but currently no implementation do this).
/// </summary>
public ref struct TriviaCollector
{
    ReadOnlySpan<char> _s;
    readonly ImmutableArray<Trivia>.Builder _collector;
    ReadOnlyMemory<char> _head;
    int _iHead;
    int _result;

    internal TriviaCollector( ReadOnlySpan<char> s, ReadOnlyMemory<char> head, ImmutableArray<Trivia>.Builder collector )
    {
        _s = s;
        _head = head;
        _collector = collector;
    }

    /// <summary>
    /// Accepts a trivia. This must not be called once <see cref="Error(TokenType)"/> has been called.
    /// </summary>
    /// <param name="tokenType">The type of trivia.</param>
    /// <param name="length">The trivia length. Must be positive.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    public int Accept( TokenType tokenType, int length )
    {
        Throw.CheckState( Result >= 0 );
        Throw.CheckArgument( tokenType.IsTrivia() );
        Throw.CheckArgument( length > 0 );
        _collector.Add( new Trivia( tokenType, _head.Slice( _iHead, length ) ) );
        _iHead += length;
        _s = _s.Slice( length );
        return _result += length;
    }

    /// <summary>
    /// Gets the accepted length (that should be the sum of the <see cref="Trivia.Content"/>'s length) on success
    /// or the error value.
    /// </summary>
    public int Result => _result;

    /// <summary>
    /// Signals an error by returning and setting the <see cref="Result"/> to the combination of the
    /// type of trivia with the <see cref="TokenType.ErrorClassBit"/>.
    /// <para>
    /// This must be called only once and once called, <see cref="Accept(TokenType, int)"/> cannot be called anymore.
    /// </para>
    /// </summary>
    /// <param name="tokenType">The type of trivia.</param>
    /// <returns>The error value.</returns>
    public int Error( TokenType tokenType )
    {
        Throw.CheckState( Result >= 0 );
        Throw.CheckArgument( tokenType.IsTrivia() );
        return _result = (int)(TokenType.ErrorClassBit | tokenType);
    }

    /// <summary>
    /// Gets the head to analyze.
    /// </summary>
    public readonly ReadOnlySpan<char> Head => _s;
}
