using CK.Core;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.TS.Angular.Engine;

class NgRoute
{
    readonly NgRoutedComponentAttributeImpl? _routedAttr;
    readonly ITSDeclaredFileType? _tsType;
    NgRoute? _firstChild;
    NgRoute? _lastChild;
    NgRoute? _nextChild;

    public NgRoute( NgRoutedComponentAttributeImpl? component, ITSDeclaredFileType? tsType )
    {
        Throw.DebugAssert( "component != null => tsType != null", component == null || tsType != null );
        _routedAttr = component;
        _tsType = tsType;
    }

    public bool BindToTarget( IActivityMonitor monitor, Dictionary<Type, NgRoute> routes, Func<Type, Type?> typeMapper )
    {
        Throw.DebugAssert( _routedAttr != null );
        var t = _routedAttr.Attribute.TargetComponent;
        var mapped = t == typeof( AppComponent ) ? t : typeMapper( t );
        if( mapped == null )
        {
            monitor.Error( $"""Invalid [NgRoutedComponent] on '{_routedAttr.DecoratedType:N}': TargetComponent '{_routedAttr.Attribute.TargetComponent:C}' type cannot be resolved.""" );
            return false;
        }
        if( !routes.TryGetValue( mapped, out var target ) )
        {
            monitor.Error( $"""Invalid [NgRoutedComponent] on '{_routedAttr.DecoratedType:N}': TargetComponent '{_routedAttr.Attribute.TargetComponent:C}' is not a component with routes.""" );
            return false;
        }
        if( target != null )
        {
            if( target._firstChild == null )
            {
                target._firstChild = this;
            }
            else
            {
                Throw.DebugAssert( target._lastChild != null );
                target._lastChild._nextChild = this;
            }
            target._lastChild = this;
        }
        return true;
    }

    public bool IsAppComponent => _tsType == null;

    public bool IsRouted => _routedAttr != null;

    public bool HasChildren => _firstChild != null;

    public IEnumerable<NgRoute> Children
    {
        get
        {
            var c = _firstChild;
            while( c != null )
            {
                Throw.DebugAssert( "Child route can only be a RoutedComponent.", c._routedAttr != null );
                yield return c;

                c = c._nextChild;
            }
        }
    }

    internal void GenerateRoutes( IActivityMonitor monitor, TypeScriptFile routes, int childDepth )
    {
        ITSFileBodySection body = routes.Body;
        bool atLeastOne = false;
        foreach( var c in Children )
        {
            Throw.DebugAssert( "Child route can only be a RoutedComponent.", c._routedAttr != null && c._tsType != null );
            var comp = c._routedAttr;
            if( atLeastOne )
            {
                body.Append( "," ).NewLine();
            }
            atLeastOne = true;
            body.Whitespace( childDepth * 2 ).Append( "{ path: " ).AppendSourceString( comp.Route );
            if( comp.Attribute.RegistrationMode == RouteRegistrationMode.None )
            {
                routes.Imports.Import( c._tsType );
                body.Append( ", component: " ).Append( comp.ComponentName );
            }
            else
            {
                var f = c._tsType.File;
                body.Append( ", loadComponent: () => import( " )
                    .AppendSourceString( routes.Folder.GetRelativePathTo( f.Folder ).AppendPart( f.Name.Remove( f.Name.Length - 3 ) ) )
                    .Append( " ).then( c => c." ).Append( comp.ComponentName ).Append( " )" );
            }
            if( c.HasChildren )
            {
                Throw.DebugAssert( c is NgRouteWithRoutes );
                var routesNames = $"r{comp.ComponentName}";
                body.Append( ", children: " ).Append( routesNames ).NewLine();
                var r = ((NgRouteWithRoutes)c);
                routes.Imports.ImportFromFile( r.RoutesFile, $"default {routesNames}" );
                body.NewLine();
            }
            body.Append( " }" );
        }
    }

    public StringBuilder Write( StringBuilder b, int childDepth )
    {
        Throw.DebugAssert( !IsAppComponent );
        if( childDepth > 0 ) b.Append( ' ', childDepth * 2 );

        var name = _routedAttr?.FileComponentName ?? _tsType!.TypeName;
        b.Append( "-> " ).Append( name ).AppendLine();
        if( this is NgRouteWithRoutes r )
        {
            foreach( var c in r.Children )
            {
                c.Write( b, childDepth + 1 );
            }
        }
        return b;
    }

}
