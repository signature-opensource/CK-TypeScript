namespace CK.TypeScript.CodeGen;

sealed class ImportFromLibrary : ImportFromBase
{
    readonly LibraryImport _lib;
    readonly string _subPath;

    public ImportFromLibrary( FileImportCodePart part, LibraryImport lib, string subPath )
        : base( part )
    {
        _lib = lib;
        _subPath = subPath;
    }

    public override LibraryImport? FromLibrary => _lib;

    public LibraryImport Library => _lib;

    public string SubPath => _subPath;

    public override void Add( string symbolNames )
    {
        base.Add( symbolNames );
        _lib.IsUsed = true;
    }

    public void Write( ref SmarterStringBuilder b )
    {
        if( WriteImportAndSymbols( ref b ) )
        {
            b.Builder.Append( " from '" ).Append( _lib.Name );
            if( _subPath.Length > 0 ) b.Builder.Append( '/' ).Append( _subPath );
            b.Builder.AppendLine( "';" );
        }
    }
}
