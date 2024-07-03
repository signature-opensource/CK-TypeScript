using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Template method that centralizes deferred files cleanup and
    /// provides a hook when saving <see cref="TypeScriptFile"/>.
    /// <para>
    /// Deferred files cleanup minimizes impacts on file watchers: we don't destroy/recreate the
    /// target folder. Instead we update the existing files in place and then remove any files that
    /// have not been generated.
    /// </para>
    /// <para>
    /// There is no way to stop a save operation in the middle: the whole <see cref="TypeScriptRoot.Root"/> will be
    /// traversed. Any save state must be implemented in a specialized of this strategy.
    /// </para>
    /// </summary>
    public class TypeScriptFileSaveStrategy
    {
        readonly NormalizedPath _target;
        // Avoids a reallocation.
        internal NormalizedPath _currentTarget;
        readonly HashSet<string>? _cleanupFiles;
        readonly DependencyCollection _dependencies;

        /// <summary>
        /// Initializes a new strategy. The <paramref name="targetPath"/> is a folder that may not exist
        /// and must be totally dedicated to the automatic TypeScript generation: by default, any existing
        /// file that has not been regenrated will be deleted.
        /// </summary>
        /// <param name="targetPath">The folder that will reflect the <see cref="TypeScriptRoot"/> content.</param>
        /// <param name="withCleanupFiles">False to keep existing files as-is.</param>
        public TypeScriptFileSaveStrategy( TypeScriptRoot root, NormalizedPath targetPath, bool withCleanupFiles = true )
        {
            Throw.CheckNotNullArgument( root );
            _dependencies = new DependencyCollection( root.LibraryManager.IgnoreVersionsBound );
            _target = targetPath;
            if( withCleanupFiles ) _cleanupFiles = new HashSet<string>();
        }

        /// <summary>
        /// The full path of the target folder.
        /// </summary>
        public NormalizedPath Target => _target;

        /// <summary>
        /// Gets the mutable set of file path that will be deleted by <see cref="Finalize(IActivityMonitor)"/>.
        /// <para>
        /// This set can be altered. By clearing it, no existing files will be removed.
        /// </para>
        /// </summary>
        public HashSet<string>? CleanupFiles => _cleanupFiles;

        /// <summary>
        /// Gets the dependencies that the generated code requires.
        /// <para>
        /// This is updated by <see cref="TypeScriptRoot.Save(IActivityMonitor, TypeScriptFileSaveStrategy)"/>.
        /// </para>
        /// </summary>
        public DependencyCollection GeneratedDependencies => _dependencies;

        /// <summary>
        /// Called by <see cref="TypeScriptRoot.Save(IActivityMonitor, TypeScriptFileSaveStrategy)"/> before saving
        /// the <see cref="TypeScriptRoot.Root"/> folder and all its files.
        /// <para>
        /// At this level this discovers the exisitng files so that <see cref="Finalize(IActivityMonitor)"/> can remove
        /// the remaining ones.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success (always true at this level), false on error (errors must be logged).</returns>
        public virtual bool Initialize( IActivityMonitor monitor )
        {
            if( _cleanupFiles != null && Directory.Exists( _target ) )
            {
                var previous = Directory.EnumerateFiles( _target, "*", SearchOption.AllDirectories );
                if( Path.DirectorySeparatorChar != NormalizedPath.DirectorySeparatorChar )
                {
                    previous = previous.Select( p => p.Replace( Path.DirectorySeparatorChar, NormalizedPath.DirectorySeparatorChar ) );
                }
                _cleanupFiles.AddRange( previous );
                monitor.Trace( $"Found {_cleanupFiles.Count} existing files in '{_target}'." );
            }
            return true;
        }

        /// <summary>
        /// At this level simply writes the <see cref="TypeScriptFile.Imports"/> and <see cref="TypeScriptFile.Body"/>
        /// and removes the <paramref name="filePath"/> from <see cref="CleanupFiles"/>.
        /// <para>
        /// This may be overridden to write altered content (or to skip writing).
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="file">The file to save.</param>
        /// <param name="filePath">The full file target path.</param>
        public virtual void SaveFile( IActivityMonitor monitor, TypeScriptFile file, NormalizedPath filePath )
        {
            monitor.Trace( $"Saving '{file.Name}'." );
            File.WriteAllText( filePath, file.GetCurrentText() );
            _cleanupFiles?.Remove( filePath );
        }

        /// <summary>
        /// Called by <see cref="TypeScriptRoot.Save(IActivityMonitor, TypeScriptFileSaveStrategy)"/> after having saved
        /// all the files.
        /// <para>
        /// At this level, deletes the files that remain in <see cref="CleanupFiles"/> on success (<paramref name="savedCount"/>
        /// is not null).
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>The <paramref name="savedCount"/>, null on error (errors must be logged).</returns>
        public virtual int? Finalize( IActivityMonitor monitor, int? savedCount )
        {
            if( savedCount.HasValue && _cleanupFiles != null )
            {
                if( _cleanupFiles.Count == 0 )
                {
                    monitor.Info( "No previous file exist that have not been regenerated." );
                }
                else
                {
                    using( monitor.OpenInfo( $"Deleting {_cleanupFiles.Count} previous files." ) )
                    {
                        foreach( var p in _cleanupFiles )
                        {
                            monitor.Debug( $"Deleting '{p.AsSpan( _target.Path.Length )}'." );
                            try
                            {
                                if( File.Exists( p ) ) File.Delete( p );
                            }
                            catch( Exception ex )
                            {
                                monitor.Error( $"While deleting '{p}'. Ignoring.", ex );
                            }
                        }
                    }
                }
            }
            return savedCount;
        }
    }


}
