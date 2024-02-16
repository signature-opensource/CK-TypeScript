using CK.Core;
using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Event arguments that exposes an object for which a <see cref="ITSType"/> must be resolved.
    /// This is raised when a key type that is not a C# type must be resolved.
    /// </summary>
    public sealed class RequireTSFromObjectEventArgs : EventMonitoredArgs
    {
        readonly TSTypeManager _tsTypes;
        readonly object _keyType;
        ITSType? _resolved;
        bool _resolvedByMapping;

        internal RequireTSFromObjectEventArgs( IActivityMonitor monitor, TSTypeManager tSTypes, object keyType )
            : base( monitor )
        {
            _tsTypes = tSTypes;
            _keyType = keyType;
        }

        /// <summary>
        /// Gets the key (not a C# type) to resolve.
        /// </summary>
        public object KeyType => _keyType;

        /// <summary>
        /// Gets the resolved TypeScript type.
        /// </summary>
        public ITSType? ResolvedType => _resolved;

        /// <summary>
        /// Gets whether <see cref="ResolveByMapping(IActivityMonitor, object)"/> has been called.
        /// </summary>
        public bool IsResolvedByMapping => _resolvedByMapping;

        /// <summary>
        /// Sets the resolved type (can be the nullable type).
        /// </summary>
        /// <param name="type">The resolved type.</param>
        public void SetResolvedType( ITSType type )
        {
            Throw.CheckState( ResolvedType == null );
            Throw.CheckNotNullArgument( type );
            _resolved = type;
        }

        /// <summary>
        /// Sets the resolved type by mapping it to another object or C# type resolution.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="keyType">The object or C# type to resolve.</param>
        /// <returns>The resolved type.</returns>
        public ITSType ResolveByMapping( IActivityMonitor monitor, object keyType, bool? isNullable = null )
        {
            Throw.CheckState( ResolvedType == null );
            var ts = _tsTypes.ResolveTSType( monitor, keyType );
            if( isNullable.HasValue )
            {
                ts = isNullable.Value ? ts.Nullable : ts.NonNullable;
            }
            _resolvedByMapping = true;
            _resolved = ts;
            if( keyType is not Type ) _tsTypes.OnResolvedByMapping( keyType, ts );
            return ts;
        }
    }

}

