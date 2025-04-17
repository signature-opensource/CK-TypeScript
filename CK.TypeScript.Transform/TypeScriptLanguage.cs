using CK.Transform.Core;

namespace CK.TypeScript.Transform;

/// <summary>
/// Accepts ".ts" file name extension.
/// </summary>
public sealed class TypeScriptLanguage : TransformLanguage
{
    internal const string _languageName = "TypeScript";

    /// <summary>
    /// Initializes a new TypeScriptLanguage instance.
    /// </summary>
    public TypeScriptLanguage()
        : base( _languageName, ".ts" )
    {
    }

    /// <inheritdoc/>
    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new TypeScriptAnalyzer();
        var t = new TypeScriptTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
