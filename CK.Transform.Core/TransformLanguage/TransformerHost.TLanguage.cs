using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;


public sealed partial class TransformerHost
{
    sealed class TLanguage : TransformLanguage
    {
        readonly TransformerHost _host;

        internal TLanguage( TransformerHost host )
            : base( "Transform" )
        {
            _host = host;
        }

        internal protected override Analyzer CreateTargetAnalyzer() => _host._transformAnalyzer;

        internal protected override BaseTransformAnalyzer CreateTransformAnalyzer() => _host._transformAnalyzer;
    }
}
