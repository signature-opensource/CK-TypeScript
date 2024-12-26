using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;


public sealed partial class TransformerHost
{
    /// <summary>
    /// Transform language itself.
    /// The target and transform statements analyzers are cached, bound to the TransformerHost. 
    /// </summary>
    sealed class RootTransformLanguage : TransformLanguage
    {
        readonly RootTransformAnalyzer _rootAnalyzer;
        readonly TransformStatementAnalyzer _thisAnalyzer;

        sealed class ThisStatementAnalyzer : TransformStatementAnalyzer
        {
            public ThisStatementAnalyzer( TransformLanguage language )
                : base( language )
            {
            }
        }

        internal RootTransformLanguage( TransformerHost host )
            : base( "Transform" )
        {
            _thisAnalyzer = new ThisStatementAnalyzer( this );
            _rootAnalyzer = new RootTransformAnalyzer( host );
        }

        public RootTransformAnalyzer RootAnalyzer => _rootAnalyzer;

        public TransformStatementAnalyzer TransformStatementAnalyzer => _thisAnalyzer;

        protected internal override IAnalyzer<TransfomerFunction> CreateTargetAnalyzer() => _rootAnalyzer;

        protected internal override TransformStatementAnalyzer CreateTransformStatementAnalyzer( TransformerHost host ) => _thisAnalyzer;
    }

}
