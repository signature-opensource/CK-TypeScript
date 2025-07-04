using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;

namespace CK.TypeScript.CodeGen;

sealed class FileImportCodePart : ITSFileImportSection
{
    readonly TypeScriptFile _file;
    List<ImportFromLibrary>? _importsFromLibs;
    List<ImportFromFile>? _importsFromFiles;
    ImportFromLocalCKGen? _importFromLocalCKGen;
    // Fake, always empty, collector.
    ImportFromFileSelfReference? _importFromFileSelfReference;

    public FileImportCodePart( TypeScriptFile f )
    {
        _file = f;
    }

    internal TypeScriptFile File => _file;

    public void ImportFromLibrary( LibraryImport library, string symbolNames ) => DoEnsure( library, "" ).Add( symbolNames );

    public void ImportFromLibrary( (LibraryImport Library, string SubPath) source, string symbolNames ) => DoEnsure( source.Library, source.SubPath ).Add( symbolNames );

    public ITSImportLine EnsureFile( TypeScriptFileBase file )
    {
        return _file == file
                ? (_importFromFileSelfReference ??= new ImportFromFileSelfReference( _file ))
                : DoEnsure( file );
    }

    public void ImportFromFile( TypeScriptFileBase file, string symbolNames ) => EnsureFile( file ).Add( symbolNames );

    ImportFromLibrary DoEnsure( LibraryImport library, string subPath )
    {
        Throw.CheckNotNullArgument( library );
        _importsFromLibs ??= new List<ImportFromLibrary>();
        ImportFromLibrary line;
        if( subPath.Length > 0 && subPath[0] == '/' )
        {
            subPath = subPath.Substring( 1 );
        }
        int idx = _importsFromLibs.IndexOf( i => i.Library == library && i.SubPath == subPath );
        if( idx < 0 )
        {
            line = new ImportFromLibrary( this, library, subPath );
            _importsFromLibs.Add( line );
        }
        else
        {
            line = _importsFromLibs[idx];
        }

        return line;
    }

    ImportFromFile DoEnsure( TypeScriptFileBase file )
    {
        Throw.CheckNotNullArgument( file );
        _importsFromFiles ??= new List<ImportFromFile>();
        ImportFromFile line;
        int idx = _importsFromFiles.IndexOf( i => i.ImportedFile == file );
        if( idx < 0 )
        {
            line = new ImportFromFile( this, file );
            _importsFromFiles.Add( line );
        }
        else
        {
            line = _importsFromFiles[idx];
        }

        return line;
    }


    public void ImportFromLocalCKGen( string symbolNames )
    {
        _importFromLocalCKGen ??= new ImportFromLocalCKGen( this );
        _importFromLocalCKGen.Add( symbolNames );
    }

    public ITSFileImportSection Import( ITSType tsType )
    {
        Throw.CheckArgument( tsType != null );
        tsType.EnsureRequiredImports( this );
        return this;
    }

    public ITSFileImportSection EnsureImport( IActivityMonitor monitor, Type type )
    {
        Throw.CheckNotNullArgument( type );
        var tsType = _file.Root.TSTypes.ResolveTSType( monitor, type );
        tsType.EnsureRequiredImports( this );
        return this;
    }

    public IEnumerable<string> ImportedLibraryNames => _importsFromLibs?.Select( l => l.Library.Name ) ?? ImmutableArray<string>.Empty;

    internal void ClearImports()
    {
        _importsFromFiles?.Clear();
        _importsFromLibs?.Clear();
        _importFromLocalCKGen?.Clear();
    }

    internal Action<ITSFileImportSection> CreateImportSnapshot()
    {
        var hasLib = _importsFromLibs != null && _importsFromLibs.Count != 0;
        var hasFile = _importsFromFiles != null && _importsFromFiles.Count != 0;
        var hasCKGen = _importFromLocalCKGen != null && _importFromLocalCKGen.Count != 0;

        if( !hasLib && !hasFile && !hasCKGen )
        {
            return Util.ActionVoid;
        }
        var snapshot = new List<(object? Owner, string? DefaultImportSymbol, TSImportedName[] ImportedNames)>();
        if( _importsFromLibs != null ) snapshot.AddRange( _importsFromLibs.Select( i => i.GetSnapshot( (i.Library, i.SubPath) ) ) );
        if( _importsFromFiles != null ) snapshot.AddRange( _importsFromFiles.Select( i => i.GetSnapshot( i.ImportedFile ) ) );
        if( _importFromLocalCKGen != null ) snapshot.Add( _importFromLocalCKGen.GetSnapshot( null ) );

        return i => ((FileImportCodePart)i).ImportSnapshot( snapshot );
    }

    void ImportSnapshot( object snapshot )
    {
        var all = (List<(object? Owner, string? DefaultImportSymbol, TSImportedName[] ImportedNames)>)snapshot;
        foreach( var (owner, defSymbol, name) in all )
        {
            switch( owner )
            {
                case ValueTuple<LibraryImport, string> lib: DoEnsure( lib.Item1, lib.Item2 ).SetSnapshot( defSymbol, name ); break;
                case TypeScriptFileBase file: DoEnsure( file ).SetSnapshot( defSymbol, name ); break;
                default:
                    Throw.DebugAssert( owner is null );
                    _importFromLocalCKGen ??= new ImportFromLocalCKGen( this );
                    _importFromLocalCKGen.SetSnapshot( defSymbol, name );
                    break;
            }
        }
    }

    internal void Build( ref SmarterStringBuilder b, IActivityMonitor? monitor, TSTypeManager? tsTypes )
    {
        if( _importsFromLibs != null )
        {
            foreach( var lib in _importsFromLibs )
            {
                lib.Write( ref b  );
            }
        }
        if( _importsFromFiles != null )
        {
            foreach( var f in _importsFromFiles )
            {
                f.Write( ref b );
            }
        }
        if( _importFromLocalCKGen != null )
        {
            _importFromLocalCKGen.Write( ref b, monitor, tsTypes );
        }
    }

    public override string ToString()
    {
        var b = new SmarterStringBuilder( new StringBuilder() );
        Build( ref b, null, null );
        if( b.Length > 0 ) b.Append( Environment.NewLine );
        return b.ToString();
    }
}
