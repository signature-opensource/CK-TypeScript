using CK.Transform.Core;
using System;

namespace CK.Core;

sealed partial class TFunction : ITransformable
{
    readonly TFunctionSource _source;
    TransformerFunction? _function;
    ITransformable? _target;
    TFunction? _nextFunction;
    TFunction? _prevFunction;
    TransformableImpl _transformableImpl;

    public TFunction( TFunctionSource source, TransformerFunction function, ITransformable target )
    {
        _source = source;
        _function = function;
        _target = target;
    }

    public TFunction? NextFunction => _nextFunction;

    TFunction? ITransformable.FirstFunction => _transformableImpl.FirstFunction;

    TFunction? ITransformable.LastFunction => _transformableImpl.LastFunction;

    void ITransformable.Add( TFunction f ) => _transformableImpl.Add( f );

    void ITransformable.Remove( TFunction f ) => _transformableImpl.Remove( f );

    internal void Die( IActivityMonitor monitor )
    {
        _function = null;
        _target?.Remove( this );
    }

    internal TransformerFunction? GetTransformerFunction( IActivityMonitor monitor, TransformerHost transformerHost )
    {
        Throw.DebugAssert( _function != null );
        if( _transformableImpl.HasFunctions )
        {
            var text = _source.Text.Substring( _function.Span.Beg, _function.Span.Length );
            text = _transformableImpl.Transform( monitor, transformerHost, text );
            if( text == null ) return null;
            return transformerHost.TryParseFunction( monitor, text );
        }
        return _function;
    }
}
