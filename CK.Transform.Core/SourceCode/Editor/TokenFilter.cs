using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Encapsulates a list of <see cref="TokenMatch"/>.
/// The only invalid filter is the <c>default</c> one.
/// <para>
/// Equality operator is the same as the <see cref="ImmutableArray{T}"/>: the inner array
/// must be the same instance.
/// </para>
/// </summary>
public readonly struct TokenFilter : IEquatable<TokenFilter>
{
    readonly TokenMatch[] _matches;

    internal TokenFilter( TokenMatch[] matches )
    {
        _matches = matches;
    }

    /// <summary>
    /// Gets whether this filter is not the <c>default</c> one.
    /// </summary>
    [MemberNotNullWhen( true, nameof( Matches ), nameof( ArrayMatches ) )]
    public bool IsValid => _matches != null;

    /// <summary>
    /// Gets the list of matches.
    /// Never null when <see cref="IsValid"/> is true.
    /// </summary>
    public IReadOnlyList<TokenMatch>? Matches => _matches;

    /// <summary>
    /// Gets the match at a specified token position.
    /// When no match contains the position, the <c>default</c> TokenMatch is returned (<see cref="TokenMatch.IsEmpty"/> is true).
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>The match that is <see cref="TokenMatch.IsEmpty"/> when not found.</returns>
    public TokenMatch FindMatchAt( int tokenIndex ) => _matches.FirstOrDefault( m => m.Span.Contains( tokenIndex ) );

    public override bool Equals( [NotNullWhen( true )] object? obj ) => obj is TokenFilter f && Equals( f );

    public bool Equals( TokenFilter other ) => other._matches == _matches;

    public static bool operator ==( TokenFilter left, TokenFilter right ) => left.Equals( right );

    public static bool operator !=( TokenFilter left, TokenFilter right ) => !(left == right);

    internal TokenMatch[]? ArrayMatches => _matches;

    public override int GetHashCode() => _matches?.GetHashCode() ?? 0;
}
