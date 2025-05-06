namespace CK.Transform.Core;


public sealed partial class TransformerHost
{
    internal static readonly string _transformLanguageName = "Transform";

    /// <summary>
    /// Transform language itself.
    /// The target and transform statements analyzers are cached, bound to the TransformerHost.
    /// Its single file extension is ".t".
    /// </summary>
    sealed class RootTransformLanguage : TransformLanguage
    {
        readonly RootTransformAnalyzer _rootAnalyzer;
        readonly TransformStatementAnalyzer _thisAnalyzer;

        internal RootTransformLanguage( TransformerHost host )
            : base( _transformLanguageName, ".transform", ".t" )
        {
            _thisAnalyzer = new ThisStatementAnalyzer( this );
            _rootAnalyzer = new RootTransformAnalyzer( host );
        }

        public RootTransformAnalyzer RootAnalyzer => _rootAnalyzer;

        protected internal override (TransformStatementAnalyzer, ITargetAnalyzer) CreateAnalyzers( TransformerHost host )
        {
            return (_thisAnalyzer, _rootAnalyzer);
        }
    }

}
