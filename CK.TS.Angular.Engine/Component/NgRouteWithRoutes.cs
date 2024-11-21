using CK.Core;
using CK.TypeScript.CodeGen;
using System.Linq;

namespace CK.TS.Angular.Engine;

sealed class NgRouteWithRoutes : NgRoute
{
    readonly TypeScriptFile _routes;
    internal NgRouteWithRoutes? _nextWithRoutes;

    public NgRouteWithRoutes( TypeScriptFile routeFile,
                              NgRoute? parent,
                              NgRoutedComponentAttributeImpl? component,
                              ITSDeclaredFileType? tsType,
                              NgRouteWithRoutes? nextWithRoutes )
        : base( parent, component, tsType )
    {
        _routes = routeFile;
        _nextWithRoutes = nextWithRoutes;
    }

    internal void GenerateRoutes( IActivityMonitor monitor )
    {
        _routes.Body.Append( """
                export default [

                """ );
        GenerateRoutes( monitor, _routes );
        _routes.Body.Append( """

                ];
                """ );

    }
}
