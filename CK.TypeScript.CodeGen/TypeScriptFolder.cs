using CK.Core;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using static CK.Core.CheckedWriteStream;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Folder in a <see cref="TypeScriptRoot.Root"/>.
/// </summary>
public sealed partial class TypeScriptFolder
{
    readonly TypeScriptRoot _root;
    readonly TypeScriptFolder? _parent;
    TypeScriptFolder? _firstChild;
    TypeScriptFolder? _next;
    internal TypeScriptFileBase? _firstFile;
    int _fileCount;
    readonly NormalizedPath _path;
    bool _wantBarrel;
    bool _hasExportedSymbol;

    // We consider the largest set of invalid characters and
    // only remove the '/' for paths.
    // This is more strict than the regular invalid chars.
    static readonly char[] _invalidFileNameChars;
    static readonly char[] _invalidPathChars;

    static TypeScriptFolder()
    {
        _invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
        _invalidPathChars = _invalidFileNameChars.Where( c => c != '/' ).ToArray();
    }

    internal TypeScriptFolder( TypeScriptRoot root )
    {
        _root = root;
    }

    internal TypeScriptFolder( TypeScriptFolder parent, string name, TypeScriptFolder? previous )
    {
        _root = parent._root;
        _parent = parent;
        _path = parent._path.AppendPart( name );
        if( previous == null )
        {
            _next = parent._firstChild;
            parent._firstChild = this;
        }
        else
        {
            _next = previous._next;
            previous._next = this;
        }
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
    public TypeScriptFolder? Parent => _parent;

    /// <summary>
    /// Gets the root TypeScript context.
    /// </summary>
    public TypeScriptRoot Root => _root;

    /// <summary>
    /// Gets whether this folder has a barrel (see <see href="https://basarat.gitbook.io/typescript/main-1/barrel"/>).
    /// When true, a 'index.ts' barrel file will be published if <see cref="HasExportedSymbol"/> is true.
    /// </summary>
    public bool HasBarrel => _wantBarrel;

    /// <summary>
    /// Definitely sets <see cref="HasBarrel"/> to true.
    /// </summary>
    public void EnsureBarrel()
    {
        if( !_wantBarrel )
        {
            _wantBarrel = true;
            if( _hasExportedSymbol ) IncrementFileCount();
        }
    }

    /// <summary>
    /// Gets whether at least one of the file in this folder or any subfolder exports a type name.
    /// </summary>
    public bool HasExportedSymbol => _hasExportedSymbol;

    internal void SetHasExportedSymbol()
    {
        if( !_hasExportedSymbol )
        {
            _hasExportedSymbol = true;
            _parent?.SetHasExportedSymbol();
            if( _wantBarrel ) IncrementFileCount();
        }
    }

    internal void IncrementFileCount()
    {
        ++_fileCount;
        _parent?.IncrementFileCount();
    }

    /// <summary>
    /// Gets the total number of files that this folder will publish.
    /// This can differ from the <see cref="AllFilesRecursive"/> count because of the 'index.ts'
    /// barrel management.
    /// </summary>
    public int FileCount => _fileCount;

    TypeScriptFolder FindOrCreateLocalFolder( string name )
    {
        return FindLocalFolder( name, out var previous ) ?? CreateLocalFolder( name, previous );
    }

    TypeScriptFolder? FindLocalFolder( ReadOnlySpan<char> name, out TypeScriptFolder? previous )
    {
        previous = null;
        var c = _firstChild;
        while( c != null )
        {
            int cmp = name.CompareTo( c.Name, StringComparison.Ordinal );
            if( cmp == 0 ) return c;
            if( cmp > 0 ) previous = c;
            c = c._next;
        }
        return null;
    }

    TypeScriptFolder CreateLocalFolder( string name, TypeScriptFolder? previous )
    {
        CheckCreateLocalName( name, isFolder: true );
        var f = new TypeScriptFolder( this, name, previous );
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
        bool tsExtension = fileOrFolderName.EndsWith( ".ts", StringComparison.OrdinalIgnoreCase );
        if( isFolder )
        {
            if( tsExtension )
            {
                Throw.ArgumentException( "folderName", $"Folder '{fileOrFolderName}' cannot end with '.ts'." );
            }
        }
        else 
        {
            if( !tsExtension )
            {
                Throw.ArgumentException( "fileName", $"File '{fileOrFolderName}' must end with '.ts'." );
            }
        }
    }

    internal static string? GetInvalidFileOrFolderNameError( string fileOrFolderName, bool isFolder )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fileOrFolderName );
        int bad = fileOrFolderName.IndexOfAny( isFolder ? _invalidPathChars : _invalidFileNameChars );
        if( bad >= 0 )
        {
            return $"Invalid character '{fileOrFolderName[bad]}' in '{fileOrFolderName}'.";
        }
        if( fileOrFolderName == TypeScriptFileBase._hiddenFileName )
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
    public TypeScriptFolder? FindFolder( ReadOnlySpan<char> path )
    {
        var f = this;
        if( !path.IsEmpty )
        {
            Span<Range> ranges = stackalloc Range[256];
            foreach( var r in SplitPath( path, ranges ) )
            {
                f = f.FindLocalFolder( path[r], out _ );
                if( f == null ) return null;
            }
        }
        return f;
    }

    static ReadOnlySpan<Range> SplitPath( ReadOnlySpan<char> path, Span<Range> ranges )
    {
        int len = path.SplitAny( ranges, "/\\", StringSplitOptions.None );
        if( len == ranges.Length )
        {
            Throw.ArgumentException( nameof( path ), $"Too many path separators in '{path}'. Max is {ranges.Length - 1}." );
        }
        return ranges.Slice( 0, len );
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
    /// This returns 0 when <paramref name="other"/> is this folder.
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
}
