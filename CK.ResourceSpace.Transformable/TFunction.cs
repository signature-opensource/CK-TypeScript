using CK.Transform.Core;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CK.Core;

sealed partial class TFunction : ITransformable
{
    readonly TFunctionSource _source;
    TransformerFunction _function;
    string _functionName;
    ITransformable _target;
    TFunction? _nextFunction;
    TFunction? _prevFunction;
    TransformableImpl _transformableImpl;

    public TFunction( TFunctionSource source,
                      TransformerFunction function,
                      ITransformable target,
                      string functionName )
    {
        Throw.DebugAssert( functionName == ComputeName( source, function, target ) );
        _source = source;
        _function = function;
        _target = target;
        _functionName = functionName;
    }

    internal static string ComputeName( TFunctionSource source, TransformerFunction function, ITransformable target )
    {
        var n = function.Name;
        if( n == null )
        {
            n = $"{source.SourceName}({target.TransfomableTargetName})";
        }
        return n;
    }

    public TFunction? NextFunction => _nextFunction;

    string ITransformable.TransfomableTargetName => _functionName;

    TFunction? ITransformable.FirstFunction => _transformableImpl.FirstFunction;

    TFunction? ITransformable.LastFunction => _transformableImpl.LastFunction;

    internal TFunctionSource Source => _source;

    public TransformerFunction Function => _function;

    bool ITransformable.TryFindInsertionPoint( IActivityMonitor monitor, TFunctionSource source, TransformerFunction f, out TFunction? before )
            => _transformableImpl.TryFindInsertionPoint( monitor, source, f, out before );

    void ITransformable.Add( TFunction f, TFunction? before ) => _transformableImpl.Add( f, before );

    void ITransformable.Remove( TFunction f ) => _transformableImpl.Remove( f );

    internal void Die( IActivityMonitor monitor )
    {
        _target?.Remove( this );
    }

    internal TransformerFunction? GetTransformerFunction( IActivityMonitor monitor, TransformerHost transformerHost )
    {
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
