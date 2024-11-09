namespace CK.TypeScript.CodeGen;

sealed class ImportFromLibrary : ImportFromBase
{
    readonly LibraryImport _lib;

    public ImportFromLibrary( FileImportCodePart part, LibraryImport lib )
        : base( part )
    {
        _lib = lib;
    }

    public override LibraryImport? FromLibrary => _lib;

    public LibraryImport Library => _lib;

    public override void Add( string symbolNames )
    {
        base.Add( symbolNames );
        _lib.IsUsed = true;
    }

    public void Write( ref SmarterStringBuilder b )
    {
        if( WriteImportAndSymbols( ref b ) )
        {
            b.Builder.Append( " from '" ).Append( _lib.Name ).AppendLine( "';" );
        }
    }
}
