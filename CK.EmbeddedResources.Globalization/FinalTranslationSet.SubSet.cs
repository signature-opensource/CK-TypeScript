using CK.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.EmbeddedResources;

public sealed partial class FinalTranslationSet
{
    internal sealed class SubSet : IFinalTranslationSet
    {
        readonly FinalTranslationSet _root;
        readonly IReadOnlyDictionary<string, FinalTranslationValue> _translations;
        readonly ActiveCulture _culture;
        readonly bool _isAmbiguous;

        public SubSet( FinalTranslationSet root,
                       ActiveCulture culture,
                       IReadOnlyDictionary<string, FinalTranslationValue> translations,
                       bool isAmbiguous )
        {
            Throw.DebugAssert( culture.Index > 0 );
            _root = root;
            _translations = translations;
            _isAmbiguous = isAmbiguous;
            _culture = culture;
        }

        public IReadOnlyDictionary<string, FinalTranslationValue> Translations => _translations;

        public bool IsAmbiguous => _isAmbiguous;

        public ActiveCulture Culture => _culture;

        public IFinalTranslationSet? Parent => _root._subSets[_culture.Index];

        public IEnumerable<IFinalTranslationSet> Children => Culture.Children.Select( c => _root._subSets[c.Index] ).Where( s => s != null )!;
    }

    internal IFinalTranslationSet[] CloneSubSets() => Unsafe.As<IFinalTranslationSet[]>( _subSets.Clone() );
}
