using CK.Core;
using CK.Transform.Core;
using System;
using System.IO;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class ExternalItemResolver : IExternalTransformableItemResolver
    {
        private NormalizedPath _ckGenFolder;
        readonly NormalizedPath _srcFolderPath;

        public ExternalItemResolver( NormalizedPath ckGenFolder, NormalizedPath srcFolderPath )
        {
            _ckGenFolder = ckGenFolder;
            _srcFolderPath = srcFolderPath;
        }

        public ExternalTransformableItem? Resolve( IActivityMonitor monitor,
                                                   TransformerFunction transformer,
                                                   ReadOnlySpan<char> expectedPath,
                                                   bool isNamePrefix,
                                                   ReadOnlySpan<char> name )
        {
            Throw.CheckArgument( "Conventional prefix for external transformation targets.", expectedPath.StartsWith( "../" ) );
            // We only allow "src/" subpath. We don't want a transformer to have free access to the file system...
            var target = _ckGenFolder.Combine( transformer.Target ).ResolveDots();
            if( !target.StartsWith( _srcFolderPath ) )
            {
                monitor.Error( $"Invalid transformer target '{transformer.Target}'. The target path must be in th 'src/' folder." );
                return null;
            }
            if( !File.Exists( target ) )
            {
                monitor.Error( $"""
                    Unable to find transformer target '{transformer.Target}'.
                    File '{target}' not found.
                    """ );
                return null;
            }
            return new Item( File.ReadAllText( target ), target );
        }

        sealed class Item : ExternalTransformableItem
        {
            public Item( string initialText, string externalPath )
                : base( initialText, externalPath )
            {
            }

            protected override bool Install( IActivityMonitor monitor, string transformedText )
            {
                Throw.CheckArgument( InitialText != transformedText );
                using( monitor.OpenInfo( $"Saving transformed file '{ExternalPath}'." ) )
                {
                    try
                    {
                        File.WriteAllText( ExternalPath, transformedText );
                        return true;
                    }
                    catch(Exception ex )
                    {
                        monitor.Error( $"While saving '{ExternalPath}'.", ex );
                        return false;
                    }
                }
            }
        }
    }
}
