namespace CK.TypeScript.CodeGen;

sealed class ImportFromFile : ImportFromBase
{
    readonly TypeScriptFileBase _file;

    public ImportFromFile( FileImportCodePart part, TypeScriptFileBase file )
        : base( part )
    {
        _file = file;
    }

    public override TypeScriptFileBase? FromTypeScriptFile => _file;

    public TypeScriptFileBase ImportedFile => _file;

    public void Write( ref SmarterStringBuilder b )
    {
        if( WriteImportAndSymbols( ref b ) )
        {
            b.Builder.Append( " from '" )
                     .Append( File.Folder.GetRelativePathTo( _file.Folder ) )
                     .Append( '/' )
                     .Append( _file.Name.Substring( 0, _file.Name.Length - 3 ) )
                     .AppendLine( "';" );
        }
    }

}
