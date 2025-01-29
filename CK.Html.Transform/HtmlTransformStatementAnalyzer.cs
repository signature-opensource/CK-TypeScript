using CK.Transform.Core;

namespace CK.Html.Transform;

sealed class HtmlTransformStatementAnalyzer : TransformStatementAnalyzer, ILowLevelTokenizer
{
    readonly HtmlAnalyzer _htmlAnalyzer;

    internal HtmlTransformStatementAnalyzer( TransformLanguage language, HtmlAnalyzer tsAnalyzer )
        : base( language )
    {
        _htmlAnalyzer = tsAnalyzer;
    }

    public LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        // No specific tokens needed currently.
        return TransformLanguage.MinimalTransformerLowLevelTokenize( head );
    }

    protected override TransformStatement? ParseStatement( ref TokenizerHead head )
    {
        return base.ParseStatement( ref head );
    }

}
