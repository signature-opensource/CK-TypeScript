using CK.Core;
using System.IO;
using System.Linq;

namespace CK.TypeScript.CodeGen;

sealed class ImportFromLocalCKGen : ImportFromBase
{
    public ImportFromLocalCKGen( FileImportCodePart part )
        : base( part )
    {
    }

    public override bool FromLocalCkGen => true;

    public void Write( ref SmarterStringBuilder b, IActivityMonitor? monitor, TSTypeManager? tsTypes )
    {
        Throw.DebugAssert( (monitor == null) == (tsTypes == null) );
        if( tsTypes == null )
        {
            if( WriteImportAndSymbols( ref b ) )
            {
                b.Builder.AppendLine( " from '@local/ck-gen';" );
            }
        }
        else
        {
            var resolved = ImportedNames.GroupBy( i => tsTypes.FindByTypeName( i.ExportedName ) as ITSDeclaredFileType );
            foreach( var i in resolved )
            {
                if( i.Key == null )
                {
                    var warnMsg = $"""
                        No associated file found for '{i.Select( t => t.ExportedName ).Concatenate( "', '" )}'. (Did you miss a [TypeScriptFile( file, type )]?)
                        """;
                    b.Builder.Append( "// " ).Append( warnMsg ).AppendLine();
                    b.Builder.Append( "import " );
                    WriteImportedNamesInBraces( b, i );
                    b.Builder.AppendLine( " from '@local/ck-gen';" );
                    monitor.Warn( $"In '{File.Folder}{File.Name}': {warnMsg}" );
                }
                else
                {
                    b.Builder.Append( "import " );
                    WriteImportedNamesInBraces( b, i );
                    b.Builder.Append( " from '@local/ck-gen/" )
                             .Append( i.Key.ImportPath )
                             .AppendLine( "';" );
                }
            }
        }
    }

}
