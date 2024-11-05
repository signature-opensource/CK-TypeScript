using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;

namespace CK.TS.Angular.Engine;

sealed class ComponentManager
{
    readonly Dictionary<Type, NgRoute> _routes;
    readonly TypeScriptContext _context;
    NgRouteWithRoutes _firstWithRoutes;

    public ComponentManager( TypeScriptContext context )
    {
        _context = context;
        _routes = new Dictionary<Type, NgRoute>();
        _firstWithRoutes = RegisterNgRouteWithRoutes( typeof( CKGenAppModule ), "CK/Angular", null, null, null );
        _context.AfterCodeGeneration += OnAfterCodeGeneration;
    }

    internal bool RegisterComponent( IActivityMonitor monitor, NgComponentAttributeImpl ngComponent, ITSDeclaredFileType tsType )
    {
        NgRoute? target = null;
        var routedComponent = ngComponent as NgRoutedComponentAttributeImpl;
        if( routedComponent != null )
        {
            if( !_routes.TryGetValue( routedComponent.Attribute.TargetComponent, out target ) )
            {
                monitor.Error( $"""Invalid [NgRoutedComponent] on '{routedComponent.DecoratedType:N}': TargetComponent '{routedComponent.Attribute.TargetComponent:C}' is not a component with routes.""" );
                return false;
            }
        }
        if( ngComponent.Attribute.HasRoutes )
        {
            RegisterNgRouteWithRoutes( ngComponent.DecoratedType, ngComponent.TypeScriptFolder, target, routedComponent, tsType );
        }
        else if( target != null )
        {
            _routes.Add( ngComponent.DecoratedType, new NgRoute( target, routedComponent, tsType ) );
        }
        return true;
    }

    NgRouteWithRoutes RegisterNgRouteWithRoutes( Type type,
                                                 NormalizedPath folder,
                                                 NgRoute? parent,
                                                 NgRoutedComponentAttributeImpl? component,
                                                 ITSDeclaredFileType? tsType )
    {
        var r = _context.Root.Root.FindOrCreateTypeScriptFile( folder.AppendPart( "routes.ts" ) );
        var c = new NgRouteWithRoutes( r, parent, component, tsType, _firstWithRoutes );
        _routes.Add( type, c );
        return _firstWithRoutes = c;
    }

    void OnAfterCodeGeneration( object? sender, EventMonitoredArgs e )
    {
        var r = _firstWithRoutes;
        do
        {
            r.GenerateRoutes( e.Monitor );
            r = r._nextWithRoutes;
        }
        while( r != null );
    }

}
