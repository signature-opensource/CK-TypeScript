using CK.Transform.Core;

namespace CK.Transform.Core;


public sealed partial class TransformerHostOld
{
    sealed class TLanguage : TransformLanguageOld
    {
        readonly TransformerHostOld _host;

        internal TLanguage( TransformerHostOld host )
            : base( "Transform" )
        {
            _host = host;
        }

        internal protected override Analyzer CreateTargetAnalyzer() => new TAnalyzer( _host );

        internal protected override BaseTransformAnalyzer CreateTransformAnalyzer(  TransformerHostOld host ) => _host._transformAnalyzer;
    }
}
