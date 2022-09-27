
using CK.Core;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Types adapter: this folder exposes a typed <see cref="Root"/> and
    /// works with similar typed folders <see cref="TypeScriptFile{TRoot}"/>.
    /// </summary>
    /// <typeparam name="TRoot">The actual type of the root.</typeparam>
    public sealed class TypeScriptFolder<TRoot> : TypeScriptFolder
        where TRoot : TypeScriptRoot
    {
        TypeScriptFolder( TRoot root )
            : base( root )
        {
        }

        static internal TypeScriptFolder Create( TRoot root ) => new TypeScriptFolder<TRoot>( root );

        internal TypeScriptFolder( TypeScriptFolder<TRoot> parent, string name )
            : base( parent, name )
        {
        }

        /// <inheritdoc cref="TypeScriptFolder.Root" />
        public new TRoot Root => (TRoot)base.Root;

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFolder(NormalizedPath)" />
        public new TypeScriptFolder<TRoot>? FindOrCreateFolder( NormalizedPath path ) => (TypeScriptFolder<TRoot>?)base.FindOrCreateFolder( path );

        private protected override TypeScriptFolder CreateFolder( string name ) => new TypeScriptFolder<TRoot>( this, name );

        /// <inheritdoc cref="TypeScriptFolder.FindFolder(string)" />
        public new TypeScriptFolder<TRoot>? FindFolder( string name ) => (TypeScriptFolder<TRoot>?)base.FindFolder( name );

        /// <inheritdoc cref="TypeScriptFolder.Folders" />
        public new IEnumerable<TypeScriptFolder<TRoot>> Folders => (IEnumerable<TypeScriptFolder<TRoot>>)base.Folders;

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(string)" />
        public new TypeScriptFile<TRoot> FindOrCreateFile( string name ) => (TypeScriptFile<TRoot>)base.FindOrCreateFile( name );

        private protected override TypeScriptFile CreateFile( string name ) => new TypeScriptFile<TRoot>( this, name );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(string, out bool)" />
        public new TypeScriptFile<TRoot> FindOrCreateFile( string name, out bool created ) => (TypeScriptFile<TRoot>)base.FindOrCreateFile( name, out created );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(NormalizedPath)" />
        public new TypeScriptFile<TRoot> FindOrCreateFile( NormalizedPath filePath ) => (TypeScriptFile<TRoot>)base.FindOrCreateFile( filePath );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(NormalizedPath, out bool)" />
        public new TypeScriptFile<TRoot> FindOrCreateFile( NormalizedPath filePath, out bool created ) => (TypeScriptFile<TRoot>)base.FindOrCreateFile( filePath, out created );

        /// <inheritdoc cref="TypeScriptFolder.FindFile(string)" />
        public new TypeScriptFile<TRoot>? FindFile( string name ) => (TypeScriptFile<TRoot>?)base.FindFile( name );

        /// <inheritdoc cref="TypeScriptFolder.Files" />
        public new IEnumerable<TypeScriptFile<TRoot>> Files => (IEnumerable<TypeScriptFile<TRoot>>)base.Files;
    }

}
