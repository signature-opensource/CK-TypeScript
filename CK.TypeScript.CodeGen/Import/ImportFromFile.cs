namespace CK.TypeScript.CodeGen;

sealed class ImportFromFile : ImportFromBase
{
    readonly IMinimalTypeScriptFile _file;

    public ImportFromFile( FileImportCodePart part, IMinimalTypeScriptFile file )
        : base( part )
    {
        _file = file;
    }

    public override IMinimalTypeScriptFile? FromTypeScriptFile => _file;

    public IMinimalTypeScriptFile ImportedFile => _file;

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
