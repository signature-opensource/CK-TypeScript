namespace CK.Transform.Core;

/// <summary>
/// Captures "in ..." span where ... is <see cref="RangeLocation"/> (after/before/between ...) or
/// a <see cref="LocationMatcher"/>. 
/// </summary>
public sealed class InScope : SourceSpan
{
    InScope( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Checks that <see cref="RangeMatch"/> or <see cref="LocationMatch"/> is not null.
    /// </summary>
    public override bool CheckValid()
    {
        return base.CheckValid() && FirstChild is RangeLocation or LocationMatcher;
    }

    /// <summary>
    /// Gets the after/before/between ... range if this is not a <see cref="LocationMatch"/>.
    /// </summary>
    public RangeLocation? RangeMatch => FirstChild as RangeLocation;

    /// <summary>
    /// Gets the location matcher if this is not a <see cref="RangeMatch"/>.
    /// </summary>
    public LocationMatcher? LocationMatch => FirstChild as LocationMatcher;

    /// <summary>
    /// Gets the filtered token provider.
    /// <para>
    /// This is never null when <see cref="CheckValid"/> is true.
    /// </para>
    /// </summary>
    public ITokenFilterOperator? Operator => FirstChild as ITokenFilterOperator;


    internal static InScope? Match( TransformLanguageAnalyzer analyzer, ref TokenizerHead head, Token? inToken )
    {
        if( inToken == null && !head.TryAcceptToken( "in", out _ ) )
        {
            return null;
        }
        int begSpan = head.LastTokenIndex;
        RangeLocation? range = RangeLocation.Match( analyzer, ref head );
        if( range == null )
        {
            var matcher = LocationMatcher.Parse( analyzer, ref head );
            if( matcher == null ) return null;
        }
        return head.AddSpan( new InScope( begSpan, head.LastTokenIndex + 1 ) );
    }
}
