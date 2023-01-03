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
    /// Folder in a <see cref="TypeScriptGenerator.Root"/>.
    /// <para>
    /// This is the base class and non generic version of <see cref="TypeScriptFolder{TRoot}"/>.
    /// </para>
    /// </summary>
    public class TypeScriptFolder
    {
        readonly TypeScriptGenerator _generator;
        TypeScriptFolder? _firstChild;
        TypeScriptFolder? _next;
        internal TypeScriptFile? _firstFile;

        static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

        internal TypeScriptFolder( TypeScriptGenerator g )
        {
            _generator = g;
        }

        internal TypeScriptFolder( TypeScriptFolder parent, string name )
        {
            _generator = parent._generator;
            Parent = parent;
            _next = parent._firstChild;
            parent._firstChild = this;
            FullPath = parent.FullPath.AppendPart( name );
        }

        /// <summary>
        /// Gets this folder's name.
        /// This string is empty when this is the <see cref="TypeScriptGenerator.Root"/>, otherwise
        /// it necessarily not empty and without '.ts' extension.
        /// </summary>
        public string Name => FullPath.LastPart;

        /// <summary>
        /// Gets this folder's full path.
        /// </summary>
        public NormalizedPath FullPath { get; }

        /// <summary>
        /// Gets whether this folder is the root one.
        /// </summary>
        public bool IsRoot => FullPath.IsEmptyPath;

        /// <summary>
        /// Gets the parent folder. Null when this is the <see cref="TypeScriptGenerator.Root"/>.
        /// </summary>
        public TypeScriptFolder? Parent { get; }

        /// <summary>
        /// Gets the TypeScript generator.
        /// </summary>
        public TypeScriptGenerator Generator => _generator;

        /// <summary>
        /// Finds or creates a folder.
        /// </summary>
        /// <param name="name">The folder's name to find or create. Must not be empty nor ends with '.ts'.</param>
        /// <returns>The folder.</returns>
        public TypeScriptFolder FindOrCreateFolder( string name ) => FindFolder( name ) ?? CreateFolder( name );

        private protected virtual TypeScriptFolder CreateFolder( string name ) => new TypeScriptFolder( this, name );

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
        /// Finds an existing folder or returns null.
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

        private protected virtual TypeScriptFile CreateFile( string name ) => new TypeScriptFile( this, name );

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
        /// This folder and the other one must belong to the same <see cref="TypeScriptGenerator"/>
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
            return Throw.InvalidOperationException<NormalizedPath>( "TypeScriptFolder must belong to the same TypeScriptRoot." );

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
        /// Saves this folder, its files and all its subordinated folders, into one or more actual paths on the file system.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="outputPaths">Any number of target directories.</param>
        /// <param name="createBarrel">Optional strategy to create barrels in folders.</param>
        /// <returns>True on success, false is an error occurred (the error has been logged).</returns>
        public bool Save( IActivityMonitor monitor, IEnumerable<NormalizedPath> outputPaths, Func<NormalizedPath, bool> createBarrel )
        {
            using( monitor.OpenTrace( $"Saving {(IsRoot ? $"TypeScript Root folder into {outputPaths.Select( o => o.ToString() ).Concatenate()}" : Name)}." ) )
            {
                if( createBarrel == null ) createBarrel = _ => false;
                try
                {
                    var newOnes = IsRoot ? outputPaths : outputPaths.Select( p => p.AppendPart( Name ) ).ToArray();

                    if( _firstFile != null )
                    {
                        foreach( var p in newOnes ) Directory.CreateDirectory( p );
                        foreach( var file in Files )
                        {
                            file.Save( monitor, newOnes );
                        }
                    }
                    var folder = _firstChild;
                    while( folder != null )
                    {
                        if( !folder.Save( monitor, newOnes, createBarrel ) ) return false;
                        folder = folder._next;
                    }
                    string? barrel = null;
                    foreach( var p in newOnes )
                    {
                        if( createBarrel( p ) )
                        {
                            if( barrel == null )
                            {
                                var b = new StringBuilder();
                                AddExportsToBarrel( p, default, b, createBarrel );
                                barrel = b.ToString();
                            }
                            File.WriteAllText( p.AppendPart( "index.ts" ), barrel );
                        }
                    }
                    return true;
                }
                catch( Exception ex )
                {
                    monitor.Error( ex );
                    return false;
                }
            }
        }

        void AddExportsToBarrel( NormalizedPath parentPath, NormalizedPath subPath, StringBuilder b, Func<NormalizedPath, bool> createBarrel )
        {
            if( !subPath.IsEmptyPath
                && (createBarrel( parentPath.Combine( subPath ) )) )
            {
                AddExportFolder( subPath, b );
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
                    folder.AddExportsToBarrel( parentPath, subPath.AppendPart( folder.Name ), b, createBarrel );
                    folder = folder._next;
                }
            }

            static void AddExportFile( NormalizedPath subPath, StringBuilder b, ReadOnlySpan<char> fileName )
            {
                b.Append( "export * from './" ).Append( subPath );
                if( !subPath.IsEmptyPath ) b.Append( '/' );
                b.Append( fileName ).AppendLine( "';" );
            }

            static void AddExportFolder( NormalizedPath subPath, StringBuilder b )
            {
                b.Append( "export * from './" ).Append( subPath ).AppendLine( "';" );
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
