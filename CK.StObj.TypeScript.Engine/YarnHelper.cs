using CK.Core;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace CK.Setup;


/// <summary>
/// Provides helper for Node and Yarn.
/// </summary>
public static class YarnHelper
{
    /// <summary>
    /// The Jest's setupFile name.
    /// </summary>
    public const string JestSetupFileName = "jest.CKTypeScriptEngine.ts";

    /// <summary>
    /// The current yarn version that is embedded in the CK.StObj.TypeScript.Engine assembly
    /// and can be automatically installed. See <see cref="TypeScriptBinPathAspectConfiguration.AutoInstallYarn"/>.
    /// </summary>
    public const string AutomaticYarnVersion = "4.3.1";

    const string _yarnFileName = $"yarn-{AutomaticYarnVersion}.cjs";
    const string _autoYarnPath = $".yarn/releases/{_yarnFileName}";

    static YarnHelper()
    {
        if( !typeof( YarnHelper ).Assembly.GetManifestResourceNames().Contains( $"CK.StObj.TypeScript.Engine.{_yarnFileName}" ) )
        {
            Throw.CKException( $"CK.StObj.TypeScript.Engine.csproj must be updated with <EmbeddedResource Include=\"../.yarn/releases/{_yarnFileName}\" />" );
        }
    }

    /// <summary>
    /// Locates yarn in <paramref name="workingDirectory"/> or above and calls it with the provided <paramref name="command"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="command">The command to run.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <returns>True on success, false if yarn cannot be found or the process failed.</returns>
    public static bool RunYarn( IActivityMonitor monitor, NormalizedPath workingDirectory, string command, Dictionary<string, string>? environmentVariables )
    {
        var yarnPath = TryFindYarn( workingDirectory, out var _ );
        if( yarnPath.HasValue )
        {
            return DoRunYarn( monitor, workingDirectory, command, yarnPath.Value, environmentVariables );
        }
        monitor.Error( $"Unable to find yarn in '{workingDirectory}' or above." );
        return false;
    }

    // Reads the typescript version from .yarn/sdks/typescript/package.json or returns null if the
    // Yarn TypeScript sdk is not installed or the version cannot be read.
    internal static SVersion? GetYarnSdkTypeScriptVersion( IActivityMonitor monitor, NormalizedPath targetProjectPath )
    {
        var sdkTypeScriptPath = targetProjectPath.Combine( ".yarn/sdks/typescript/package.json" );
        // We don't care of the ignoreVersionsBound (we'll never merge the versions).
        var packageJson = PackageJsonFile.ReadFile( monitor, sdkTypeScriptPath, "Yarn sdk package.json", ignoreVersionsBound: true );
        if( packageJson == null ) return null;
        if( packageJson.IsEmpty )
        {
            monitor.Warn( $"Missing expected '{sdkTypeScriptPath}' to be able to read the Yarn sdk TypeScript version." );
            return null;
        }
        if( packageJson.Version == null )
        {
            monitor.Warn( $"Missing version in '{sdkTypeScriptPath}'. Unable to read the Yarn sdk TypeScript version." );
            return null;
        }
        if( !packageJson.Version.Prerelease.Equals( "sdk", StringComparison.OrdinalIgnoreCase ) )
        {
            monitor.Warn( $"Invalid Yarn sdks typescript version '{packageJson.Version}'. It should end with '-sdk'.{Environment.NewLine}File: '{sdkTypeScriptPath}'." );
            return null;
        }
        return SVersion.Create( packageJson.Version.Major, packageJson.Version.Minor, packageJson.Version.Patch );
    }

