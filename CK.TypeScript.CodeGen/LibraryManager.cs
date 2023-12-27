using CK.Core;
using System.Collections.Generic;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Centrally manages <see cref="LibraryImport"/>.
    /// </summary>
    public sealed class LibraryManager
    {
        readonly Dictionary<string, LibraryImport> _libraries;

        internal LibraryManager()
        {
            _libraries = new Dictionary<string, LibraryImport>();
        }

        /// <summary>
        /// Imported external library used by the generated code.
        /// </summary>
        public IReadOnlyDictionary<string, LibraryImport> LibraryImports => _libraries;

        /// <summary>
        /// Ensures that an external library will be present in the project.
        /// </summary>
        /// <param name="lib">The library infos.</param>
        /// <returns>This section to enable fluent syntax.</returns>
        public void EnsureLibrary( LibraryImport lib )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( lib.Name );
            if( !_libraries.TryGetValue( lib.Name, out var exists ) )
            {
                _libraries[lib.Name] = lib;
            }
            else
            {
                if( exists.Version != lib.Version )
                {
                    Throw.InvalidOperationException( $"Previously imported this library at version {exists.Version}, but currently importing it with version {lib.Version}" );
                }
                if( exists.DependencyKind < lib.DependencyKind )
                {
                    _libraries[lib.Name] = lib;
                }
            }
            foreach( var d in lib.ImpliedDependencies )
            {
                EnsureLibrary( d );
            }
        }
    }
}
