using CK.Core;
using CK.TypeScript.CodeGen;
using System;
using System.IO;
using System.Threading;

namespace CK.Setup;

public sealed partial class TypeScriptContext // Save
{
    /// <summary>
    /// The current version of this tooling is saved in the "ckVersion" property of
    /// the <see cref="TypeScriptIntegrationContext.TargetPackageJson"/> file.
    /// </summary>
    public const int CKTypeScriptCurrentVersion = 1;

    internal bool Save( IActivityMonitor monitor )
    {
        bool success = true;
        using( monitor.OpenInfo( $"Saving generated TypeScript for:{Environment.NewLine}{BinPathConfiguration.ToOnlyThisXml()}" ) )
        {
            var ckGenFolder = BinPathConfiguration.TargetProjectPath.AppendPart( "ck-gen" );
            var targetCKGenFolder = BinPathConfiguration.TargetCKGenPath;
            var saver = new TypeScriptFileSaveStrategy( Root, targetCKGenFolder );
            // We want a root barrel for the generated module.
            Root.Root.EnsureBarrel();
            // Saving the root.  
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
                    var liveEnginePath = typeof( TypeScript.LiveEngine.LiveState ).Assembly.Location;
                    if( BinPathConfiguration.TargetProjectPath.TryGetRelativePathTo( liveEnginePath,
                                                                                        out var relative ) )
                    {
                        _integrationContext.TargetPackageJson.Scripts["ck-watch"] = $"""
                        dotnet "$PROJECT_CWD/{relative}"
                        """;
                    }
                    else
                    {
                        monitor.Warn( $"""
                            Unable to compute reltive path from:
                            {BinPathConfiguration.TargetProjectPath}
                            to:
                            {liveEnginePath}
                            No 'yarn ck-watch' command available.
                            """ );
                        _integrationContext.TargetPackageJson.Scripts.Remove( "ck-watch" );
                    }
                    success &= _integrationContext.Run( monitor, saver );
                }
            }
        }
        return success;
    }

    /// <summary>
    /// Reusable helper (until we find a host for this one).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="folderPath">The fully qualified path to delete.</param>
    /// <param name="recursive">True to delete all files and folders. By default the folder must be empty.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool DeleteFolder( IActivityMonitor monitor, string folderPath, bool recursive = false )
    {
        Throw.CheckArgument( Path.IsPathFullyQualified( folderPath ) );
        int retryCount = 0;
        retry:
        try
        {
            if( Directory.Exists( folderPath ) )
            {
                Directory.Delete( folderPath, recursive );
            }
            if( File.Exists( folderPath ) )
            {
                monitor.Error( $"Unable to delete a folder: it is a file. Path: '{folderPath}'." );
                return false;
            }
            return true;
        }
        catch( DirectoryNotFoundException )
        {
            return !Path.Exists( folderPath );
        }
        catch( Exception ex )
        {
            if( retryCount++ > 5 )
            {
                monitor.Error( $"Unable to delete folder '{folderPath}'.", ex );
                return false;
            }
            if( retryCount == 1 ) monitor.Warn( $"Deleting folder '{folderPath}' failed. Retrying up tp 5 times." );
            Thread.Sleep( retryCount * 50 );
            goto retry;
        }
    }


}
