using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CK.TS.Angular.Engine;

sealed class ComponentManager
{
    readonly Dictionary<Type, NgRoute> _routes;
    readonly TypeScriptContext _context;
    readonly LibraryImport _angularCore;
    readonly Dictionary<string, ITSDeclaredFileType> _namedComponents;
    readonly TypeScriptFile _namedComponentsResolver;

    NgRouteWithRoutes _firstWithRoutes;

    public ComponentManager( TypeScriptContext context, LibraryImport angularCore )
    {
        _context = context;
        _angularCore = angularCore;
        _routes = new Dictionary<Type, NgRoute>();
        _firstWithRoutes = RegisterNgRouteWithRoutes( typeof( AppComponent ), "CK/Angular", null, null );
        _namedComponents = new Dictionary<string, ITSDeclaredFileType>();
        _namedComponentsResolver = context.Root.Root.FindOrCreateTypeScriptFile( "CK/Angular/NamedComponentsResolver.ts" );
        _context.AfterCodeGeneration += OnAfterCodeGeneration;
    }

    internal bool RegisterComponent( IActivityMonitor monitor, NgComponentAttributeImpl ngComponent, ITSDeclaredFileType tsType )
    {
        // Named component registration.
        if( typeof( INgNamedComponent ).IsAssignableFrom( ngComponent.DecoratedType ) )
        {
            var name = ngComponent.FileComponentName;
            if( _namedComponents.TryGetValue( name, out var exists ) )
            {
                monitor.Error( $"""
                    Named component '{name}' defined by '{ngComponent.DecoratedType:N}'
                    cannot be mapped to '{tsType.File.Folder}{tsType.File}', it is already mapped to '{exists.File.Folder}{exists.File}'.
                    """ );
                return false;
            }
            _namedComponents.Add( name, tsType );
        }
        // Routes handling.
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
        GenerateNamedComponentsResolver();
        GenerateRoutes( e.Monitor );
    }

    void GenerateNamedComponentsResolver()
    {
        _namedComponentsResolver.Imports.ImportFromLibrary( _angularCore, "Type" );
        var b = _namedComponentsResolver.Body;
        b.Append( """
            export function resolveNamedComponentTypeAsync( name: string ): Promise<Type<unknown>> | undefined {
              switch( name ) {

            """ );

        foreach( var (name, type) in _namedComponents )
        {
            b.Append( "    case " ).AppendSourceString( name )
                .Append( ": return import( '../../" ).AppendSourceString( type.ImportPath )
                .Append( "' ).then( c => c." )
                .Append( type.TypeName ).Append( " );" ).NewLine();
        }
        b.Append( """
                    }
                    return;
                  }
                  """ );
    }

    void GenerateRoutes( IActivityMonitor monitor )
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
                success &= route.BindToTarget( monitor, _routes, typeMapper );
            }
        }
        if( success )
        {
            StringBuilder bLog = new StringBuilder( "Generating Angular static Routes:" );
            bLog.AppendLine().Append( "-> AppComponent" ).AppendLine();
            var r = _firstWithRoutes;
            do
            {
                if( !r.IsAppComponent ) r.Write( bLog, 1 );
                r.GenerateRoutes( monitor, 0 );
                r = r._nextWithRoutes;
            }
            while( r != null );
            monitor.Info( bLog.ToString() );
        }
    }
}
