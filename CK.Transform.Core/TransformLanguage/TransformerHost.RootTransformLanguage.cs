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

        sealed class ThisStatementAnalyzer : TransformStatementAnalyzer
        {
            public ThisStatementAnalyzer( TransformLanguage language )
                : base( language )
            {
            }

            // /// <summary>
            // /// There is currently no specific statements to transform transformers: the base
            // /// ParseStatement is fine.
            // /// </summary>
            // protected override TransformStatement? ParseStatement( ref TokenizerHead head )
            // {
            //     return base.ParseStatement( ref head );
            // }
        }

        internal RootTransformLanguage( TransformerHost host )
            : base( _transformLanguageName, ".t" )
        {
            _thisAnalyzer = new ThisStatementAnalyzer( this );
            _rootAnalyzer = new RootTransformAnalyzer( host );
        }

        public RootTransformAnalyzer RootAnalyzer => _rootAnalyzer;

        public TransformStatementAnalyzer TransformStatementAnalyzer => _thisAnalyzer;

        protected internal override (TransformStatementAnalyzer, IAnalyzer) CreateAnalyzers( TransformerHost host ) => (_thisAnalyzer, _rootAnalyzer);

    }

}
