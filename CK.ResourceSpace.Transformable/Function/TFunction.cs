using CK.Transform.Core;
using System.Collections.Generic;

namespace CK.Core;

sealed partial class TFunction : ITransformable
{
    readonly FunctionSource _source;
    // The target of the function is the key to detect an update of
    // an existing function vs. a destroy/insert of a new TFunction.
    // It is not readonly as an unbound function can be rebound to a new target.
    ITransformable _target;
    // The function name is used as the transform target
    // for transform of transform.
    string _functionName;
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
        return function.Name ?? $"{source.Resources.Package.FullName}({target.TransfomableTargetName})";
    }

    public string FunctionName => _functionName;

    public TFunction? NextFunction => _nextFunction;

    string ITransformable.TransfomableTargetName => _functionName;

    TFunction? ITransformable.FirstFunction => _transformableImpl.FirstFunction;

    TFunction? ITransformable.LastFunction => _transformableImpl.LastFunction;

    internal FunctionSource Source => _source;

    internal TransformerFunction Function => _function;

    internal ITransformable Target => _target;

    internal void SetNewTarget( ITransformable t ) => _target = t;

    /// <summary>
    /// Follows the transform functions until the ultimate item to transform is reached:
    /// it is a <see cref="TransformableItem"/> or a <see cref="ExternalItem"/>.
    /// </summary>
    internal ITransformable? PeeledTarget
    {
        get
        {
            var peeledTarget = _target;
            while( peeledTarget is TFunction f2 )
            {
                peeledTarget = f2._target;
            }
            return peeledTarget;
        }
    }

    internal void MustApplyChangesToTarget( HashSet<TransformableItem> toBeInstalled )
    {
        // This is only called in Live mode: 
        // ExternalItem are not serialized (their functions are not registered) and
        // on change their source, they are not resolved (as there's no external item resolver).
        // => There is no more ExternalItem, only TFunction and TransformableItem.
        Throw.DebugAssert( PeeledTarget is TransformableItem );
        toBeInstalled.Add( (TransformableItem)PeeledTarget );
    }

    internal void Update( TransformerFunction transformerFunction, string functionName )
    {
        _function = transformerFunction;
        _functionName = functionName;
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
