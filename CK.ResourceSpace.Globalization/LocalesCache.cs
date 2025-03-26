
using CK.EmbeddedResources;

namespace CK.Core;

sealed class LocalesCache : ResPackageDataHandler<FinalTranslationSet>
{
    readonly LocalesResourceHandler _handler;
    readonly ActiveCultureSet _activeCultures;

    public LocalesCache( LocalesResourceHandler handler, IResPackageDataCache cache, ActiveCultureSet activeCultures )
        : base( cache )
    {
        _handler = handler;
        _activeCultures = activeCultures;
    }

    public ActiveCultureSet ActiveCultures => _activeCultures;

    protected override FinalTranslationSet Aggregate( FinalTranslationSet data1, FinalTranslationSet data2 )
    {
        return data1.Aggregate( data2 );
    }

    protected override FinalTranslationSet? Combine( IActivityMonitor monitor, IResPackageResources resources, FinalTranslationSet data )
    {
        if( resources.Resources.LoadTranslations( monitor,
                                                  _activeCultures,
                                                  out var definitions,
                                                  _handler.RootFolderName ) )
        {
            return definitions != null
                    ? definitions.Combine( monitor, data )
                    : data;
        }
        return null;
    }

    protected override FinalTranslationSet? Create( IActivityMonitor monitor, ResPackage package )
    {
        FinalTranslationSet? initial = null;
        if( package.BeforeResources.Resources.LoadTranslations( monitor,
                                                                _activeCultures,
                                                                out var definitions,
                                                                _handler.RootFolderName ) )
        {
            initial = definitions != null
                            ? definitions.ToInitialFinalSet( monitor )
                            : new FinalTranslationSet( _activeCultures );
        }
        return initial;
    }
}
