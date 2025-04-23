using CK.Transform.Core;
using System;

namespace CK.Core;

sealed partial class TFunction : ITransformable
{
    readonly FunctionsSource _source;
    // The name of the function (see ComputeName) is the key to
    // detect an update of an existing function vs. a destroy/insert
    // of a new TFunction.
    readonly string _functionName;
    ITransformable _target;
    TFunction? _nextFunction;
    TFunction? _prevFunction;
    TransformableImpl _transformableImpl;
    TransformerFunction _function;

    public TFunction( FunctionsSource source,
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

    internal static string ComputeName( FunctionsSource source, TransformerFunction function, ITransformable target )
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

    internal FunctionsSource Source => _source;

    public TransformerFunction Function => _function;

    internal void SetFunction( TransformerFunction transformerFunction )
    {
        _function = transformerFunction;
    }

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
            var text = _transformableImpl.Transform( monitor, transformerHost, _function.Text );
            if( text == null ) return null;
            return transformerHost.TryParseFunction( monitor, text );
        }
        return _function;
    }

}
