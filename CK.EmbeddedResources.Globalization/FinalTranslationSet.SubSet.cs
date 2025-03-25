using CK.Core;
using System.Collections.Generic;
using System.Linq;

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
            _root = root;
            _translations = translations;
            _isAmbiguous = isAmbiguous;
            _culture = culture;
        }

        public IReadOnlyDictionary<string, FinalTranslationValue> Translations => _translations;

        public bool IsAmbiguous => _isAmbiguous;

        public ActiveCulture Culture => _culture;

        public IEnumerable<IFinalTranslationSet> Children => Culture.Children.Select( c => _root._subSets[c.Index] ).Where( s => s != null )!;
    }
}
