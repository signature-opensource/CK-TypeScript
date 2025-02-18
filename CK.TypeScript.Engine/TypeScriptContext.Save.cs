using CK.Core;
using CK.TypeScript.CodeGen;
using CK.TypeScript.LiveEngine;
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

            // If live state must be built, it's time to build it.
            var liveState = _initializer.LiveState;
            if( liveState != null )
            {
                using( monitor.OpenInfo( "Initializing ck-watch live state." ) )
                {
                    liveState.ClearState( monitor );
                    foreach( var p in _initializer.Packages )
                    {
                        if( p.LocalResPath != null )
                        {
                            liveState.AddLocalPackage( monitor, p.LocalResPath, p.Resources.DisplayName );
                        }
                        else
                        {
                            liveState.AddRegularPackage( monitor, p.TSLocales, p.Assets );
                        }
                    }
                }
            }

            // If a ck-gen/dist folder exists, we delete it no matter what.
            // This applies to NpmPackage integration mode. 
            // When UseSrcFolder is false, its files appear in the list of cleanup files and
            // it should be recompiled anyway.
            // In Inline mode, there is no dist/. And when there is no integration at all, the /dist
            // shouldn't exist.
            string distFolder = ckGenFolder + "/dist";
            if( Directory.Exists( distFolder ) )
            {
                monitor.Info( "Found a 'ck-gen/dist' folder. Deleting it" );
                DeleteFolder( monitor, distFolder, recursive: true );
            }
            var saver = BinPathConfiguration.CKGenBuildMode
                        ? new BuildModeSaver( Root, targetCKGenFolder )
                        : new TypeScriptFileSaveStrategy( Root, targetCKGenFolder );
            // We want a root barrel for the generated module.
            Root.Root.EnsureBarrel();
            // If we are not using the ck-gen/src folder, ignore the files that are not
            // directly concerned by the code generation.
            if( !_binPathConfiguration.UseSrcFolder )
            {
                saver.CleanupIgnoreFiles.Add( ".gitignore" );
                if( _binPathConfiguration.IntegrationMode != CKGenIntegrationMode.None )
                {
                    var prefix = _binPathConfiguration.IntegrationMode != CKGenIntegrationMode.NpmPackage
                                    ? "CouldBe."
                                    : null;
                    saver.CleanupIgnoreFiles.Add( prefix + "package.json" );
                    saver.CleanupIgnoreFiles.Add( prefix + "tsconfig.json" );
                    saver.CleanupIgnoreFiles.Add( prefix + "tsconfig-cjs.json" );
                    saver.CleanupIgnoreFiles.Add( prefix + "tsconfig-es6.json" );
                }
            }
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
                    if( liveState != null )
                    {
                        _integrationContext.TargetPackageJson.Scripts["ck-watch"] = $"""
                            dotnet "{typeof(LiveState).Assembly.Location}"
                            """;
                    }
                    success &= _integrationContext.Run( monitor, saver );
                }
            }
            // Whatever occured above, if we have a live state, write it.
            liveState?.WriteState( monitor );
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
