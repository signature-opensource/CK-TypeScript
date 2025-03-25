using CK.Core;
using System.Collections.Generic;
using System.Linq;

namespace CK.EmbeddedResources;

public sealed partial class TranslationDefinitionSet
{
    sealed class SubSet : ITranslationDefinitionSet
    {
        readonly TranslationDefinitionSet _root;
        readonly ActiveCulture _culture;
        readonly ResourceLocator _origin;
        readonly IReadOnlyDictionary<string, TranslationDefinition> _translations;

        public SubSet( TranslationDefinitionSet root,
                       ActiveCulture culture,
                       ResourceLocator origin,
                       IReadOnlyDictionary<string, TranslationDefinition> translations )
        {
            _root = root;
            _culture = culture;
            _origin = origin;
            _translations = translations;
        }

        public ActiveCulture Culture => _culture;

        public ResourceLocator Origin => _origin;

        public IReadOnlyDictionary<string, TranslationDefinition> Translations => _translations;

        public IEnumerable<ITranslationDefinitionSet> Children => Culture.Children.Select( c => _root._subSets[c.Index] ).Where( s => s != null )!;
    }

    internal bool CheckNoSubSet( IActivityMonitor monitor, ActiveCulture c, string cultureName, ResourceLocator o )
    {
        if( _subSets[c.Index] != null )
        {
            monitor.Error( $"Duplicate files found for culture '{cultureName}': {o} and {_subSets[c.Index]!.Origin} lead to the same culture." );
            return false;
        }
        return true;
    }

    internal ITranslationDefinitionSet CreateSubSet( ActiveCulture culture, ResourceLocator o, Dictionary<string, TranslationDefinition> translations )
    {
        Throw.DebugAssert( _subSets[culture.Index] == null );
        var subSet = new SubSet( this, culture, o, translations );
        _subSets[culture.Index] = subSet;
        return subSet;
    }


}
