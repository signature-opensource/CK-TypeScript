using CK.Core;
using System;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Publish target of <see cref="TypeScriptRoot.Publish(IActivityMonitor, ITypeScriptPublishTarget)"/>.
/// </summary>
public interface ITypeScriptPublishTarget
{
    /// <summary>
    /// Called before the publication. <see cref="TypeScriptFolder.PublishedFileCount"/> can be used to
    /// prepare this target.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="root">The published root.</param>
    /// <returns>True on success, false if publishing must be cancelled for any reason (that must be logged).</returns>
    bool Open( IActivityMonitor monitor, TypeScriptRoot root );

    /// <summary>
    /// Adds each file with its final content. The path are lexicographically sorted (<see cref="StringComparer.Ordinal"/>).
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="content">The file content.</param>
    void Add( ReadOnlySpan<char> path, string content );

    /// <summary>
    /// Called after the publication.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="root">The published root.</param>
    /// <param name="ex">The error that occurred if any. This error has already been logged.</param>
    /// <returns>
    /// True on success, false on error. This is ignored if <paramref name="ex"/>
    /// is not null (<see cref="TypeScriptRoot.Publish(IActivityMonitor, ITypeScriptPublishTarget)"/> always returns false).
    /// </returns>
    bool Close( IActivityMonitor monitor, TypeScriptRoot root, Exception? ex );
}