    internal static NormalizedPath? GetYarnInstallPath( IActivityMonitor monitor, NormalizedPath targetProjectPath, bool autoInstall )
    {
        var yarnPath = TryFindYarn( targetProjectPath, out var aboveCount );
        if( yarnPath.HasValue )
        {
            var current = yarnPath.Value.LastPart;
            if( current.StartsWith("yarn-")
                && current.Length > 5
                && Version.TryParse( Path.GetFileNameWithoutExtension( current.AsSpan( 5 ) ), out var version ) )
            {
                if( version.Major < 4 )
                {
                    monitor.Warn( $"Yarn found at '{yarnPath}' but expected version is Yarn 4. " +
                                  $"Please upgrade to Yarn 4: yarn set version {AutomaticYarnVersion}." );

                }
                else
                {
                    monitor.Info( $"Yarn {version.ToString(3)} found at '{yarnPath}'." );
                }
            }
            else
            {
                monitor.Warn( $"Unable to read version from Yarn found at '{yarnPath}'. Expected something like '{_yarnFileName}'." );
            }
        }
        else if( autoInstall )
        {
            var gitRoot = targetProjectPath.PathsToFirstPart( null, [".git"] ).FirstOrDefault( p => Directory.Exists( p ) );
            if( gitRoot.IsEmptyPath )
            {
                monitor.Warn( $"No '.git' found above to setup a shared yarn. Auto installing yarn in target '{targetProjectPath}'." );
                yarnPath = AutoInstall( monitor, targetProjectPath, 0 );
            }
            else
            {
                Throw.DebugAssert( gitRoot.LastPart == ".git" );
                monitor.Info( $"Git root found: '{gitRoot}'. Setting up a shared .yarn cache." );
                aboveCount = targetProjectPath.Parts.Count - gitRoot.Parts.Count + 1;
                yarnPath = AutoInstall( monitor, targetProjectPath, aboveCount );
            }
        }
        if( !yarnPath.HasValue )
        {
            monitor.Warn( $"No yarn found in '{targetProjectPath}' or above and AutoInstallYarn is false." );
        }
        else
        {
            EnsureYarnRcFileAtYarnLevel( monitor, yarnPath.Value );
        }
        return yarnPath;

        static NormalizedPath? AutoInstall( IActivityMonitor monitor, NormalizedPath targetProjectPath, int aboveCount )
        {
            NormalizedPath? yarnPath;
            var yarnRootPath = targetProjectPath.RemoveLastPart( aboveCount );
            monitor.Info( $"No yarn found, we will add our own {_autoYarnPath} in '{yarnRootPath}'." );
            var yarnBinDir = yarnRootPath.Combine( ".yarn/releases" );
            monitor.Trace( $"Extracting '{_yarnFileName}' to '{yarnBinDir}'." );
            Directory.CreateDirectory( yarnBinDir );
            yarnPath = yarnBinDir.AppendPart( _yarnFileName );
            var a = Assembly.GetExecutingAssembly();
            Throw.DebugAssert( a.GetName().Name == "CK.StObj.TypeScript.Engine" );
            using( var yarnBinStream = a.GetManifestResourceStream( $"CK.StObj.TypeScript.Engine.{_yarnFileName}" ) )
            using( var fileStream = File.OpenWrite( yarnPath ) )
            {
                yarnBinStream!.CopyTo( fileStream );
            }
            HandleGitIgnore( monitor, yarnRootPath );
            return yarnPath;

            static void HandleGitIgnore( IActivityMonitor monitor, NormalizedPath yarnRootPath )
            {
                var gitIgnore = yarnRootPath.AppendPart( ".gitignore" );
                const string yarnDefault = """
                              # Yarn - Not using Zero-Install (.yarn/cache and .pnp.* are not commited).
                              .pnp.*
                              .yarn/*
                              !.yarn/patches
                              !.yarn/plugins
                              !.yarn/releases
                              !.yarn/sdks
                              !.yarn/versions

                              # Because we can have subordinated .yarn folder we must exclude any .yarn/install-state.gz
                              # and yarn/unplugged since we don't use Zero-Install.
                              **/.yarn/install-state.gz
                              **/.yarn/unplugged

                              """;
                if( File.Exists( gitIgnore ) )
                {
                    var ignore = File.ReadAllText( gitIgnore );
                    if( !ignore.Contains( ".yarn/*" ) )
                    {
                        monitor.Info( $"No '.yarn/*' found in '{gitIgnore}'. Adding default section:{yarnDefault}" );
                        ignore += yarnDefault;
                    }
                    else
                    {
                        monitor.Info( $"At least '.yarn/*' found in '{gitIgnore}'. Skipping the injection of the default section:{yarnDefault}" );
                    }
                }
                else
                {
                    monitor.Info( $"No '{gitIgnore}' found. Creating one with the default section:{yarnDefault}" );
                    File.WriteAllText( gitIgnore, yarnDefault );
                }
            }
        }
    }

