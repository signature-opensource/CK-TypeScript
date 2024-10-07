using CK.Core;
using CK.TypeScript.CodeGen;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class BuildModeSaver : TypeScriptFileSaveStrategy
    {
        List<(NormalizedPath Gen, ResourceTypeLocator Origin)>? _clashes;

        public BuildModeSaver( TypeScriptRoot root, NormalizedPath targetPath )
            : base( root, targetPath )
        {
        }

        public override void SaveFile( IActivityMonitor monitor, BaseFile file, NormalizedPath filePath )
        {
            if( !File.Exists( filePath ) )
            {
                base.SaveFile( monitor, file, filePath );
                return;
            }
            using var stream = file.TryGetContentStream();
            if( stream != null )
            {
                using var fMem = (RecyclableMemoryStream)Util.RecyclableStreamManager.GetStream();
                stream.CopyTo( fMem );
                if( !CheckEqual( filePath, fMem ) )
                {
                    fMem.Position = 0;
                    using var fDisk = File.OpenWrite( OnClashPath( monitor, file, filePath ) );
                    fMem.CopyTo( fDisk );
                }
            }
            else
            {
                Throw.DebugAssert( "Files are either pure stream or text based.", file is TextFileBase );
                var existing = File.ReadAllText( filePath );
                var newOne = Unsafe.As<TextFileBase>( file ).GetCurrentText();
                if( existing != newOne )
                {
                    File.WriteAllText( OnClashPath( monitor, file, filePath ), newOne );
                }
            }
            CleanupFiles?.Remove( filePath );

            static bool CheckEqual( NormalizedPath filePath, RecyclableMemoryStream fMem )
            {
                using var checker = CheckedWriteStream.Create( fMem );
                using var fDisk = File.OpenRead( filePath );
                fDisk.CopyTo( checker );
                return checker.GetResult() == CheckedWriteStream.Result.None;
            }

        }

        string OnClashPath( IActivityMonitor monitor, BaseFile file, NormalizedPath filePath )
        {
            string savedFilePath;
            _clashes ??= new List<(NormalizedPath, ResourceTypeLocator)>();
            _clashes.Add( (file.Folder.Path.AppendPart( file.Name ), file is IResourceFile rF ? rF.Locator : default) );
            savedFilePath = $"{filePath.Path}.G{file.Extension}";
            monitor.Trace( $"Saving '{Path.GetFileName( savedFilePath.AsSpan() )}'." );
            // Avoid deleting the generated file if it already exists.
            CleanupFiles?.Remove( savedFilePath );
            return savedFilePath;
        }

        public override int? Finalize( IActivityMonitor monitor, int? savedCount )
        {
            if( _clashes != null )
            {
                using( monitor.OpenError( $"BuildMode: {_clashes.Count} files have been generated differently than the existing one:" ) )
                {
                    var b = new StringBuilder();
                    foreach( var clash in _clashes.GroupBy( c => c.Origin.Declarer?.Assembly ) )
                    {
                        if( clash.Key == null )
                        {
                            b.AppendLine( "> (generated file):" );
                            foreach( var (f, _) in clash )
                            {
                                b.Append( "   " ).AppendLine( f );
                            }
                        }
                        else
                        {
                            b.Append( "> Assembly: " ).Append( clash.Key.GetName().Name ).Append( ':' ).AppendLine();
                            foreach( var (f, origin) in clash )
                            {
                                b.Append( "   " ).Append( f ).Append( " <= " ).Append( origin.ResourceName )
                                 .Append( ", declared by " ).AppendLine( origin.Declarer.ToCSharpName() );
                            }
                        }
                    }
                    monitor.Trace( b.ToString() );
                }
                savedCount = null;
            }
            return base.Finalize( monitor, savedCount );
        }
    }

}
