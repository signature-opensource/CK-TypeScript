using CK.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.EmbeddedResources;

public sealed partial class FinalTranslationSet // SubSets
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

        public IFinalTranslationSet Parent => _root._subSets[_culture.Parent!.Index]!;

        public IEnumerable<IFinalTranslationSet> Children => _culture.Children.Select( c => _root._subSets[c.Index] ).Where( s => s != null )!;

        public IEnumerable<KeyValuePair<string, FinalTranslationValue>> RootPropagatedTranslations
        {
            get
            {
                // This should be optimal:
                // - No allocation for the direct children of the root.
                // - A single HashSet hidden by closure for deeper children.
                IEnumerable<KeyValuePair<string, FinalTranslationValue>> source = _translations;
                var parent = Parent;
                source = source.Concat( parent.Translations.Where( kv => !_translations.ContainsKey( kv.Key ) ) );
                // More than one level?
                var grandParent = parent.Parent;
                if( grandParent != null )
                {
                    var dedup = new HashSet<string>( _translations.Keys.Concat( parent.Translations.Keys ) );
                    do
                    {
                        source = source.Concat( grandParent.Translations.Where( kv => dedup.Add( kv.Key ) ) );
                        grandParent = grandParent.Parent;
                    }
                    while( grandParent != null );
                }
                return source;
            }
        }
    }

    internal IFinalTranslationSet[] CloneSubSets() => Unsafe.As<IFinalTranslationSet[]>( _subSets.Clone() );
}
