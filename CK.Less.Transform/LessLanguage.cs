using CK.Transform.Core;

namespace CK.Less.Transform;

public sealed class LessLanguage : TransformLanguage
{
    internal const string _languageName = "Less";

    public LessLanguage()
        : base( _languageName )
    {
    }

    /// <summary>
    /// Accepts ".less" or ".css" file name extension.
    /// </summary>
    /// <param name="fileName">The file name or path to consider.</param>
    /// <returns>True if this is a Less or Css file name.</returns>
    public override bool IsLangageFilename( ReadOnlySpan<char> fileName )
    {
        return fileName.EndsWith( ".less", StringComparison.Ordinal )
               || fileName.EndsWith( ".css", StringComparison.Ordinal );
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new LessAnalyzer();
        var t = new LessTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
