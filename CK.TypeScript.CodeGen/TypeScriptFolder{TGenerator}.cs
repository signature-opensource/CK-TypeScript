
using CK.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Types adapter: this folder exposes a typed <see cref="Generator"/> and
    /// works with similar typed folders <see cref="TypeScriptFile{TGenerator}"/>.
    /// </summary>
    /// <typeparam name="TGenerator">The actual type of the root.</typeparam>
    public sealed class TypeScriptFolder<TGenerator> : TypeScriptFolder
        where TGenerator : TypeScriptGenerator
    {
        TypeScriptFolder( TGenerator root )
            : base( root )
        {
        }

        static internal TypeScriptFolder Create( TGenerator root ) => new TypeScriptFolder<TGenerator>( root );

        internal TypeScriptFolder( TypeScriptFolder<TGenerator> parent, string name )
            : base( parent, name )
        {
        }

        /// <inheritdoc cref="TypeScriptFolder.Generator" />
        public new TGenerator Generator => Unsafe.As<TGenerator>( base.Generator );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFolder(NormalizedPath)" />
        public new TypeScriptFolder<TGenerator> FindOrCreateFolder( NormalizedPath path ) => Unsafe.As<TypeScriptFolder<TGenerator>>( base.FindOrCreateFolder( path ) );

        private protected override TypeScriptFolder CreateFolder( string name ) => new TypeScriptFolder<TGenerator>( this, name );

        /// <inheritdoc cref="TypeScriptFolder.FindFolder(string)" />
        public new TypeScriptFolder<TGenerator>? FindFolder( string name ) => Unsafe.As<TypeScriptFolder<TGenerator>?>( base.FindFolder( name ) );

        /// <inheritdoc cref="TypeScriptFolder.Folders" />
        public new IEnumerable<TypeScriptFolder<TGenerator>> Folders => Unsafe.As<IEnumerable<TypeScriptFolder<TGenerator>>>( base.Folders );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(string)" />
        public new TypeScriptFile<TGenerator> FindOrCreateFile( string name ) => Unsafe.As<TypeScriptFile<TGenerator>>( base.FindOrCreateFile( name ) );

        private protected override TypeScriptFile CreateFile( string name ) => new TypeScriptFile<TGenerator>( this, name );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(string, out bool)" />
        public new TypeScriptFile<TGenerator> FindOrCreateFile( string name, out bool created ) => Unsafe.As<TypeScriptFile<TGenerator>>( base.FindOrCreateFile( name, out created ) );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(NormalizedPath)" />
        public new TypeScriptFile<TGenerator> FindOrCreateFile( NormalizedPath filePath ) => Unsafe.As<TypeScriptFile<TGenerator>>( base.FindOrCreateFile( filePath ) );

        /// <inheritdoc cref="TypeScriptFolder.FindOrCreateFile(NormalizedPath, out bool)" />
        public new TypeScriptFile<TGenerator> FindOrCreateFile( NormalizedPath filePath, out bool created ) => Unsafe.As<TypeScriptFile<TGenerator>>( base.FindOrCreateFile( filePath, out created ) );

        /// <inheritdoc cref="TypeScriptFolder.FindFile(string)" />
        public new TypeScriptFile<TGenerator>? FindFile( string name ) => Unsafe.As<TypeScriptFile<TGenerator>?>( base.FindFile( name ) );

        /// <inheritdoc cref="TypeScriptFolder.Files" />
        public new IEnumerable<TypeScriptFile<TGenerator>> Files => Unsafe.As<IEnumerable<TypeScriptFile<TGenerator>>>( base.Files );

        /// <inheritdoc cref="TypeScriptFolder.AllFilesRecursive" />
        public new IEnumerable<TypeScriptFile<TGenerator>> AllFilesRecursive => Files.Concat( Folders.SelectMany( s => s.AllFilesRecursive ) );

    }

}
