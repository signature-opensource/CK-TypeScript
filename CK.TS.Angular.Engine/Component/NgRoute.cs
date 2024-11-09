using CK.Core;
using CK.TypeScript.CodeGen;
using System.Collections.Generic;

namespace CK.TS.Angular.Engine;

class NgRoute
{
    readonly NgRoute? _parent;
    readonly NgRoutedComponentAttributeImpl? _component;
    readonly ITSDeclaredFileType? _tsType;
    NgRoute? _firstChild;
    NgRoute? _lastChild;
    NgRoute? _nextChild;

    public NgRoute( NgRoute? parent, NgRoutedComponentAttributeImpl? component, ITSDeclaredFileType? tsType )
    {
        Throw.DebugAssert( (parent == null) == (component == null) );
        Throw.DebugAssert( "Only NgRouteWithRoutes may NOT be a RoutedComponent.", parent != null || this is NgRouteWithRoutes );
        Throw.DebugAssert( component == null || tsType != null );
        _parent = parent;
        _component = component;
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

    public NgRoutedComponentAttributeImpl? Component => _component;

    public bool HasChildren => _firstChild != null;

    public IEnumerable<NgRoute> Children
    {
        get
        {
            var c = _firstChild;
            while( c != null )
            {
                Throw.DebugAssert( "Child route can only be a RoutedComponent.", c._component != null );
                yield return c;
                c = _nextChild;
            }
        }
    }

    internal void GenerateRoutes( IActivityMonitor monitor, TypeScriptFile routes )
    {
        ITSFileBodySection body = routes.Body;
        bool atLeastOne = false;
        foreach( var c in Children )
        {
            Throw.DebugAssert( "Child route can only be a RoutedComponent.", c._component != null && c._tsType != null );
            var comp = c._component;
            if( atLeastOne )
            {
                body.Append( "," ).NewLine();
            }
            body.Append( "{ path: " ).AppendSourceString( comp.Route );
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
                    .Append( " )" );
            }
            if( c.HasChildren && c is not NgRouteWithRoutes )
            {
                body.Append( ", children: [" ).NewLine();
                c.GenerateRoutes( monitor, routes );
                body.Append( "]" ).NewLine();
            }
            body.Append( "}" );
        }
    }
}
