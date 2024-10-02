using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Folder in a <see cref="TypeScriptRoot.Root"/>.
/// </summary>
public sealed partial class TypeScriptFolder
{
    readonly TypeScriptRoot _root;
    TypeScriptFolder? _firstChild;
    readonly TypeScriptFolder? _next;
    internal BaseFile? _firstFile;
    readonly NormalizedPath _path;
    bool _wantBarrel;

    // We consider the largest set of invalid characters and
    // only remove the '/' for paths.
    // This is more strict than the regular invalid chars.
    static readonly char[] _invalidFileNameChars;
    static readonly char[] _invalidPathChars;

    static TypeScriptFolder()
    {
        _invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
        _invalidPathChars = _invalidFileNameChars.Where( c =>  c != '/' ).ToArray();
    }

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
    /// it necessarily not empty and without '.ts' extension.
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
    public bool HasBarrel => _wantBarrel;

    /// <summary>
    /// Definitely sets <see cref="HasBarrel"/> to true.
    /// </summary>
    public void EnsureBarrel() => _wantBarrel = true;

    TypeScriptFolder FindOrCreateLocalFolder( string name )
    {
        return FindLocalFolder( name ) ?? CreateLocalFolder( name );
    }

    TypeScriptFolder? FindLocalFolder( string name )
    {
        var c = _firstChild;
        while( c != null )
        {
            if( c.Name == name ) return c;
            c = c._next;
        }
        return null;
    }

    TypeScriptFolder CreateLocalFolder( string name )
    {
        CheckCreateLocalName( name, isFolder: true );
        var f = new TypeScriptFolder( this, name );
        _root.OnFolderCreated( f );
        return f;
    }

    void CheckCreateLocalName( string fileOrFolderName, bool isFolder )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fileOrFolderName );
        var e = GetInvalidFileOrFolderNameError( fileOrFolderName, isFolder );
        if( e != null )
        {
            throw new ArgumentException( e, nameof( fileOrFolderName ) );
        }
        if( isFolder
                 ? FindLocalFile( fileOrFolderName ) != null
                 : FindLocalFolder( fileOrFolderName ) != null )
        {
            throw new InvalidOperationException( $"Unable to create {(isFolder ? "folder" : "file")} named '{fileOrFolderName}'. A {(isFolder ? "file" : "folder")} with this name exists." );
        }
    }

    internal static string? GetInvalidFileOrFolderNameError( string fileOrFolderName, bool isFolder )
    {
        int bad = fileOrFolderName.IndexOfAny( isFolder ? _invalidPathChars : _invalidFileNameChars );
        if( bad >= 0 )
        {
            return $"Invalid character '{fileOrFolderName[bad]}' in '{fileOrFolderName}'.";
        }
        if( fileOrFolderName == BaseFile._hiddenFileName )
        {
            return $"Forbidden name '{fileOrFolderName}'.";
        }
        return null;
    }

    /// <summary>
    /// Finds or creates a subordinated folder by its path.
    /// </summary>
    /// <param name="path">The folder's path to find or create. None of its parts must end with '.ts'.</param>
    /// <returns>The folder.</returns>
    public TypeScriptFolder FindOrCreateFolder( NormalizedPath path )
    {
        var f = this;
        if( !path.IsEmptyPath )
        {
            foreach( var name in path.Parts )
            {
                f = f.FindOrCreateLocalFolder( name );
            }
        }
        return f;
    }

    /// <summary>
    /// Finds an existing subordinated folder by its path or returns null.
    /// </summary>
    /// <param name="path">The path to the subordinated folder to find.</param>
    /// <returns>The existing folder or null.</returns>
    public TypeScriptFolder? FindFolder( NormalizedPath path )
    {
        var f = this;
        if( !path.IsEmptyPath )
        {
            foreach( var name in path.Parts )
            {
                f = f.FindLocalFolder( name );
                if( f == null ) return null;
            }
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
    /// <param name="saver">The <see cref="TypeScriptFileSaveStrategy"/>.</param>
    /// <returns>Number of files saved on success, null if an error occurred (the error has been logged).</returns>
    public int? Save( IActivityMonitor monitor, TypeScriptFileSaveStrategy saver )
    {
        using( monitor.OpenTrace( IsRoot ? $"Saving TypeScript Root folder into {saver.Target}" : $"Saving /{Name}." ) )
        {
            var parentTarget = saver._currentTarget;
            var target = IsRoot ? saver.Target : parentTarget.AppendPart( Name );
            saver._currentTarget = target;
            try
            {
                int result = 0;

                bool createdDirectory = false;
                if( _firstFile != null )
                {
                    Directory.CreateDirectory( target );
                    createdDirectory = true;
                    foreach( var file in AllFiles )
                    {
                        file.Save( monitor, saver );
                        ++result;
                    }
                }
                var folder = _firstChild;
                while( folder != null )
                {
                    var r = folder.Save( monitor, saver );
                    if( !r.HasValue ) return null;
                    result += r.Value;
                    folder = folder._next;
                }
                if( _wantBarrel && FindLocalFile( "index.ts" ) == null )
                {
                    var b = new StringBuilder();
                    AddExportsToBarrel( default, b );
                    if( b.Length > 0 )
                    {
                        if( !createdDirectory ) Directory.CreateDirectory( target );

                        var index = target.AppendPart( "index.ts" );
                        File.WriteAllText( index, b.ToString() );
                        saver.CleanupFiles?.Remove( index );
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
            finally
            {
                saver._currentTarget = parentTarget;
            }
        }
    }

    void AddExportsToBarrel( NormalizedPath subPath, StringBuilder b )
    {
        if( !subPath.IsEmptyPath && (_wantBarrel || FindLocalFile("index.ts") != null) )
        {
            b.Append( "export * from './" ).Append( subPath ).AppendLine( "';" );
        }
        else
        {
            var file = _firstFile;
            while( file != null )
            {
                if( file is IMinimalTypeScriptFile ts && ts.AllTypes.Any() )
                {
                    AddExportFile( subPath, b, file.Name.AsSpan().Slice( 0, file.Name.Length - 3 ) );
                }
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

}
