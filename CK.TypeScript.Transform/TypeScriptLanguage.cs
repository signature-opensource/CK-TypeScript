using CK.Transform.Core;
using CK.Transform.TransformLanguage;

namespace CK.TypeScript.Transform;

sealed class TypeScriptLanguage : TransformLanguage
{
    readonly TransformerHost _host;

    internal TypeScriptLanguage( TransformerHost host )
    : base( "TypeScript" )
    {
        _host = host;
    }

    protected override Analyzer CreateTargetAnalyzer() => new TypeScriptAnalyzer();

    protected override BaseTransformAnalyzer CreateTransformAnalyzer() => new TypeScriptTransformAnalyzer( _host, this );
}
