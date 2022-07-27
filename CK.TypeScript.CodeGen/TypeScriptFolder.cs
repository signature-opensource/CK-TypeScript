using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Folder in a <see cref="TypeScriptRoot.Root"/>.
    /// </summary>
    public class TypeScriptFolder
    {
        readonly TypeScriptRoot _g;
        TypeScriptFolder? _firstChild;
        TypeScriptFolder? _next;
        internal TypeScriptFile? _firstFile;

        static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

        internal TypeScriptFolder( TypeScriptRoot g )
        {
            _g = g;
            Name = String.Empty;
        }

        internal TypeScriptFolder( TypeScriptFolder parent, string name )
        {
            _g = parent._g;
            Parent = parent;
            Name = name;
            _next = parent._firstChild;
            parent._firstChild = this;
        }

        /// <summary>
        /// Gets this folder's name.
        /// This string is empty when this is the <see cref="TypeScriptRoot.Root"/>, otherwise
        /// it necessarily not empty without '.ts' extension.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether this folder is the root one.
        /// </summary>
        public bool IsRoot => Name.Length == 0;

        /// <summary>
        /// Gets the parent folder. Null when this is the <see cref="TypeScriptRoot.Root"/>.
        /// </summary>
        public TypeScriptFolder? Parent { get; }

        /// <summary>
        /// Gets the root TypeScript context.
        /// </summary>
        public TypeScriptRoot Root => _g;

        /// <summary>
        /// Gets or sets whether a barrel (see https://basarat.gitbook.io/typescript/main-1/barrel) must be
        /// generated in this folder.
        /// When null (the default), this is decided by the strategy of the <see cref="Save"/> method.
        /// When set, the strategy is not used and this property takes precedence.
        /// </summary>
        public bool? CreateBarrelOnSave { get; set; }

        /// <summary>
        /// Finds or creates a folder.
        /// </summary>
        /// <param name="name">The folder's name to find or create. Must not be empty nor ends with '.ts'.</param>
        /// <returns>The folder.</returns>
        public TypeScriptFolder FindOrCreateFolder( string name ) => FindFolder( name ) ?? new TypeScriptFolder( this, name );

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
        /// Finds or creates a file in this folder.
        /// </summary>
        /// <param name="name">The file's name to find or create. Must not be empty and must end with '.ts'.</param>
        /// <returns>The file.</returns>
        public TypeScriptFile FindOrCreateFile( string name ) => FindFile( name ) ?? new TypeScriptFile( this, name );

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
                f = new TypeScriptFile( this, name );
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
        /// Gets all the files that this folder contains.
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
        /// Saves this folder, its files and all its subordinated folders, into one or more actual paths on the file system.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="outputPaths">Any number of target directories.</param>
        /// <param name="createBarrel">Optional strategy to create barrels in folders. See <see cref="CreateBarrelOnSave"/>.</param>
        /// <returns>True on success, false is an error occurred (the error has been logged).</returns>
        public bool Save( IActivityMonitor monitor, IEnumerable<NormalizedPath> outputPaths, Func<NormalizedPath,bool>? createBarrel = null )
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
                        var file = _firstFile;
                        while( file != null )
                        {
                            file.Save( monitor, newOnes );
                            file = file._next;
                        }
                    }
                    var folder = _firstChild;
                    while( folder != null )
                    {
                        if( !folder.Save( monitor, newOnes, createBarrel ) ) return false;
                        folder = folder._next;
                    }
                    if( CreateBarrelOnSave != false )
                    {
                        string? barrel = null;
                        foreach( var p in newOnes )
                        {
                            if( CreateBarrelOnSave ?? createBarrel( p ) )
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
                && (CreateBarrelOnSave ?? createBarrel( parentPath.Combine( subPath ) )) )
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
