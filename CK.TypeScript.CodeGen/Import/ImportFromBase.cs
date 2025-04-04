using CK.Core;
using System.Collections.Generic;

namespace CK.TypeScript.CodeGen;

abstract class ImportFromBase : ITSImportLine
{
    string? _defaultImportSymbol;
    List<TSImportedName> _importedNames;
    readonly FileImportCodePart _part;

    protected ImportFromBase( FileImportCodePart part )
    {
        _importedNames = new List<TSImportedName>();
        _part = part;
    }

    internal TypeScriptFile File => _part.File;

    public virtual bool FromLocalCkGen => false;

    public virtual LibraryImport? FromLibrary => null;

    public virtual TypeScriptFileBase? FromTypeScriptFile => null;

    public string? DefaultImportSymbol => _defaultImportSymbol;

    public IReadOnlyList<TSImportedName> ImportedNames => _importedNames;

    public virtual void Add( string symbolNames )
    {
        // Quick and dirty implementation. Sorry.
        foreach( var n in symbolNames.Split( ',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries ) )
        {
            if( n.StartsWith( "default " ) )
            {
                var newDef = n.Substring( 8 );
                SetDefaultImportSymbol( newDef );
            }
            else
            {
                int idx = n.IndexOf( " as " );
                TSImportedName name = idx >= 0
                                        ? new TSImportedName( n.Substring( 0, idx ).TrimEnd(), n.Substring( idx + 4 ).TrimStart() )
                                        : new TSImportedName( n, null );
                AddImportedName( name );
            }
        }
    }

    void AddImportedName( TSImportedName name )
    {
        if( !_importedNames.Contains( name ) )
        {
            _importedNames.Add( name );
        }
    }

    void SetDefaultImportSymbol( string symbolName )
    {
        if( _defaultImportSymbol != null && symbolName != _defaultImportSymbol )
        {
            Throw.InvalidOperationException( $"""
                          Conflicting import of the default export in '{File.Folder.Path}/{File.Name}'.
                          Symbol '{_defaultImportSymbol}' is already defined, importing '{symbolName}' is not posssible.
                          """ );
        }
        _defaultImportSymbol = symbolName;
    }

    internal void SetSnapshot( string? defSymbol, TSImportedName[] names )
    {
        if( defSymbol != null ) SetDefaultImportSymbol( defSymbol );
        foreach( var n in names ) AddImportedName( n );
    }


    internal int Count => _importedNames.Count;

    internal void Clear()
    {
        _defaultImportSymbol = null;
        _importedNames.Clear();
    }

    internal (object? Owner, string? DefaultImportSymbol, TSImportedName[] ImportedNames) GetSnapshot( object? owner )
    {
        return (owner, _defaultImportSymbol, _importedNames.ToArray());
    }

    internal bool WriteImportAndSymbols( ref SmarterStringBuilder b )
    {
        bool hasNames = _importedNames.Count != 0;
        if( _defaultImportSymbol == null && !hasNames ) return false;
        b.Append( "import " );
        if( _defaultImportSymbol != null )
        {
            b.Builder.Append( _defaultImportSymbol );
            if( hasNames ) b.Builder.Append( ", " );
        }
        if( hasNames )
        {
            b.Builder.Append( "{ " );
            hasNames = false;
            foreach( var n in _importedNames )
            {
                if( hasNames ) b.Builder.Append( ", " );
                hasNames = true;
                if( n.IsAliased ) b.Builder.Append( n.ExportedName ).Append( " as " ).Append( n.ImportedName );
                else b.Builder.Append( n.ExportedName );
            }
            b.Builder.Append( " }" );
        }
        return true;
    }

}
