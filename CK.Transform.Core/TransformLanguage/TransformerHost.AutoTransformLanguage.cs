using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;


public sealed partial class TransformerHost
{
    /// <summary>
    /// Transform language of the transfom language of a known language:
    /// the <see cref="Language.TargetLanguageAnalyzer"/> is the <see cref="Language.TransformLanguageAnalyzer"/>
    /// of the target language.
    /// </summary>
    sealed class AutoTransformLanguage : TransformLanguage
    {
        readonly Language _target;

        internal AutoTransformLanguage( Language target )
            : base( target )
        {
            _target = target;
        }

        protected internal override TransformLanguageAnalyzer CreateAnalyzer( Language language )
        {
            return new TransformLanguageAnalyzer( language, _target.TransformLanguageAnalyzer );
        }
    }
}
