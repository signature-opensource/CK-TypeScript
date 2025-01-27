using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Less.Transform;

public sealed class LessLanguage : TransformLanguage
{
    internal const string _languageName = "Less";

    public LessLanguage()
        : base( _languageName )
    {
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new LessAnalyzer();
        var t = new LessTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
