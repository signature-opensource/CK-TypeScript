using CK.Core;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Event arguments that exposes an object for which a <see cref="ITSType"/> must be resolved.
    /// This is raised when a key type that is not a C# type must be resolved.
    /// </summary>
    public sealed class RequireTSFromObjectEventArgs : EventMonitoredArgs
    {
        readonly object _keyType;
        ITSType? _resolved;

        internal RequireTSFromObjectEventArgs( IActivityMonitor monitor, object keyType )
            : base( monitor )
        {
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
        /// Sets the resolved type (can be the nullable type).
        /// </summary>
        /// <param name="type">The resolved type.</param>
        public void SetResolvedType( ITSType type )
        {
            Throw.CheckState( ResolvedType == null );
            Throw.CheckNotNullArgument( type );
            _resolved = type;
        }
    }

}

