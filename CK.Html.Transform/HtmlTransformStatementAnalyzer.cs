using CK.Transform.Core;

namespace CK.Html.Transform;

sealed class HtmlTransformStatementAnalyzer : TransformLanguageAnalyzer
{
    internal HtmlTransformStatementAnalyzer( TransformerHost.Language language, HtmlAnalyzer analyzer )
        : base( language, analyzer )
    {
    }

}
