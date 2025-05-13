using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Encapsulates a list of <see cref="TokenMatch"/>.
/// The only invalid filter is the <c>default</c> one and it never appears.
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

    public override bool Equals( [NotNullWhen( true )] object? obj ) => obj is TokenFilter f && Equals( f );

    public bool Equals( TokenFilter other ) => other._matches == _matches;

    public static bool operator ==( TokenFilter left, TokenFilter right ) => left.Equals( right );

    public static bool operator !=( TokenFilter left, TokenFilter right ) => !(left == right);

    internal TokenMatch[]? ArrayMatches => _matches;

    public override int GetHashCode() => _matches?.GetHashCode() ?? 0;
}
