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
        // We don't type the export default as Route[] for subordinated routes.ts.
        _routes.Body.Append( """
                // import { Route } from '@angular/router';

                export default [

                """ );
        GenerateRoutes( monitor, _routes, childDepth, out _ );
        _routes.Body.Append( """

                ]; // as Route[];
                """ );
    }

}
