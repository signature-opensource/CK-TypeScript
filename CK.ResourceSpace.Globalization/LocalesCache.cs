
//using CK.EmbeddedResources;

//namespace CK.Core;

//sealed class LocalesCache : ResPackageDataHandler<FinalLocaleCultureSet>
//{
//    readonly AssetsResourceHandler _handler;

//    public LocalesCache( AssetsResourceHandler handler, IResPackageDataCache cache )
//        : base( cache )
//    {
//        _handler = handler;
//    }

//    protected override FinalResourceAssetSet Aggregate( FinalResourceAssetSet data1, FinalResourceAssetSet data2 )
//    {
//        return data1.Aggregate( data2 );
//    }

//    protected override FinalResourceAssetSet? Combine( IActivityMonitor monitor, IResPackageResources resources, FinalResourceAssetSet data )
//    {
//        if( resources.Resources.LoadAssets( monitor,
//                                            resources.Package.DefaultTargetPath,
//                                            out var definitions,
//                                            _handler.RootFolderName ) )
//        {
//            return definitions != null
//                    ? definitions.Combine( monitor, data )
//                    : data;
//        }
//        return null;
//    }

//    protected override FinalResourceAssetSet? Create( IActivityMonitor monitor, ResPackage package )
//    {
//        FinalResourceAssetSet? initial = null;
//        if( package.BeforeResources.Resources.LoadAssets( monitor,
//                                                           package.DefaultTargetPath,
//                                                           out var definitions,
//                                                           _handler.RootFolderName ) )
//        {
//            initial = definitions != null
//                            ? definitions.ToInitialFinalSet( monitor )
//                            : FinalResourceAssetSet.Empty;
//        }
//        return initial;
//    }
//}
