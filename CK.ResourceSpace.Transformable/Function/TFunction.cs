using CK.Transform.Core;
using System;

namespace CK.Core;

sealed partial class TFunction : ITransformable
{
    readonly FunctionSource _source;
    // The name of the function (see ComputeName) is the key to
    // detect an update of an existing function vs. a destroy/insert
    // of a new TFunction.
    readonly string _functionName;
    ITransformable _target;
    TFunction? _nextFunction;
    TFunction? _prevFunction;
    TransformableImpl _transformableImpl;
    TransformerFunction _function;

    public TFunction( FunctionSource source,
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

    internal static string ComputeName( FunctionSource source, TransformerFunction function, ITransformable target )
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

    internal FunctionSource Source => _source;

    public TransformerFunction Function => _function;

    internal void SetFunction( TransformerFunction transformerFunction )
    {
        _function = transformerFunction;
    }

    bool ITransformable.TryFindInsertionPoint( IActivityMonitor monitor, FunctionSource source, TransformerFunction f, out TFunction? before )
            => _transformableImpl.TryFindInsertionPoint( monitor, source, f, out before );

    void ITransformable.Add( TFunction f, TFunction? before ) => _transformableImpl.Add( f, before );

    void ITransformable.Remove( TFunction f ) => _transformableImpl.Remove( f );

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

    /// <summary>
    /// Removes this function from the <see cref="TransformEnvironment.TransformFunctions"/> name dictionary
    /// and from its <see cref="ITransformable"/> target.
    /// </summary>
    /// <param name="environment">The environment.</param>
    internal void Remove( TransformEnvironment environment )
    {
        Throw.DebugAssert( environment.TransformFunctions.TryGetValue( _functionName, out var found ) && found == this );
        environment.TransformFunctions.Remove( _functionName );
        _target.Remove( this );
    }

    /// <summary>
    /// Removes this function from the <see cref="TransformEnvironment.UnboundFunctions"/>
    /// (if it exists) and adds the transformers of this transformer to the UnboundFunctions if any.
    /// </summary>
    /// <param name="environment">The environment.</param>
    internal void Destroy( TransformEnvironment environment )
    {
        Throw.DebugAssert( environment.IsLive );
        environment.UnboundFunctions.Remove( this );
        var f = _transformableImpl.FirstFunction;
        while( f != null )
        {
            environment.UnboundFunctions.Add( f );
            f = f.NextFunction;
        }
    }

}
