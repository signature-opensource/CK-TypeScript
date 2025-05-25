using CK.Transform.Core;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Core;

/// <summary>
/// An external item is only a <see cref="ITransformable"/>. It wraps a public <see cref="ExternalTransformableItem"/>
/// that is resolved by the <see cref="IExternalTransformableItemResolver"/>.
/// </summary>
sealed class ExternalItem : ITransformable
{
    readonly ExternalTransformableItem _item;
    TFunction.TransformableImpl _transformableImpl;

    internal ExternalItem( ExternalTransformableItem item )
    {
        _item = item;
    }

    /// <summary>
    /// Gets the external item itself.
    /// </summary>
    internal ExternalTransformableItem Item => _item;

    TFunction? ITransformable.FirstFunction => _transformableImpl.FirstFunction;

    TFunction? ITransformable.LastFunction => _transformableImpl.LastFunction;

    string ITransformable.TransfomableTargetName => _item.ExternalPath;

    bool ITransformable.TryFindInsertionPoint( IActivityMonitor monitor, FunctionSource source, TransformerFunction f, out TFunction? before )
        => _transformableImpl.TryFindInsertionPoint( monitor, source, f, out before );

    void ITransformable.Add( TFunction f, TFunction? before ) => _transformableImpl.Add( f, before );

    void ITransformable.Remove( TFunction f ) => _transformableImpl.Remove( f );

    internal string? GetTransformedText( IActivityMonitor monitor, TransformerHost transformerHost )
    {
        Throw.DebugAssert( "Otherwise this ExternalItem would not exist.", _transformableImpl.HasFunctions );
        return _transformableImpl.Transform( monitor,
                                             transformerHost,
                                             _item.InitialText.AsMemory(),
                                             idempotenceCheck: true );
    }
}
