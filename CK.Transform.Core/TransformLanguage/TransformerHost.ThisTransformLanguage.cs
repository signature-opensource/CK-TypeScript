namespace CK.Transform.Core;


public sealed partial class TransformerHost
{
    internal static readonly string _transformLanguageName = "Transform";

    /// <summary>
    /// Transform language itself.
    /// The target and transform statements analyzers are cached, bound to the TransformerHost.
    /// Its file extensions are ".transform" and ".t".
    /// </summary>
    sealed class ThisTransformLanguage : TransformLanguage
    {
        readonly ThisLanguageAnalyzer _rootAnalyzer;

        internal ThisTransformLanguage( TransformerHost host )
            : base( _transformLanguageName, ".transform", ".t" )
        {
            _rootAnalyzer = new ThisLanguageAnalyzer( host );
        }

        public ThisLanguageAnalyzer RootAnalyzer => _rootAnalyzer;

        protected internal override (TransformStatementAnalyzer, ITargetAnalyzer) CreateAnalyzers( Language language )
        {
            return (new ThisStatementAnalyzer( language ), _rootAnalyzer);
        }
    }

}
