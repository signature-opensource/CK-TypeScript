using CK.Transform.Core;

namespace CK.Less.Transform;

/// <summary>
/// Accepts ".less" or ".css" file name extension.
/// </summary>
public sealed class LessLanguage : TransformLanguage
{
    internal const string _languageName = "Less";

    public LessLanguage()
        : base( _languageName, ".less", ".css" )
    {
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new LessAnalyzer();
        var t = new LessTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
