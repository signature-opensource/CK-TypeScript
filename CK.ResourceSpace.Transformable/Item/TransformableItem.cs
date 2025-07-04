using CK.EmbeddedResources;
using CK.Transform.Core;
using System;

namespace CK.Core;

/// <summary>
/// A transformable item is <see cref="IResourceInput"/> and a <see cref="ITransformable"/>.
/// This concrete class applies to stable items. <see cref="LocalItem"/> specializes it.
/// <para>
/// This is exposed publicly only through the <see cref="ITransformInstallableItem"/> interface.
/// </para>
/// </summary>
partial class TransformableItem : IResourceInput, ITransformable, ITransformInstallableItem
{
    readonly IResPackageResources _resources;
    protected readonly string _fullResourceName;
    TFunction.TransformableImpl _transformableImpl;
    internal TransformableItem? _nextInPackage;
    internal TransformableItem? _prevInPackage;
    string _text;
    readonly NormalizedPath _targetPath;
    readonly int _languageIndex;

    public TransformableItem( IResPackageResources resources,
                              string fullResourceName,
                              string text,
                              int languageIndex,
                              NormalizedPath targetPath )
    {
        Throw.DebugAssert( languageIndex >= 0 );
        _resources = resources;
        _fullResourceName = fullResourceName;
        _languageIndex = languageIndex;
        _text = text;
        _targetPath = targetPath;
    }

    public IResPackageResources Resources => _resources;

    public ResourceLocator Origin => new ResourceLocator( _resources.Resources, _fullResourceName );

    public NormalizedPath TargetPath => _targetPath;

    public virtual bool IsLocalItem => false;

    public int LanguageIndex => _languageIndex;

    public string Text => _text;

    public TFunction? FirstFunction => _transformableImpl.FirstFunction;

    string ITransformable.TransfomableTargetName => _targetPath.Path;

    TFunction? ITransformable.LastFunction => _transformableImpl.LastFunction;

    bool ITransformable.TryFindInsertionPoint( IActivityMonitor monitor, FunctionSource source, TransformerFunction f, out TFunction? before )
        => _transformableImpl.TryFindInsertionPoint( monitor, source, f, out before );

    void ITransformable.Add( TFunction f, TFunction? before ) => _transformableImpl.Add( f, before );

    void ITransformable.Remove( TFunction f ) => _transformableImpl.Remove( f );

    protected void SetText( string text ) => _text = text;

    internal string? GetFinalText( IActivityMonitor monitor, TransformerHost transformerHost )
    {
        if( _transformableImpl.HasFunctions )
        {
            return _transformableImpl.Transform( monitor, transformerHost, Text.AsMemory() );
        }
        return Text;
    }
}
