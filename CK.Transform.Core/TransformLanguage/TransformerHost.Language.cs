using CK.Core;

namespace CK.Transform.Core;

public sealed partial class TransformerHost
{
    /// <summary>
    /// Cached instance of a language for this host.
    /// </summary>
    public sealed class Language
    {
        readonly TransformerHost _host;
        readonly TransformLanguage _language;
        readonly LanguageTransformAnalyzer _transformLanguageAnalyzer;
        readonly int _index;

        /// <summary>
        /// Gets the <see cref="TransformLanguage.LanguageName"/>.
        /// </summary>
        public string LanguageName => _language.LanguageName;

        /// <summary>
        /// Gets the transform language.
        /// </summary>
        public TransformLanguage TransformLanguage => _language;

        /// <summary>
        /// Gets the analyzer for the transform language.
        /// </summary>
        public LanguageTransformAnalyzer TransformLanguageAnalyzer => _transformLanguageAnalyzer;

        /// <summary>
        /// Gets the target language analyzer.
        /// </summary>
        public ITargetAnalyzer TargetLanguageAnalyzer => _transformLanguageAnalyzer.TargetAnalyzer;

        /// <summary>
        /// Gets whether this is an automatic language.
        /// See <see cref="TransformLanguage.IsAutoLanguage"/>.
        /// </summary>
        public bool IsAutoLanguage => _language.IsAutoLanguage;

        /// <summary>
        /// Gets the index in the <see cref="TransformerHost.Languages"/> list or -1
        /// if <see cref="IsAutoLanguage"/> is true.
        /// </summary>
        public int Index => _index;

        /// <summary>
        /// Gets the host to which this language is bound.
        /// </summary>
        public TransformerHost Host => _host;

        // Registered language constructor.
        internal Language( TransformerHost host, TransformLanguage language, int index )
        {
            _host = host;
            _language = language;
            _index = index;
            _transformLanguageAnalyzer = language.CreateAnalyzer( this );
        }

        // Auto language.
        internal Language( TransformerHost host, TransformLanguage language )
        {
            Throw.DebugAssert( language.IsAutoLanguage );
            _host = host;
            _language = language;
            _index = -1;
            _transformLanguageAnalyzer = language.CreateAnalyzer( this );
        }
    }

}
