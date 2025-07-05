using CK.Core;
using CK.TypeScript.CodeGen;
using System.Linq;

namespace CK.TS.Angular.Engine;

sealed class NgRouteWithRoutes : NgRoute
{
    readonly TypeScriptFile _routes;
    internal NgRouteWithRoutes? _nextWithRoutes;

    public NgRouteWithRoutes( TypeScriptFile routeFile,
                              NgRoutedComponentAttributeImpl? component,
                              ITSDeclaredFileType? tsType,
                              NgRouteWithRoutes? nextWithRoutes )
        : base( component, tsType )
    {
        _routes = routeFile;
        _nextWithRoutes = nextWithRoutes;
    }

    public TypeScriptFile RoutesFile => _routes;

    internal void GenerateRoutes( IActivityMonitor monitor, int childDepth )
    {
        _routes.Body.Append( """
                export default [

                """ );
        GenerateRoutes( monitor, _routes, childDepth );
        _routes.Body.Append( """

                ];
                """ );

    }
}
