using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// See <see cref="TypeScriptFileSaveStrategy.GeneratedDependencies"/>.
    /// </summary>
    public sealed class DependencyCollection : IReadOnlyDictionary<string,PackageDependency>
    {
        readonly Dictionary<string,PackageDependency> _dependencies;
        readonly bool _ignoreVersionsBound;
        int _changeTracker;

        /// <summary>
        /// Initializes a new empty dependency collection.
        /// </summary>
        /// <param name="ignoreVersionsBound">See <see cref="LibraryManager.IgnoreVersionsBound"/>.</param>
        public DependencyCollection( bool ignoreVersionsBound )
        {
            _dependencies = new Dictionary<string, PackageDependency>();
            _ignoreVersionsBound = ignoreVersionsBound;
        }

        /// <summary>
        /// Gets whether this collection skips the version bound check when updating a <see cref="PackageDependency"/>.
        /// See <see cref="LibraryManager.IgnoreVersionsBound"/>.
        /// </summary>
        public bool IgnoreVersionsBound => _ignoreVersionsBound;

        /// <summary>
        /// Incremented whenever a dependency is updated or added.
        /// <para>
        /// Caution: this cannot track direct calls to <see cref="PackageDependency.UnconditionalSetDependencyKind(DependencyKind)"/>
        /// or <see cref="PackageDependency.UnconditionalSetVersion(CSemVer.SVersionBound)"/>.
        /// </para>
        /// </summary>
        public int ChangeTracker => _changeTracker;

        /// <summary>
        /// Removes a dependency.
        /// </summary>
        /// <param name="name">The <see cref="PackageDependency.Name"/> to remove.</param>
        /// <returns>True if it has been removed, false if not found.</returns>
        public bool Remove( string name )
        {
            if( _dependencies.Remove( name ) )
            {
                _changeTracker++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds or updates a dependency to this collection.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="dependency">The dependency to merge.</param>
        /// <param name="cloneAddedDependency">
        /// By default, when the <paramref name="dependency"/> doesn't exist a clone is added in this collection.
        /// Sets this to true to reference the provided instance.
        /// </param>
        /// <returns>True on success, false otherwise.</returns>
        public bool AddOrUpdate( IActivityMonitor monitor, PackageDependency dependency, bool cloneAddedDependency = true )
        {
            if( _dependencies.TryGetValue( dependency.Name, out var our ) )
            {
                if( our.Version != dependency.Version )
                {
                    if( !our.DoUpdate( dependency.Version, _ignoreVersionsBound, out var error, out var _ ) )
                    {
                        monitor.Error( error );
                        return false;
                    }
                    _changeTracker++;
                }
                if( our.DependencyKind != dependency.DependencyKind )
                {
                    our.Update( dependency.DependencyKind );
                    _changeTracker++;
                }
            }
            else
            {
                _dependencies.Add( dependency.Name,
                                   cloneAddedDependency
                                    ? new PackageDependency( dependency.Name, dependency.Version, dependency.DependencyKind )
                                    : dependency );
                ++_changeTracker;
            }
            return true;
        }

        /// <summary>
        /// Merges the <paramref name="dependencies"/> (upgrade existing ones or creates new independent ones) in
        /// this collection.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="dependencies">The dependencies to merge.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool UpdateDependencies( IActivityMonitor monitor, IEnumerable<PackageDependency> dependencies )
        {
            bool success = true;
            foreach( var d in dependencies )
            {
                success &= AddOrUpdate( monitor, d );
            }
            return success;
        }

        /// <inheritdoc />
        public int Count => _dependencies.Count;

        /// <inheritdoc />
        public IEnumerable<string> Keys => _dependencies.Keys;

        /// <inheritdoc />
        public IEnumerable<PackageDependency> Values => _dependencies.Values;

        /// <inheritdoc />
        public PackageDependency this[string key] => _dependencies[key];

        /// <inheritdoc />
        public void Clear() => _dependencies.Clear();

        /// <inheritdoc />
        public bool ContainsKey( string key ) => _dependencies.ContainsKey( key );

        /// <inheritdoc />
        public bool TryGetValue( string key, [MaybeNullWhen( false )] out PackageDependency value ) => _dependencies.TryGetValue( key, out value );

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string,PackageDependency>> GetEnumerator() => _dependencies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dependencies.GetEnumerator();

    }
}
