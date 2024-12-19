using CK.Core;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Builds a unique range on the extrema of the inner range(s).
/// </summary>
public sealed class NodeScopeExtrema : NodeScopeBuilder
{
    /// <summary>
    /// Parameter for the extrema detection.
    /// </summary>
    public enum Option
    {
        /// <summary>
        /// The extrema are the smallest and greatest locations of the inner ranges.
        /// </summary>
        None,

        /// <summary>
        /// The final range is from the very first node of the root up to the
        /// smallest start of the inner ranges.  
        /// </summary>
        Before,

        /// <summary>
        /// The final range is from the very first node of the root up to the greatest end of the inner ranges.  
        /// </summary>
        BeforeIncluded,

        /// <summary>
        /// The final range is from the smallest start of the inner ranges up to the last node of the root.
        /// </summary>
        AfterIncluded,

        /// <summary>
        /// The final range is from the greatest end of the inner ranges up to the last node of the root.  
        /// </summary>
        After
    }

    readonly NodeScopeBuilder _inner;
    readonly Option _option;
    NodeLocation _first;
    NodeLocation _last;

    public NodeScopeExtrema( NodeScopeBuilder inner, Option option )
    {
        Throw.CheckNotNullArgument( inner );
        _inner = inner.GetSafeBuilder();
        _option = option;
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeExtrema( _inner, _option );

    private protected override void DoReset()
    {
        _inner.Reset();
        _first = _last = null;
    }

    private protected override INodeLocationRange? DoEnter( IVisitContext context )
    {
        return Handle( _inner.Enter( context ), null );
    }

    private protected override INodeLocationRange? DoLeave( IVisitContext context )
    {
        return Handle( _inner.Leave( context ), null );
    }

    private protected override INodeLocationRange? DoConclude( IVisitContextBase context )
    {
        return Handle( _inner.Conclude( context ), context.LocationManager );
    }

    INodeLocationRange? Handle( INodeLocationRange r, INodeLocationManager? locationManager )
    {
        if( r != null )
        {
            _first = r.First.Beg.Min( _first );
            _last = r.Last.End.Max( _last );
        }
        if( locationManager != null )
        {
            if( _first == null ) return null;
            switch( _option )
            {
                case Option.AfterIncluded:
                    _last = locationManager.EndMarker;
                    break;
                case Option.After:
                    _first = _last;
                    _last = locationManager.EndMarker;
                    break;
                case Option.Before:
                    _last = _first;
                    _first = locationManager.GetFullLocation( 0 );
                    break;
                case Option.BeforeIncluded:
                    _first = locationManager.GetFullLocation( 0 );
                    break;
            }
            return _first.IsEndMarker || _first.Position == _last.Position
                    ? null
                    : new NodeLocationRange( _first, _last );
        }
        return null;
    }

    string ToString( string inner ) => _option switch
    {
        Option.None => $"(extrema of {inner})",
        Option.AfterIncluded => $"(from the start of {inner} to the end)",
        Option.After => $"(from the end of {inner} to the end)",
        Option.Before => $"(from the start to the start of {inner})",
        _ => $"(from the start to the end of {inner})"
    };

    /// <summary>
    /// Overridden to return the description of this builder.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() => ToString( _inner.ToString() );

}


