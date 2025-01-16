using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Html.Transform;

public sealed class HtmlLanguage : TransformLanguage
{
    internal const string _languageName = "TypeScript";

    public HtmlLanguage()
        : base( _languageName )
    {
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new HtmlAnalyzer();
        var t = new HtmlTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
