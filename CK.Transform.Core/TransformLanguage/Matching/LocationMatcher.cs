using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Captures "<see cref="LocationCardinality"/> <see cref="Matcher"/>".
/// </summary>
public sealed class LocationMatcher : SourceSpan, ITokenFilter
{
    LocationMatcher( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Checks that <see cref="Matcher"/> is valid.
    /// </summary>
    /// <returns>True if this span is valid.</returns>
    [MemberNotNullWhen( true, nameof( Matcher ) )]
    public override bool CheckValid()
    {
        return base.CheckValid() && Matcher != null;
    }

    /// <summary>
    /// Gets the optional match cardinality.
    /// When null, the match must be considered "single".
    /// </summary>
    public LocationCardinality? Cardinality => Children.FirstChild as LocationCardinality;

    /// <summary>
    /// Gets the span matcher.
    /// Never null when <see cref="CheckValid()"/> is true.
    /// </summary>
    public SpanMatcher? Matcher
    {
        get
        {
            var c = Children.FirstChild;
            return c as SpanMatcher ?? c?.NextSibling as SpanMatcher;
        }
    }

    internal static LocationMatcher? Parse( ref TokenizerHead head, bool monoLocationOnly = false )
    {
        int begSpan = head.LastTokenIndex + 1;
        var cardinality = LocationCardinality.Match( ref head, monoLocationOnly );
        var matcher = SpanMatcher.Match( ref head );
        return matcher == null
                ? null
                : head.AddSpan( new LocationMatcher( begSpan, head.LastTokenIndex + 1 ) );
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>? ITokenFilter.GetScopedTokens( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        Throw.DebugAssert( CheckValid() );
        var inner = Matcher.GetScopedTokens( monitor, editor );
        if( inner == null ) return null;
        if( Cardinality == null || Cardinality.Kind == LocationCardinality.LocationKind.Single )
        {
            var single = inner.SelectMany( Util.FuncIdentity ).SingleOrDefault();
            if( single != null ) return [[single]];
            int count = inner.SelectMany( Util.FuncIdentity ).Count();
            monitor.Error( $"Expected single '{Matcher}' but got {count}." );
            return null;
        }
        if( Cardinality.Kind == LocationCardinality.LocationKind.First )
        {
            var first = inner.SelectMany( Util.FuncIdentity ).Skip( Cardinality.Offset - 1 ).FirstOrDefault();
            if( first != null ) return [[first]];
            int count = inner.SelectMany( Util.FuncIdentity ).Count();
            monitor.Error( $"Expected {Cardinality} '{Matcher}' but got only {count} matches." );
        }
    }
}
