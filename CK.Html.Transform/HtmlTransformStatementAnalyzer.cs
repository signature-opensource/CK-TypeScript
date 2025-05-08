using CK.Transform.Core;
using System;

namespace CK.Html.Transform;

sealed class HtmlTransformStatementAnalyzer : LanguageTransformAnalyzer
{
    internal HtmlTransformStatementAnalyzer( TransformerHost.Language language, HtmlAnalyzer analyzer )
        : base( language, analyzer )
    {
    }

}
