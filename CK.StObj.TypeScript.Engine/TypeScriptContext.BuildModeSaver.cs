using CK.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.TypeScript.CodeGen;
using System.Text;

namespace CK.Setup
{
    public sealed partial class TypeScriptContext
    {
        sealed class BuildModeSaver : TypeScriptFileSaveStrategy
        {
            List<(NormalizedPath Gen, OriginResource? Origin)>? _clashes;

            public BuildModeSaver( TypeScriptRoot root, NormalizedPath targetPath )
                : base( root, targetPath, withCleanupFiles: true )
            {
            }

            public override void SaveFile( IActivityMonitor monitor, TypeScriptFile file, NormalizedPath filePath )
            {
                var fInfo = new FileInfo( filePath );
                if( fInfo.Exists )
                {
                    using var fTxt = fInfo.OpenText();
                    var existing = fTxt.ReadToEnd();
                    var newOne = file.GetCurrentText();
                    if( existing != newOne )
                    {
                        _clashes ??= new List<(NormalizedPath,OriginResource?)>();
                        _clashes.Add( (file.Folder.Path.AppendPart( file.Name ), file.Origin) );
                        var filePathGen = filePath.Path + ".G.ts";
                        monitor.Trace( $"Saving '{file.Name}.G.ts'." );
                        File.WriteAllText( filePathGen, file.GetCurrentText() );
                        CleanupFiles?.Remove( filePath );
                        // Avoid deleting the generated file if it already exists.
                        CleanupFiles?.Remove( filePathGen );
                        return;
                    }
                }
                base.SaveFile( monitor, file, filePath );
            }

            public override int? Finalize( IActivityMonitor monitor, int? savedCount )
            {
                if( _clashes != null )
                {
                    using( monitor.OpenError( $"BuildMode: {_clashes.Count} files have been generated differently than the existing one:" ) )
                    {
                        var b = new StringBuilder();
                        foreach( var clash in _clashes.GroupBy( c => c.Origin?.Assembly ) )
                        {
                            if( clash.Key == null )
                            {
                                b.AppendLine( "> (unknwon assembly):" );
                                foreach( var c in clash )
                                {
                                    b.Append( "   " ).AppendLine( c.Gen );
                                }
                            }
                            else
                            {
                                b.Append( "> Assembly: " ).Append( clash.Key.GetName().Name ).Append(':').AppendLine();
                                foreach( var c in clash )
                                {
                                    b.Append( "   " ).Append( c.Gen ).Append( " <= " ).AppendLine( c.Origin!.ResourceName );
                                }
                            }
                        }
                        monitor.Trace( b.ToString() );
                    }
                    base.Finalize( monitor, savedCount );
                    return null;
                }
                return base.Finalize( monitor, savedCount );
            }
        }

    }
}
