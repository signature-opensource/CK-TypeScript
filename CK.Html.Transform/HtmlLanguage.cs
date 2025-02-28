using CK.Transform.Core;

namespace CK.Html.Transform;

public sealed class HtmlLanguage : TransformLanguage
{
    internal const string _languageName = "Html";

    public HtmlLanguage()
        : base( _languageName )
    {
    }

    /// <summary>
    /// Accepts ".html" or ".htm" file name extension.
    /// </summary>
    /// <param name="fileName">The file name or path to consider.</param>
    /// <returns>True if this is a html file name.</returns>
    public override bool IsLangageFilename( ReadOnlySpan<char> fileName )
    {
        return fileName.EndsWith( ".html", StringComparison.Ordinal )
               || fileName.EndsWith( ".htm", StringComparison.Ordinal );
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new HtmlAnalyzer();
        var t = new HtmlTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
