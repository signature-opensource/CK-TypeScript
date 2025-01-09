using CK.Transform.Core;
using CK.Transform.Core;

namespace CK.TypeScript.Transform;

public sealed class TypeScriptLanguage : TransformLanguage
{
    internal const string _languageName = "TypeScript";

    public TypeScriptLanguage()
        : base( _languageName )
    {
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new TypeScriptAnalyzer();
        var t = new TypeScriptTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