    static void EnsureYarnRcFileAtYarnLevel( IActivityMonitor monitor, NormalizedPath yarnPath )
    {
        Throw.DebugAssert( yarnPath.Parts.Count > 3 && yarnPath.Parts[^3] == ".yarn" && yarnPath.Parts[^2] == "releases" );
        var def = $"""
                   yarnPath: "./{yarnPath.RemoveFirstPart( yarnPath.Parts.Count - 3 )}"

                   # We don't use Zero Install: compression level defaults to 0 (no compression) in yarn 4
                   # because 0 (no compression) is slightly better for git. As we don't commit the packages,
                   # we continue to use the yarn 3 default compression mode.
                   compressionLevel: mixed

                   # We prevent Yarn to query the remote registries to validate that the lockfile
                   # content matches the remote information.
                   enableHardenedMode: false

                   # cacheFolder: "./.yarn/cache", enableGlobalCache: false and enableMirror: false
                   # Let each repository have its local cache, independent from any global cache.
                   cacheFolder: "./.yarn/cache"
                   enableGlobalCache: false
                   enableMirror: false

                   """;
        var yarnrcFile = yarnPath.RemoveLastPart( 3 ).AppendPart( ".yarnrc.yml" );
        if( File.Exists( yarnrcFile ) )
        {
            var current = File.ReadAllText( yarnrcFile );
            monitor.Info( $"File '{yarnrcFile}' exists, leaving it unchanged:{Environment.NewLine}{current}" );
        }
        else
        {
            monitor.Info( $"Creating '{yarnrcFile}':{Environment.NewLine}{def}" );
            File.WriteAllText( yarnrcFile, def );
        }
    }

    internal static bool HasVSCodeSupport( NormalizedPath targetProjectPath )
    {
        var integrationsFile = targetProjectPath.Combine( ".yarn/sdks/integrations.yml" );
        if( !File.Exists( integrationsFile ) ) return false;
        return File.ReadAllText( integrationsFile ).Contains( "- vscode" );
    }

    static NormalizedPath? TryFindYarn( NormalizedPath currentDirectory, out int aboveCount )
    {
        // Here we should find a .yarnrc.yml:
        //  - consider its yarnPath: "..." property.
        //  - return a YarnInfo that is a (YarnRCPath,YarnPath) tuple.
        //
        // var yarnRc = currentDirectory.PathsToFirstPart( null, new[] { ".yarnrc.yml" } ).FirstOrDefault( p => Directory.Exists( p ) );
        //
        // For the moment, we only handle .yarn/release/*js file.
        aboveCount = 0;
        while( currentDirectory.HasParts )
        {
            NormalizedPath releases = currentDirectory.Combine( ".yarn/releases" );
            if( Directory.Exists( releases ) )
            {
                var yarn = Directory.GetFiles( releases )
                    .Select( s => Path.GetFileName( s ) )
                    // There is no dot on purpose, a js file can be js/mjs/cjs/whatever they invent next.
                    .Where( s => s.StartsWith( "yarn" ) && s.EndsWith( "js" ) )
                    .FirstOrDefault();
                if( yarn != null ) return releases.AppendPart( yarn );
            }
            currentDirectory = currentDirectory.RemoveLastPart();
            aboveCount++;
        }
        return default;
    }

    internal static bool DoRunYarn( IActivityMonitor monitor,
                                    NormalizedPath workingDirectory,
                                    string command,
                                    NormalizedPath yarnPath,
                                    Dictionary<string, string>? environmentVariables = null )
    {
        using( monitor.OpenInfo( $"Running 'yarn {command}' in '{workingDirectory}'{(environmentVariables == null || environmentVariables.Count == 0
                                                                                        ? ""
                                                                                        : $" with {environmentVariables.Select( kv => $"'{kv.Key}': '{kv.Value}'" ).Concatenate()}")}." ) )
        {
            int code = RunProcess( monitor.ParallelLogger, "node", $"\"{yarnPath}\" {command}", workingDirectory, environmentVariables );
            if( code != 0 )
            {
                monitor.Error( $"'yarn {command}' failed with code {code}." );
                return false;
            }
        }
        return true;
    }

    #region ProcessRunner for NodeBuild

    static CKTrait StdErrTag = ActivityMonitor.Tags.Register( "StdErr" );
    static CKTrait StdOutTag = ActivityMonitor.Tags.Register( "StdOut" );

    internal static int RunProcess( IParallelLogger logger,
                                    string fileName,
                                    string arguments,
                                    string workingDirectory,
                                    Dictionary<string,string>? environmentVariables )
    {
        var info = new ProcessStartInfo( fileName, arguments )
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        if( environmentVariables != null && environmentVariables.Count > 0 )
        {
            foreach( var kv in environmentVariables ) info.EnvironmentVariables.Add( kv.Key, kv.Value );
        }

        using var process = new Process { StartInfo = info };
        process.OutputDataReceived += ( sender, data ) =>
        {
            if( data.Data != null ) logger.Trace( StdOutTag, data.Data );
        };
        process.ErrorDataReceived += ( sender, data ) =>
        {
            if( data.Data != null ) logger.Trace( StdErrTag, data.Data );
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return process.ExitCode;

    }

    #endregion
}
