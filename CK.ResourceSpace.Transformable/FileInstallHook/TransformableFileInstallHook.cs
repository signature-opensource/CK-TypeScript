using CK.BinarySerialization;
using CK.Transform.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core;

/// <summary>
/// Required hook implementation on the setup side: Live hooks can be simple <see cref="ITransformableFileInstallHook"/>.
/// </summary>
public abstract class TransformableFileInstallHook : ITransformableFileInstallHook
{
    TransformerHost? _transformerHost;
    ResSpaceData? _data;

    internal void Initialize( ResSpaceData data, TransformerHost host )
    {
        _data = data;
        _transformerHost = host;
    }

    /// <summary>
    /// Gets whether this hook is initialized.
    /// </summary>
    [MemberNotNullWhen( true, nameof( TransformerHost ), nameof( SpaceData ) )]
    protected bool IsInitialized => _transformerHost != null;

    /// <summary>
    /// Gets the <see cref="SpaceData"/>.
    /// <para>
    /// <see cref="IsInitialized"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    protected ResSpaceData? SpaceData
    {
        get
        {
            Throw.CheckState( IsInitialized );
            return _data;
        }
    }

    /// <summary>
    /// Gets the transformer host.
    /// <para>
    /// <see cref="IsInitialized"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    protected TransformerHost? TransformerHost
    {
        get
        {
            Throw.CheckState( IsInitialized );
            return _transformerHost;
        }
    }

    /// <inheritdoc />
    public abstract void StartInstall( IActivityMonitor monitor );

    /// <inheritdoc />
    public abstract bool HandleInstall( IActivityMonitor monitor,
                                        ITransformInstallableItem item,
                                        string finalText,
                                        IResourceSpaceItemInstaller installer,
                                        out bool handled );

    /// <inheritdoc />
    public abstract void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer );

    /// <summary>
    /// Gets whether <see cref="WriteLiveState(IBinarySerializer)"/> must be called.
    /// Defaults to true: by default if <see cref="ResSpaceData.HasLiveState"/> is true, a hook must be able
    /// to restore a Live <see cref="ITransformableFileInstallHook"/>.
    /// <para>
    /// This is never called when <see cref="ResSpaceData.HasLiveState"/> is false.
    /// </para>
    /// </summary>
    /// <returns></returns>
    internal protected virtual bool ShouldWriteLiveState => true;

    /// <summary>
    /// Writes the live state in the primary live state file.
    /// This is called when <see cref="ResSpaceData.HasLiveState"/> and <see cref="ShouldWriteLiveState"/> are both true.
    /// <para>
    /// A public static method with the following signature must be defined:
    /// <code>
    /// 
    ///     /// &lt;summary&gt;
    ///     /// Restores a &lt;see cref="ITransformableFileInstallHook"/&gt; from previously written data by WriteLiveState.
    ///     /// Nothing prevents the live updater to be implemented by this handler.
    ///     /// &lt;/para&gt;
    ///     /// &lt;/summary&gt;
    ///     /// &lt;param name="monitor"&gt;The monitor to use.&lt;/param&gt;
    ///     /// &lt;param name="d"&gt;The deserializer.&lt;/param&gt;
    ///     /// &lt;returns&gt;The live updater on success, null on error. Errors must be logged.&lt;/returns&gt;
    ///     public static ITransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor,
    ///                                                                 IBinaryDeserializer d )
    ///     { ... }
    ///
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="s">The serializer for the primary <see cref="ResSpace.LiveStateFileName"/>.</param>
    internal protected abstract void WriteLiveState( IBinarySerializer s );



}
