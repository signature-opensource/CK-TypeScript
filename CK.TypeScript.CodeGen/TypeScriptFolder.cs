using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CK.TypeScript.CodeGen
{
    public class TypeScriptFolder
    {
        readonly TypeScriptCodeGenerationContext _g;
        TypeScriptFolder? _firstChild;
        TypeScriptFolder? _next;
        internal TypeScriptFile? _firstFile;

        static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

        internal TypeScriptFolder( TypeScriptCodeGenerationContext g )
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
        /// This string is empty when this is the <see cref="TypeScriptCodeGenerationContext.Root"/>, otherwise
        /// it necessarily not ends with '.ts'.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether this folder is the root one.
        /// </summary>
        public bool IsRoot => Name.Length == 0;

        /// <summary>
        /// Gets the parent folder. Null when this is the <see cref="TypeScriptCodeGenerationContext.Root"/>.
        /// </summary>
        public TypeScriptFolder? Parent { get; }

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
        /// Finds an exisitng folder or returns null.
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
        /// Finds or creates a file in this folder or a subordinated folder.
        /// </summary>
        /// <param name="filePath">The file's full path to find or create. The <see cref="NormalizedPath.LastPart"/> must end with '.ts'.</param>
        /// <returns>The file.</returns>
        public TypeScriptFile FindOrCreateFile( NormalizedPath filePath )
        {
            return FindOrCreateFolder( filePath.RemoveLastPart() ).FindOrCreateFile( filePath.LastPart );
        }

        /// <summary>
        /// Finds an exisitng file in this folder or returns null.
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

        public bool Save( IActivityMonitor monitor, IReadOnlyList<NormalizedPath> outputPaths )
        {
            using( monitor.OpenTrace( $"Saving {(IsRoot ? $"TypeScript Root folder into {outputPaths.Select( o => o.ToString() ).Concatenate()}" : Name)}." ) )
            {
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
                        if( !folder.Save( monitor, newOnes ) ) return false;
                        folder = folder._next;
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

        static void CheckName( string name, bool isFolder )
        {
            if( String.IsNullOrWhiteSpace( name ) || name.IndexOfAny( _invalidFileNameChars ) >= 0 ) throw new ArgumentException( $"Empty name or invalid characters in: '{name}'.", nameof( name ) );
            if( name.EndsWith( ".ts", StringComparison.OrdinalIgnoreCase ) )
            {
                if( isFolder ) throw new ArgumentException( $"Folder name must not end with '.ts': '{name}'.", nameof( name ) );
            }
            else
            {
                if( !isFolder ) throw new ArgumentException( $"File name must end with '.ts': '{name}'.", nameof( name ) );
            }
        }
    }
}
