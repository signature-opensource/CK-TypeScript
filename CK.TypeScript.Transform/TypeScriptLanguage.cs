using CK.Transform.Core;
using CK.Transform.TransformLanguage;

namespace CK.TypeScript.Transform;

public sealed class TypeScriptLanguage : TransformLanguage
{
    public TypeScriptLanguage()
        : base( "TypeScript" )
    {
    }

    protected override Analyzer CreateTargetAnalyzer() => new TypeScriptAnalyzer();

    protected override BaseTransformAnalyzer CreateTransformAnalyzer(TransformerHost host) => new TypeScriptTransformAnalyzer( host, this );
}
