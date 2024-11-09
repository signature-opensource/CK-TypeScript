namespace CK.TypeScript.CodeGen;

sealed class ImportFromLocalCKGen : ImportFromBase
{
    public ImportFromLocalCKGen( FileImportCodePart part )
        : base( part )
    {
    }

    public override bool FromLocalCkGen => true;

    public void Write( ref SmarterStringBuilder b )
    {
        if( WriteImportAndSymbols( ref b ) )
        {
            b.Builder.AppendLine( " from '@local/ck-gen';" );
        }
    }

}
