using CK.Transform.Core;
using CK.Transform.TransformLanguage;

namespace CK.TypeScript.Transform;

public sealed class TypeScriptLanguage : TransformLanguageOld
{
    public TypeScriptLanguage()
        : base( "TypeScript" )
    {
    }

    protected override Analyzer CreateTargetAnalyzer() => new TypeScriptAnalyzer();

    protected override BaseTransformAnalyzer CreateTransformAnalyzer(TransformerHostOld host) => new TypeScriptTransformAnalyzer( host, this );
}
