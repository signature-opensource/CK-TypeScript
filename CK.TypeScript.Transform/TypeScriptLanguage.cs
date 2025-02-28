using CK.Transform.Core;
using System;

namespace CK.TypeScript.Transform;

public sealed class TypeScriptLanguage : TransformLanguage
{
    internal const string _languageName = "TypeScript";

    public TypeScriptLanguage()
        : base( _languageName )
    {
    }

    /// <summary>
    /// Accepts ".ts" file name extension.
    /// </summary>
    /// <param name="fileName">The file name or path to consider.</param>
    /// <returns>True if this is a TypeScript file name.</returns>
    public override bool IsLangageFilename( ReadOnlySpan<char> fileName ) => fileName.EndsWith( ".ts", StringComparison.Ordinal );

    protected override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host )
    {
        var a = new TypeScriptAnalyzer();
        var t = new TypeScriptTransformStatementAnalyzer( this, a );
        return (t, a);
    }
}
