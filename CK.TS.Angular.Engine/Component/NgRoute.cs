using CK.Core;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.TS.Angular.Engine;

class NgRoute
{
    readonly NgRoute? _parent;
    readonly NgRoutedComponentAttributeImpl? _routedAttr;
    readonly ITSDeclaredFileType? _tsType;
    NgRoute? _firstChild;
    NgRoute? _lastChild;
    NgRoute? _nextChild;

    public NgRoute( NgRoute? parent, NgRoutedComponentAttributeImpl? component, ITSDeclaredFileType? tsType )
    {
        Throw.DebugAssert( (parent == null) == (component == null) );
        Throw.DebugAssert( "Only NgRouteWithRoutes may NOT be a RoutedComponent.", parent != null || this is NgRouteWithRoutes );
        Throw.DebugAssert( "component != null => tsType != null", component == null || tsType != null );
        _parent = parent;
        _routedAttr = component;
        _tsType = tsType;
        if( _parent != null )
        {
            if( _parent._firstChild == null )
            {
                _parent._firstChild = this;
            }
            else
            {
                Throw.DebugAssert( _parent._lastChild != null );
                _parent._lastChild._nextChild = this;
            }
            _parent._lastChild = this;
        }
    }

    public bool IsAppComponent => _routedAttr == null && _tsType == null;

    public NgRoutedComponentAttributeImpl? Component => _routedAttr;

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
                c = _nextChild;
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
        if( this is NgRouteWithRoutes r )
        {
            b.Append('[').Append( name ).Append( ']' ).AppendLine();
            foreach( var c in r.Children )
            {
                c.Write( b, childDepth + 1 );
            }
            b.Append( ']' ).AppendLine();
        }
        else
        {
            b.Append( '[' ).Append( name ).Append( ']' );
        }
        return b;
    }

    public StringBuilder WritePath( StringBuilder b )
    {
        if( IsAppComponent ) return b.Append( "[AppComponent]" );

        var name = _routedAttr?.FileComponentName ?? _tsType!.TypeName;
        if( this is NgRouteWithRoutes r )
        {
            b.Append("[WithRoutes ").Append( name ).Append( " - " ).Append( r.Children.Count() ).Append( "] " );
        }
        else
        {
            b.Append( "[Route" ).Append( name ).Append( ']' );
        }
        if( _parent != null )
        {
            b.Append( " -> " );
            _parent.WritePath( b );
        }
        return b;
    }

    public override sealed string ToString() => WritePath( new StringBuilder() ).ToString();

}
