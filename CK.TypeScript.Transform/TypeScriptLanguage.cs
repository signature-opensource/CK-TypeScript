using CK.Transform.Core;
using System;

namespace CK.TypeScript.Transform;

/// <summary>
/// Accepts ".ts" file name extension.
/// </summary>
public sealed class TypeScriptLanguage : TransformLanguage
{
    internal const string _languageName = "TypeScript";

    public TypeScriptLanguage()
        : base( _languageName, ".ts" )
    {
    }

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new TypeScriptAnalyzer();
        var t = new TypeScriptTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
