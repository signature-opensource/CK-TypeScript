using CK.Core;
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.CodeGen;

public sealed partial class TypeScriptFolder // TypeScriptFile management.
{
    /// <summary>
    /// Gets the files that this folder contains.
    /// Use <see cref="AllFilesRecursive"/> to get all the subordinated files.
    /// </summary>
    public IEnumerable<BaseFile> AllFiles
    {
        get
        {
            BaseFile? c = _firstFile;
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
    public IEnumerable<BaseFile> AllFilesRecursive => AllFiles.Concat( Folders.SelectMany( s => s.AllFilesRecursive ) );

    BaseFile? FindLocalFile( ReadOnlySpan<char> name )
    {
        var c = _firstFile;
        while( c != null )
        {
            if( name.Equals( c.Name, StringComparison.Ordinal ) ) return c;
            c = c._next;
        }
        return null;
    }

    BaseFile? DoFindFile( NormalizedPath path, out TypeScriptFolder? closest )
    {
        Throw.CheckArgument( !path.IsEmptyPath );
        closest = this;
        for( int i = 0; i < path.Parts.Count - 1; i++ )
        {
            closest = closest.FindLocalFolder( path.Parts[i] );
            if( closest == null ) return null;
        }
        return closest.FindLocalFile( path.LastPart );
    }

    BaseFile? DoFindFile( ReadOnlySpan<char> path, out TypeScriptFolder? closest )
    {
        Throw.CheckArgument( !path.IsEmpty );
        closest = this;
        Span<Range> rangeStore = stackalloc Range[256];
        var ranges = SplitPath( path, rangeStore );
        for( int i = 0; i < ranges.Length - 1; i++ )
        {
            closest = closest.FindLocalFolder( path[ranges[i]] );
            if( closest == null ) return null;
        }
        return closest.FindLocalFile( path[ranges[^1]] );
    }

    /// <summary>
    /// Finds a <see cref="BaseFile"/> in this folder or a subordinated folder.
    /// </summary>
    /// <param name="path">The file's path to find or create. Must not be empty.</param>
    /// <returns>The file or null if not found.</returns>
    public BaseFile? FindFile( NormalizedPath path ) => DoFindFile( path, out var _ );

    /// <summary>
    /// Finds a <see cref="BaseFile"/> in this folder or a subordinated folder.
    /// </summary>
    /// <param name="path">The file's path to find or create. Must not be empty.</param>
    /// <returns>The file or null if not found.</returns>
    public BaseFile? FindFile( ReadOnlySpan<char> path ) => DoFindFile( path, out var _ );

    /// <summary>
    /// Finds or creates a file in this folder or a subordinated folder.
    /// </summary>
    /// <param name="path">The file's full path to find or create. The <see cref="NormalizedPath.LastPart"/> must end with '.ts'.</param>
    /// <returns>The file.</returns>
    public TypeScriptFile FindOrCreateTypeScriptFile( NormalizedPath path ) => FindOrCreateTypeScriptFile( path, out _ );

    TypeScriptFolder FindOrCreateParentFolder( NormalizedPath path )
    {
        var f = this;
        for( int i = 0; i < path.Parts.Count - 1; i++ )
        {
            f = f.FindOrCreateLocalFolder( path.Parts[i] );
        }
        return f;
    }

    /// <summary>
    /// Finds or creates a file in this folder or a subordinated folder.
    /// </summary>
    /// <param name="path">The file's path to find or create. Must not be empty and must end with '.ts'.</param>
    /// <param name="created">True if the file has been created, false if it already existed.</param>
    /// <returns>The file.</returns>
    public TypeScriptFile FindOrCreateTypeScriptFile( NormalizedPath path, out bool created )
    {
        Throw.CheckArgument( !path.IsEmptyPath );
        var name = path.LastPart;
        Throw.CheckArgument( name.EndsWith( ".ts" ) );
        var folder = FindOrCreateParentFolder( path );
        var baseFile = folder.FindLocalFile( name );
        if( baseFile is TypeScriptFile file )
        {
            created = false;
            return file;
        }
        if( baseFile == null )
        {
            Throw.CheckArgument( "Cannot create a 'index.ts' at the root (this is the default barrel).",
                                 !folder.IsRoot || !name.Equals( "index.ts", StringComparison.OrdinalIgnoreCase ) );
            CheckCreateLocalName( name, isFolder: false );
            created = true;
            file = new TypeScriptFile( folder, name );
            _root.OnTypeScriptFileCreated( file );
            return file;
        }
        Throw.DebugAssert( baseFile is ResourceTypeScriptFile );
        throw new InvalidOperationException( $"Unable fo create '{path}'. This TypeScript file is resource based, loaded from '{(ResourceTypeScriptFile)baseFile}'." );
    }

    /// <summary>
    /// Creates a file from a resource in this folder or a subordinated folder.
    /// The result type depends on the path extension.
    /// <para>
    /// Even if this method is named Create, it allows creation from the exact same resource locator (same container, same resource name)
    /// to enable multile glob resources registrations and individual ones to work together (individual registration '.ts' can declare types).
    /// </para>
    /// </summary>
    /// <param name="locator">The resource locator of the file.</param>
    /// <param name="path">The file's path to create. Must not be empty.</param>
    /// <returns>The file that is a <see cref="IResourceFile"/>.</returns>
    public BaseFile CreateResourceFile( in ResourceLocator locator, NormalizedPath path )
    {
        Throw.CheckArgument( !path.IsEmptyPath );
        var folder = FindOrCreateParentFolder( path );
        var name = path.LastPart;
        var f = folder.FindLocalFile( name );
        if( f != null )
        {
            if( f is TypeScriptFile )
            {
                throw new InvalidOperationException( $"Unable fo create '{path}' from {locator}. This file is a TypeScript generated file." );
            }
            Throw.DebugAssert( "TypeScript is the only type of file that may not come from a resource.", f is IResourceFile );
            // See comments!
            var rF = Unsafe.As<IResourceFile>( f );
            if( rF.Locator == locator )
            {
                return f;
            }
            throw new InvalidOperationException( $"Unable fo create '{path}' from {locator}. This file already comes from {rF.Locator}." );
        }
        CheckCreateLocalName( name, isFolder: false );
        f = BaseFile.CreateResourceFile( folder, name, in locator );
        return f;
    }

}
