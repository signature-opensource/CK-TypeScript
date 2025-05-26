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
        _firstWithRoutes = RegisterNgRouteWithRoutes( typeof( AppComponent ), "CK/Angular", null, null );
        _context.AfterCodeGeneration += OnAfterCodeGeneration;
    }

    internal bool RegisterComponent( IActivityMonitor monitor, NgComponentAttributeImpl ngComponent, ITSDeclaredFileType tsType )
    {
        var asRoutedComponent = ngComponent as NgRoutedComponentAttributeImpl;
        if( ngComponent.Attribute.HasRoutes )
        {
            RegisterNgRouteWithRoutes( ngComponent.DecoratedType, ngComponent.TypeScriptFolder, asRoutedComponent, tsType );
        }
        else if( asRoutedComponent != null )
        {
            _routes.Add( ngComponent.DecoratedType, new NgRoute( asRoutedComponent, tsType ) );
        }
        return true;
    }

    NgRouteWithRoutes RegisterNgRouteWithRoutes( Type type,
                                                 NormalizedPath folder,
                                                 NgRoutedComponentAttributeImpl? component,
                                                 ITSDeclaredFileType? tsType )
    {
        var r = _context.Root.Root.FindOrCreateTypeScriptFile( folder.AppendPart( "routes.ts" ) );
        var c = new NgRouteWithRoutes( r, component, tsType, _firstWithRoutes );
        _routes.Add( type, c );
        return _firstWithRoutes = c;
    }

    void OnAfterCodeGeneration( object? sender, EventMonitoredArgs e )
    {
        Throw.DebugAssert( _routes[typeof( AppComponent )].IsAppComponent );
        Throw.DebugAssert( _routes.Values.Count( r => r.IsAppComponent ) == 1 );
        Throw.DebugAssert( "We can reach the ResSpaceData...", _context.ResSpaceData != null );

        // This is why we need the SpaceData here: the routed target is a Type
        // that can be an abstraction (INgPublic/PrivatePageComponent).
        var typeMapper = delegate ( Type t )
        {
            return _context.ResSpaceData.PackageIndex.GetValueOrDefault( t )?.Type;
        };
        bool success = true;
        foreach( var route in _routes.Values )
        {
            if( route.IsRouted )
            {
                success &= route.BindToTarget( e.Monitor, _routes, typeMapper );
            }
        }
        if( success )
        {
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
}
