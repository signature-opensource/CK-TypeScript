using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
        _firstWithRoutes = RegisterNgRouteWithRoutes( typeof( AppComponent ), "CK/Angular", null, null, null );
        _context.AfterCodeGeneration += OnAfterCodeGeneration;
    }

    internal bool RegisterComponent( IActivityMonitor monitor, NgComponentAttributeImpl ngComponent, ITSDeclaredFileType tsType )
    {
        NgRoute? target = null;
        var asRoutedComponent = ngComponent as NgRoutedComponentAttributeImpl;
        if( asRoutedComponent != null )
        {
            if( !_routes.TryGetValue( asRoutedComponent.Attribute.TargetComponent, out target ) )
            {
                monitor.Error( $"""Invalid [NgRoutedComponent] on '{asRoutedComponent.DecoratedType:N}': TargetComponent '{asRoutedComponent.Attribute.TargetComponent:C}' is not a component with routes.""" );
                return false;
            }
        }
        if( ngComponent.Attribute.HasRoutes )
        {
            RegisterNgRouteWithRoutes( ngComponent.DecoratedType, ngComponent.TypeScriptFolder, target, asRoutedComponent, tsType );
        }
        else if( target != null )
        {
            _routes.Add( ngComponent.DecoratedType, new NgRoute( target, asRoutedComponent, tsType ) );
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
        Throw.DebugAssert( _routes[typeof( AppComponent )].IsAppComponent );
        Throw.DebugAssert( _routes.Values.Count( r => r.IsAppComponent ) == 1 );
        StringBuilder bLog = new StringBuilder( "Generating Angular static Routes:" );
        bLog.AppendLine().Append( "[AppComponent]" ).AppendLine();
        var r = _firstWithRoutes;
        do
        {
            if( !r.IsAppComponent ) r.Write( bLog, 1 );
            r.GenerateRoutes( e.Monitor, 0 );
            r = r._nextWithRoutes;
        }
        while( r != null );
        e.Monitor.Info( bLog.ToString() );
    }

}
