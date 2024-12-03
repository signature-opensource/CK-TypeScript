using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;

public sealed class TransformTransformLanguage : TransformLanguage
{
    public TransformTransformLanguage()
        : base( "Transform" )
    {
    }

    internal protected override Analyzer CreateTargetAnalyzer() => new TransformTransformAnalyzer( this );

    internal protected override BaseTransformAnalyzer CreateTransformAnalyzer() => new TransformTransformAnalyzer( this );
}
