using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Folder in a <see cref="TypeScriptRoot.Root"/>.
    /// <para>
    /// This is the base class and non generic version of <see cref="TypeScriptFolder{TRoot}"/>.
    /// </para>
    /// </summary>
    public class TypeScriptFolder
    {
        readonly TypeScriptRoot _root;
        TypeScriptFolder? _firstChild;
        TypeScriptFolder? _next;
        internal TypeScriptFile? _firstFile;
        NormalizedPath _path;
        bool _hasBarrel;

        static readonly char[] _invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();

        internal TypeScriptFolder( TypeScriptRoot root )
        {
            _root = root;
        }

        internal TypeScriptFolder( TypeScriptFolder parent, string name )
        {
            _root = parent._root;
            Parent = parent;
            _path = parent._path.AppendPart( name );
            _next = parent._firstChild;
            parent._firstChild = this;
        }

        /// <summary>
        /// Gets this folder's name.
        /// This string is empty when this is the <see cref="TypeScriptRoot.Root"/>, otherwise
        /// it necessarily not empty without '.ts' extension.
        /// </summary>
        public string Name => _path.LastPart;

        /// <summary>
        /// Gets this folder's path from <see cref="Root"/>.
        /// </summary>
        public NormalizedPath Path => _path;

        /// <summary>
        /// Gets whether this folder is the root one.
        /// </summary>
        public bool IsRoot => _path.IsEmptyPath;

        /// <summary>
        /// Gets the parent folder. Null when this is the <see cref="TypeScriptRoot.Root"/>.
        /// </summary>
        public TypeScriptFolder? Parent { get; }

        /// <summary>
        /// Gets the root TypeScript context.
        /// </summary>
        public TypeScriptRoot Root => _root;

        /// <summary>
        /// Gets whether this folder has a barrel (see https://basarat.gitbook.io/typescript/main-1/barrel).
        /// </summary>
        public bool HasBarrel => _hasBarrel;

        /// <summary>
        /// Definitely sets <see cref="HasBarrel"/> to true.
        /// </summary>
        public void EnsureBarrel() => _hasBarrel = true;

        /// <summary>
        /// Finds or creates a folder.
        /// </summary>
        /// <param name="name">The folder's name to find or create. Must not be empty nor ends with '.ts'.</param>
        /// <returns>The folder.</returns>
        public TypeScriptFolder FindOrCreateFolder( string name ) => FindFolder( name ) ?? CreateFolder( name );

        private protected virtual TypeScriptFolder CreateFolder( string name )
        {
            // No need to CheckName here: FindFolder did the job.
            var f = new TypeScriptFolder( this, name );
            _root.OnFolderCreated( f );
            return f;
        }

        /// <summary>
        /// Finds or creates a subordinated folder by its path.
        /// </summary>
        /// <param name="path">The folder's path to find or create. None of its parts must end with '.ts'.</param>
        /// <returns>The folder.</returns>
        public TypeScriptFolder FindOrCreateFolder( NormalizedPath path )
        {
            var f = this;
            foreach( var name in path.Parts )
            {
                f = f.FindOrCreateFolder( name );
            }
            return f;
        }

        /// <summary>
        /// Finds an existing folder (direct child) or returns null.
        /// </summary>
        /// <param name="name">The folder's name. Must not be empty nor ends with '.ts'.</param>
        /// <returns>The existing folder or null.</returns>
        public TypeScriptFolder? FindFolder( string name )
        {
            CheckName( name, true );
            var c = _firstChild;
            while( c != null )
            {
                if( c.Name == name ) return c;
                c = c._next;
            }
            return null;
        }

        /// <summary>
        /// Finds an existing folder by its path or returns null.
        /// </summary>
        /// <param name="subPath">The path to the subordinated folder to find.</param>
        /// <returns>The existing folder or null.</returns>
        public TypeScriptFolder? FindFolder( NormalizedPath subPath )
        {
            var f = this;
            foreach( var name in subPath.Parts )
            {
                f = f.FindFolder( name );
                if( f == null ) return null;
            }
            return f;
        }

        /// <summary>
        /// Gets all the subordinated folders.
        /// </summary>
        public IEnumerable<TypeScriptFolder> Folders
        {
            get
            {
                var c = _firstChild;
                while( c != null )
                {
                    yield return c;
                    c = c._next;
                }
            }
        }

        /// <summary>
        /// Gets the files that this folder contains.
        /// </summary>
        public IEnumerable<TypeScriptFile> Files
        {
            get
            {
                var c = _firstFile;
                while( c != null )
                {
                    yield return c;
                    c = c._next;
                }
            }
        }

        /// <summary>
        /// Gets all the files that this folder and its sub folders contain.
        /// </summary>
        public IEnumerable<TypeScriptFile> AllFilesRecursive => Files.Concat( Folders.SelectMany( s => s.AllFilesRecursive ) );

        /// <summary>
        /// Finds or creates a file in this folder.
        /// </summary>
        /// <param name="name">The file's name to find or create. Must not be empty and must end with '.ts'.</param>
        /// <returns>The file.</returns>
        public TypeScriptFile FindOrCreateFile( string name ) => FindFile( name ) ?? CreateFile( name );

        private protected virtual TypeScriptFile CreateFile( string name )
        {
            var f = new TypeScriptFile( this, name );
            _root.OnFileCreated( f );
            return f;
        }

        /// <summary>
        /// Finds or creates a file in this folder.
        /// </summary>
        /// <param name="name">The file's name to find or create. Must not be empty and must end with '.ts'.</param>
        /// <param name="created">True if the file has been created, false if it already existed.</param>
        /// <returns>The file.</returns>
        public TypeScriptFile FindOrCreateFile( string name, out bool created )
        {
            created = false;
            TypeScriptFile? f = FindFile( name );
            if( f == null )
            {
                f = CreateFile( name );
                created = true;
            }
            return f;
        }

        /// <summary>
        /// Finds or creates a file in this folder or a subordinated folder.
        /// </summary>
        /// <param name="filePath">The file's full path to find or create. The <see cref="NormalizedPath.LastPart"/> must end with '.ts'.</param>
        /// <returns>The file.</returns>
        public TypeScriptFile FindOrCreateFile( NormalizedPath filePath )
        {
            return FindOrCreateFolder( filePath.RemoveLastPart() ).FindOrCreateFile( filePath.LastPart );
        }

        /// <summary>
        /// Finds or creates a file in this folder or a subordinated folder.
        /// </summary>
        /// <param name="filePath">The file's full path to find or create. The <see cref="NormalizedPath.LastPart"/> must end with '.ts'.</param>
        /// <param name="created">True if the file has been created, false if it already existed.</param>
        /// <returns>The file.</returns>
        public TypeScriptFile FindOrCreateFile( NormalizedPath filePath, out bool created )
        {
            return FindOrCreateFolder( filePath.RemoveLastPart() ).FindOrCreateFile( filePath.LastPart, out created );
        }

        /// <summary>
        /// Finds an existing file in this folder or returns null.
        /// </summary>
        /// <param name="name">The file's name. Must not be empty and must end with '.ts'.</param>
        /// <returns>The existing file or null.</returns>
        public TypeScriptFile? FindFile( string name )
        {
            CheckName( name, false );
            var c = _firstFile;
            while( c != null )
            {
                if( c.Name == name ) return c;
                c = c._next;
            }
            return null;
        }

        /// <summary>
        /// Finds a folder below this one by returning its depth.
        /// This returns 0 when this is the same as <paramref name="other"/>
        /// and -1 if other is not subordinated to this folder.
        /// </summary>
        /// <param name="other">The other folder to locate.</param>
        /// <returns>The depth of the other folder below this one.</returns>
        public int FindBelow( TypeScriptFolder other )
        {
            int depth = 0;
            TypeScriptFolder? c = other;
            while( c != this )
            {
                ++depth;
                c = c.Parent;
                if( c == null ) return -1;
            }
            return depth;
        }

        /// <summary>
        /// Gets a relative path from this folder to another one.
        /// This folder and the other one must belong to the same <see cref="TypeScriptRoot"/>
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="f">The folder to target.</param>
        /// <returns>The relative path from this one to the other one.</returns>
        public NormalizedPath GetRelativePathTo( TypeScriptFolder f )
        {
            bool firstLook = true;
            var source = this;
            NormalizedPath result = new NormalizedPath( "." );
            do
            {
                int below = source.FindBelow( f );
                if( below >= 0 )
                {
                    var p = BuildPath( f, result, below );
                    return p;
                }
                result = firstLook ? new NormalizedPath( ".." ) : result.AppendPart( ".." );
                firstLook = false;
            }
            while( (source = source.Parent!) != null );
            throw new InvalidOperationException( "TypeScriptFolder must belong to the same TypeScriptRoot." );

            static NormalizedPath BuildPath( TypeScriptFolder f, NormalizedPath result, int below )
            {
                if( below == 0 ) return result;
                if( below == 1 ) return result.AppendPart( f.Name );
                var path = new string[below];
                var idx = path.Length;
                do
                {
                    path[--idx] = f.Name;
                    f = f.Parent!;
                }
                while( idx > 0 );
                foreach( var p in path ) result = result.AppendPart( p );
                return result;
            }
        }

        /// <summary>
        /// Saves this folder, its files and all its subordinated folders, into a folder on the file system.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="outputPath">Target directory.</param>
        /// <param name="previousPaths">
        /// Optional set of file paths from which actually saved paths will be removed:
        /// what's left will be the actual generated paths.
        /// </param>
        /// <returns>Number of files saved on success, null if an error occurred (the error has been logged).</returns>
        public int? Save( IActivityMonitor monitor, NormalizedPath outputPath, HashSet<string>? previousPaths  )
        {
            using( monitor.OpenTrace( $"Saving {(IsRoot ? $"TypeScript Root folder into {outputPath}" : Name)}." ) )
            {
                try
                {
                    int result = 0;
                    var target = IsRoot ? outputPath : outputPath.AppendPart( Name );

                    bool createdDirectory = false;
                    if( _firstFile != null )
                    {
                        Directory.CreateDirectory( target );
                        createdDirectory = true;
                        foreach( var file in Files )
                        {
                            file.Save( monitor, target, previousPaths );
                            ++result;
                        }
                    }
                    var folder = _firstChild;
                    while( folder != null )
                    {
                        var r = folder.Save( monitor, target, previousPaths );
                        if( !r.HasValue ) return null;
                        result += r.Value;
                        folder = folder._next;
                    }
                    if( _hasBarrel )
                    {
                        var b = new StringBuilder();
                        AddExportsToBarrel( default, b );
                        if( b.Length > 0 )
                        {
                            if( !createdDirectory ) Directory.CreateDirectory( target );

                            var index = target.AppendPart( "index.ts" );
                            File.WriteAllText( index, b.ToString() );
                            previousPaths?.Remove( index );
                            ++result;
                        }
                    }
                    return result;
                }
                catch( Exception ex )
                {
                    monitor.Error( ex );
                    return null;
                }
            }
        }

        void AddExportsToBarrel( NormalizedPath subPath, StringBuilder b )
        {
            if( !subPath.IsEmptyPath && _hasBarrel )
            {
                b.Append( "export * from './" ).Append( subPath ).AppendLine( "';" );
            }
            else
            {
                var file = _firstFile;
                while( file != null )
                {
                    AddExportFile( subPath, b, file.Name.AsSpan().Slice( 0, file.Name.Length - 3 ) );
                    file = file._next;
                }
                var folder = _firstChild;
                while( folder != null )
                {
                    folder.AddExportsToBarrel( subPath.AppendPart( folder.Name ), b );
                    folder = folder._next;
                }

                static void AddExportFile( NormalizedPath subPath, StringBuilder b, ReadOnlySpan<char> fileName )
                {
                    b.Append( "export * from './" ).Append( subPath );
                    if( !subPath.IsEmptyPath ) b.Append( '/' );
                    b.Append( fileName ).AppendLine( "';" );
                }
            }
        }

        static void CheckName( string name, bool isFolder )
        {
            if( String.IsNullOrWhiteSpace( name ) || name.IndexOfAny( _invalidFileNameChars ) >= 0 ) Throw.ArgumentException( $"Empty name or invalid characters in: '{name}'.", nameof( name ) );
            if( name.EndsWith( ".ts", StringComparison.OrdinalIgnoreCase ) )
            {
                if( isFolder ) Throw.ArgumentException( $"Folder name must not end with '.ts': '{name}'.", nameof( name ) );
            }
            else
            {
                if( !isFolder ) Throw.ArgumentException( $"File name must end with '.ts': '{name}'.", nameof( name ) );
            }
        }
    }
}
