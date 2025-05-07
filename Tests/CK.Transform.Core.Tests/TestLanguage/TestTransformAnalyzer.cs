using System;

namespace CK.Transform.Core.Tests.Helpers;

sealed class TestTransformAnalyzer : TransformStatementAnalyzer
{
    public TestTransformAnalyzer( TransformLanguage language )
        : base( language )
    {
    }

    protected override object ParseSpanSpec( TransformerHost.Language language, RawString tokenSpec )
    {
        var singleSpanType = tokenSpec.InnerText.Span.Trim();
        if( singleSpanType.Length > 0 )
        {
            return singleSpanType switch
            {
                "braces" => new SingleSpanTypeFilter( typeof( BraceSpan ), "{braces}" ),
                "brackets" => new SingleSpanTypeFilter( typeof( BracketSpan ), "{brackets}" ),
                _ => $"""
                         Invalid span type '{singleSpanType}'. Allowed are "braces", "brackets".
                         """
            };
        }
        return IFilteredTokenEnumerableProvider.Empty;
    }
}
