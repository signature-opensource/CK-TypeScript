using CK.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Optional interface for <see cref="ResourceSpaceFolderHandler"/> and <see cref="ResourceSpaceFileHandler"/>
/// that support live updates.
/// </summary>
interface ILiveResourceSpaceHandler
{
    /// <summary>
    /// Writes the live state in the primary live state file and/or in auxiliary files.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="s">The serializer for the primary <see cref="ResourceSpace.LiveStateFileName"/>.</param>
    /// <param name="ckWatchFolderPath">Fully qualified path ending with <see cref="System.IO.Path.DirectorySeparatorChar"/>.</param>
    /// <returns>True on success, false on error. Errorrs must be logged.</returns>
    bool WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, string ckWatchFolderPath );

    /// <summary>
    /// Restores a <see cref="ILiveUpdater"/> from previously written data by <see cref="WriteLiveState(IActivityMonitor, IBinarySerializer, string)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="d">The deserializer for the primary <see cref="ResourceSpace.LiveStateFileName"/>.</param>
    /// <param name="ckWatchFolderPath">Fully qualified path ending with <see cref="System.IO.Path.DirectorySeparatorChar"/>.</param>
    /// <returns>The live updater on success, null on error. Errorrs must be logged.</returns>
    ILiveUpdater? ReadLiveState( IActivityMonitor monitor, IBinaryDeserializer d, string ckWatchFolderPath );
}
