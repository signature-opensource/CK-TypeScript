using CK.Core;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System;
using System.Linq;
using CK.TypeScript.CodeGen;
using System.Collections.Immutable;
using Microsoft.Extensions.Hosting;
using CSemVer;
using System.Formats.Asn1;
using System.Text;

namespace CK.Setup
{

    public sealed partial class TypeScriptContext
    {
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
                    // Even if we don't know yet whether Yarn is installed and even in IntegrationMode None, we lookup
                    // the Yarn typescript sdk version to check TypeScript version homogeneity.
                    // Before compiling the ck-gen folder, we must ensure that the Yarn typescript sdk is installed: if it's not,
                    // package resolution fails miserably.
                    // We read the typeScriptSdkVersion here.
                    PackageDependency typeScriptDep = FindBestTypeScriptVersion( monitor,
                                                                                    BinPathConfiguration.TargetProjectPath,
                                                                                    _integrationContext?.TargetPackageJson,
                                                                                    out SVersion? typeScriptSdkVersion );
                    // The code MAY have declared an incompatible version...
                    if( !saver.GeneratedDependencies.AddOrUpdate( monitor, typeScriptDep, cloneAddedDependency: false ) )
                    {
                        return false;
                    }
                    // It is necessarily here.
                    var final = saver.GeneratedDependencies["typescript"];
                    if( final != typeScriptDep )
                    {
                        if( final.DependencyKind != DependencyKind.DevDependency )
                        {
                            monitor.Warn( $"Some package declared \"typescript\" as a '{final.DependencyKind}'. This has been corrected to a DevDependency." );
                            // Come on, code! typescript is a dev dependency.
                            final.UnconditionalSetDependencyKind( DependencyKind.DevDependency );
                        }
                        if( final.Version != typeScriptDep.Version )
                        {
                            monitor.Warn( $"Some package declared \"typescript\" in version '{final.Version.ToNpmString()}'. Using it." );
                            typeScriptDep = final;
                        }
                    }
                    if( typeScriptSdkVersion != null )
                    {
                        TypeScriptIntegrationContext.WarnDiffTypeScriptSdkVersion( monitor, typeScriptSdkVersion, typeScriptDep.Version.Base );
                    }
                    if( _integrationContext == null )
                    {
                        monitor.Info( "Skipping any TypeScript project setup since IntegrationMode is None." );
                    }
                    else
                    {
                        success &= _integrationContext.Run( monitor, saver, final, typeScriptSdkVersion );
                    }
                }
            }
            return success;
        }

        PackageDependency FindBestTypeScriptVersion( IActivityMonitor monitor,
                                                     NormalizedPath targetProjectPath,
                                                     PackageJsonFile? targetPackageJson,
                                                     out SVersion? typeScriptSdkVersion )
        {
            // Should we use ONLY this one if it exists?
            // Currently the target project version leads and we emit warinings... Because the idea is
            // to avoid changing the target project package.json.
            typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, targetProjectPath );

            var source = "target project";
            var targetTypeScriptVersion = targetPackageJson?.Dependencies.GetValueOrDefault("typescript")?.Version;
            if( targetTypeScriptVersion is null )
            {
                if( typeScriptSdkVersion == null )
                {
                    source = "BinPathConfiguration.AutomaticTypeScriptVersion property";
                    var parseResult = SVersionBound.NpmTryParse( BinPathConfiguration.AutomaticTypeScriptVersion );
                    Throw.DebugAssert( "The version defined in code is necessarily valid.", parseResult.IsValid );
                    targetTypeScriptVersion = parseResult.Result;
                }
                else
                {
                    source = "Yarn TypeScript sdk";
                    targetTypeScriptVersion = new SVersionBound( typeScriptSdkVersion, SVersionLock.Lock, PackageQuality.Stable );
                }
            }
            monitor.Info( $"Considering TypeScript version '{targetTypeScriptVersion}' from {source}." );
            return new PackageDependency( "typescript", targetTypeScriptVersion.Value, DependencyKind.DevDependency );
        }
    }
}
