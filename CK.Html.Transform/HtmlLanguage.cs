using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Accepts ".html" or ".htm" file name extension.
/// </summary>
public sealed class HtmlLanguage : TransformLanguage
{
    internal const string _languageName = "Html";

    public HtmlLanguage()
        : base( _languageName, ".html", ".htm" )
    {
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new HtmlAnalyzer();
        var t = new HtmlTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
