using CK.BinarySerialization;

namespace CK.Core;

/// <summary>
/// Optional interface for <see cref="ResourceSpaceFolderHandler"/> and <see cref="ResourceSpaceFileHandler"/>
/// that support live updates.
/// </summary>
public interface ILiveResourceSpaceHandler
{
    /// <summary>
    /// Writes the live state in the primary live state file and/or into auxiliary files in the <paramref name="ckWatchFolderPath"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="s">The serializer for the primary <see cref="ResourceSpace.LiveStateFileName"/>.</param>
    /// <param name="ckWatchFolderPath">Fully qualified path ending with <see cref="System.IO.Path.DirectorySeparatorChar"/>.</param>
    /// <returns>True on success, false on error. Errorrs must be logged.</returns>
    bool WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, string ckWatchFolderPath );

    /// <summary>
    /// Restores a <see cref="ILiveUpdater"/> from previously written data by <see cref="WriteLiveState(IActivityMonitor, IBinarySerializer, string)"/>.
    /// Nothing prevents the live updater to be implemented by this handler.
    /// <para>
    /// This SHOULD be <c>static abstract</c> but because of <see href="https://github.com/dotnet/csharplang/issues/5955"/> this is only a
    /// <c>static virtual</c> that returns null by default.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="data">The deserialized resource space data.</param>
    /// <param name="d">The deserializer for the primary <see cref="ResourceSpace.LiveStateFileName"/>.</param>
    /// <returns>The live updater on success, null on error. Errors must be logged.</returns>
    public static virtual ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResourceSpaceData data, IBinaryDeserializer d ) => null;
}
