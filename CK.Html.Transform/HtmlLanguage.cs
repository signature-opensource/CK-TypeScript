using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Accepts ".html" or ".htm" file name extension.
/// </summary>
public sealed class HtmlLanguage : TransformLanguage
{
    internal const string _languageName = "Html";

    /// <summary>
    /// Initializes a new HtmlLanguage instance.
    /// </summary>
    public HtmlLanguage()
        : base( _languageName, ".html", ".htm" )
    {
    }

    /// <inheritdoc/>
    protected override TransformLanguageAnalyzer CreateAnalyzer( TransformerHost.Language language )
    {
        return new HtmlTransformStatementAnalyzer( language, new HtmlAnalyzer() );
    }
}
