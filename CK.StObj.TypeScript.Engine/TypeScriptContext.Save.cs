using CK.Core;
using CK.TypeScript.CodeGen;
using System;
using System.IO;

namespace CK.Setup
{

    public sealed partial class TypeScriptContext
    {
        /// <summary>
        /// The current version of this tooling is saved in the "ckVersion" property of
        /// the <see cref="TypeScriptIntegrationContext.TargetPackageJson"/> file.
        /// </summary>
        public const int CKTypeScriptCurrentVersion = 1;

        internal bool Save( IActivityMonitor monitor )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Saving generated TypeScript for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                var ckGenFolder = BinPathConfiguration.TargetProjectPath.AppendPart( "ck-gen" );
                var targetCKGenFolder = BinPathConfiguration.TargetCKGenPath;

                var saver = BinPathConfiguration.CKGenBuildMode
                            ? new BuildModeSaver( Root, targetCKGenFolder )
                            : new TypeScriptFileSaveStrategy( Root, targetCKGenFolder );
                // We want a root barrel for the generated module.
                Root.Root.EnsureBarrel();
                int? savedCount = Root.Save( monitor, saver );
                if( !savedCount.HasValue )
                {
                    return false;
                }
                // Fix stupid mistake in code that may have declared typescript as a regular dependency.
                if( saver.GeneratedDependencies.TryGetValue( "typescript", out var typeScriptFromCode ) )
                {
                    if( typeScriptFromCode.DependencyKind != DependencyKind.DevDependency )
                    {
                        monitor.Warn( $"Some package declared \"typescript\" as a '{typeScriptFromCode.DependencyKind}'. This has been corrected to a DevDependency." );
                        // Come on, code! typescript is a dev dependency.
                        typeScriptFromCode.UnconditionalSetDependencyKind( DependencyKind.DevDependency );
                    }
                }
                if( savedCount.Value == 0 )
                {
                    monitor.Warn( $"No files or folders have been generated in '{ckGenFolder}'. Skipping TypeScript integration." );
                }
                else 
                {
                    if( BinPathConfiguration.GitIgnoreCKGenFolder )
                    {
                        File.WriteAllText( Path.Combine( ckGenFolder, ".gitignore" ), "*" );
                    }
                    if( _integrationContext == null )
                    {
                        monitor.Info( "Skipping any TypeScript project setup since IntegrationMode is None." );
                    }
                    else
                    {
                        success &= _integrationContext.Run( monitor, saver );
                    }
                }
            }
            return success;
        }

    }
}
