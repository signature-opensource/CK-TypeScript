using CK.TS.Angular;

namespace CK.Ng.PublicPage;

//[SrcAppTransformer( """
//    create <html> transformer
//    begin
//      replace first "<router-outlet />" with "<ck-public-page />";
//    end
//    """ )]

[NgComponent( HasRoutes = true )]
public class PublicPageComponent : NgComponent
{
}
