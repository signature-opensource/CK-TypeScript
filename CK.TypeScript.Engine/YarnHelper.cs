using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CK.Setup;


/// <summary>
/// Provides helper for Node and Yarn.
/// </summary>
public static class YarnHelper
{
    /// <summary>
    /// The name of the Jest setup file of the previous versions of the engine: it is deleted when it is found
    /// in the target project (the setup is now in "jest.config.js" and "jest-setup.ts").
    /// </summary>
    public const string JestSetupFileName = "jest.CKTypeScriptEngine.ts";

    /// <summary>
    /// The current yarn version that is embedded in the CK.TypeScript.Engine assembly
    /// and can be automatically installed. See <see cref="TypeScriptBinPathAspectConfiguration.InstallYarn"/>.
    /// </summary>
    public const string AutomaticYarnVersion = "4.18.1";

    const string _yarnFileName = $"yarn-{AutomaticYarnVersion}.cjs";
    const string _autoYarnPath = $".yarn/releases/{_yarnFileName}";

    static YarnHelper()
    {
        if( !typeof( YarnHelper ).Assembly.GetManifestResourceNames().Contains( $"CK.TypeScript.Engine.{_yarnFileName}" ) )
        {
            Throw.CKException( $"CK.TypeScript.Engine.csproj must be updated with <EmbeddedResource Include=\"../.yarn/releases/{_yarnFileName}\" />" );
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
    public static bool RunYarn( IActivityMonitor monitor,
                                NormalizedPath workingDirectory,
                                string command,
                                Dictionary<string, string>? environmentVariables )
    {
        var yarnPath = TryFindYarn( workingDirectory, out _ );
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

    // Yarn writes the .yarn/sdks files with LF, except integrations.yml that uses CRLF. A repository-wide
    // "eol=crlf" checks them all out with CRLF: the size recorded in the index then differs from the
    // regenerated file and git reports them as modified even if their content is unchanged.
    // The rules below align the checkout with what Yarn writes.
    // The file itself is outside the "sdks/**" pattern: it is written with CRLF like any other text file.
    internal static void EnsureYarnSdksGitAttributes( IActivityMonitor monitor, NormalizedPath targetProjectPath )
    {
        var yarnPath = targetProjectPath.AppendPart( ".yarn" );
        if( !Directory.Exists( yarnPath.AppendPart( "sdks" ) ) ) return;
        var gitAttributes = yarnPath.AppendPart( ".gitattributes" );
        if( File.Exists( gitAttributes ) ) return;
        monitor.Info( $"Creating '{gitAttributes}' to check out the Yarn sdks files with LF line endings." );
        File.WriteAllText( gitAttributes, "# Yarn generates the sdks files with LF line endings, except integrations.yml.\r\nsdks/** text eol=lf\r\nsdks/integrations.yml text eol=crlf\r\n" );
    }

    internal static NormalizedPath? EnsureYarnInstallAndGetPath( IActivityMonitor monitor,
                                                                 NormalizedPath targetProjectPath,
                                                                 YarnInstallOption option,
                                                                 out Version? version )
    {
        var yarnPath = TryFindYarn( targetProjectPath, out var aboveCount );
        if( yarnPath.HasValue )
        {
            var current = yarnPath.Value.LastPart;
            if( current.StartsWith( "yarn-" )
                && current.Length > 5
                && Version.TryParse( Path.GetFileNameWithoutExtension( current.AsSpan( 5 ) ), out version ) )
            {
                monitor.Info( $"Yarn {version.ToString( 3 )} found at '{yarnPath}'." );
                if( version < Version.Parse( AutomaticYarnVersion ) )
                {
                    if( option == YarnInstallOption.AutoUpgrade )
                    {
                        monitor.Info( $"YarnInstall = AutoUpgrade: upgrading to Yarn {AutomaticYarnVersion}." );
                        yarnPath = AutoInstall( monitor, yarnPath.Value.RemoveLastPart( 3 ), yarnPath );
                        version = Version.Parse( AutomaticYarnVersion );
                    }
                    else
                    {
                        monitor.Info( $"YarnInstall = {option}. Skipping upgrade to {AutomaticYarnVersion}." );
                    }
                }
            }
            else
            {
                throw new CKException( $"Unable to read version from Yarn found at '{yarnPath}'. Expected something like '{_yarnFileName}'. This must be fixed manually" );
            }
        }
        else
        {
            if( option == YarnInstallOption.None )
            {
                monitor.Warn( $"No yarn found in '{targetProjectPath}' or above and YarnInstall is None." );
                version = null;
            }
            else
            {
                // The repository root is the folder of the git working tree. In a git worktree, it is
                // the worktree folder, not the main checkout: each worktree has its own shared yarn.
                if( !LocalDevSolution.TryFindSolutionFolder( targetProjectPath, out var yarnRootPath, out _, out _ ) )
                {
                    monitor.Warn( $"No '.git' found above to setup a shared yarn. Auto installing yarn in target '{targetProjectPath}'." );
                    yarnPath = AutoInstall( monitor, targetProjectPath, yarnPath );
                }
                else
                {
                    monitor.Info( $"Git root found: '{yarnRootPath}'. Setting up a shared .yarn cache." );
                    monitor.Info( $"No yarn found, we will add our own {_autoYarnPath} in '{yarnRootPath}'." );
                    yarnPath = AutoInstall( monitor, yarnRootPath, yarnPath );
                }
                version = Version.Parse( AutomaticYarnVersion );
            }
        }
        if( yarnPath.HasValue )
        {
            EnsureYarnRcFileAtYarnLevel( monitor, yarnPath.Value );
            EnsureInstallMarkerIgnored( monitor, yarnPath.Value.RemoveLastPart( 3 ) );
        }
        return yarnPath;

        static NormalizedPath AutoInstall( IActivityMonitor monitor,
                                            NormalizedPath yarnRootPath,
                                            NormalizedPath? previousYarnPath )
        {
            NormalizedPath yarnPath;
            var yarnBinDir = yarnRootPath.Combine( ".yarn/releases" );
            monitor.Trace( $"Extracting '{_yarnFileName}' to '{yarnBinDir}'." );
            Directory.CreateDirectory( yarnBinDir );
            yarnPath = yarnBinDir.AppendPart( _yarnFileName );
            var a = Assembly.GetExecutingAssembly();
            Throw.DebugAssert( a.GetName().Name == "CK.TypeScript.Engine" );
            using( var yarnBinStream = a.GetManifestResourceStream( $"CK.TypeScript.Engine.{_yarnFileName}" ) )
            using( var fileStream = File.Create( yarnPath ) )
            {
                yarnBinStream!.CopyTo( fileStream );
            }
            HandleGitIgnore( monitor, yarnRootPath );
            if( previousYarnPath.HasValue )
            {
                using( monitor.OpenInfo( $"Deleting old yarn runtime '{previousYarnPath.Value}'." ) )
                {
                    try
                    {
                        File.Delete( previousYarnPath.Value );
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( ex );
                    }
                }
            }
            return yarnPath;
        }
    }

    internal static void HandleGitIgnore( IActivityMonitor monitor, NormalizedPath yarnRootPath )
    {
        var gitIgnore = yarnRootPath.AppendPart( ".gitignore" );
        const string yarnDefault = """
                      # Yarn - Not using Zero-Install (.yarn/cache and .pnp.* are not commited).
                      .pnp.*
                      .yarn/*
                      !.yarn/.gitattributes
                      !.yarn/patches
                      !.yarn/plugins
                      !.yarn/releases
                      !.yarn/sdks
                      !.yarn/versions

                      # Because we can have subordinated .yarn folder we must exclude any .yarn/install-state.gz
                      # and yarn/unplugged since we don't use Zero-Install.
                      **/.yarn/install-state.gz
                      **/.yarn/unplugged
                      # The install marker of the CK.TypeScript engine (it describes the local install).
                      **/.yarn/ck-install-state

                      """;
        if( File.Exists( gitIgnore ) )
        {
            var ignore = File.ReadAllText( gitIgnore );
            if( !ignore.Contains( ".yarn/*" ) )
            {
                monitor.Info( $"No '.yarn/*' found in '{gitIgnore}'. Adding default section:{yarnDefault}" );
                if( ignore.Length > 0 && !ignore.EndsWith( '\n' ) ) ignore += Environment.NewLine;
                File.WriteAllText( gitIgnore, ignore + yarnDefault );
            }
            else
            {
                monitor.Info( $"At least '.yarn/*' found in '{gitIgnore}'. Skipping the injection of the default section:{yarnDefault}" );
                EnsureInstallMarkerIgnored( monitor, yarnRootPath );
            }
        }
        else
        {
            monitor.Info( $"No '{gitIgnore}' found. Creating one with the default section:{yarnDefault}" );
            File.WriteAllText( gitIgnore, yarnDefault );
        }
    }

    // The .gitignore of an existing yarn root (with the default section) must also ignore the install marker:
    // a committed marker would match the files of a "git pull" while the local install is older.
    internal static void EnsureInstallMarkerIgnored( IActivityMonitor monitor, NormalizedPath yarnRootPath )
    {
        var gitIgnore = yarnRootPath.AppendPart( ".gitignore" );
        if( !File.Exists( gitIgnore ) ) return;
        var ignore = File.ReadAllText( gitIgnore );
        if( !ignore.Contains( ".yarn/*" ) || ignore.Contains( InstallMarkerFileName ) ) return;
        var newLine = ignore.Contains( "\r\n" ) ? "\r\n" : "\n";
        if( ignore.Length > 0 && !ignore.EndsWith( '\n' ) ) ignore += newLine;
        ignore += $"{newLine}# The install marker of the CK.TypeScript engine (it describes the local install).{newLine}**/.yarn/{InstallMarkerFileName}{newLine}";
        monitor.Info( $"Adding '**/.yarn/{InstallMarkerFileName}' to '{gitIgnore}'." );
        File.WriteAllText( gitIgnore, ignore );
    }

    internal static void EnsureYarnRcFileAtYarnLevel( IActivityMonitor monitor, NormalizedPath yarnPath )
    {
        Throw.DebugAssert( yarnPath.Parts.Count > 3 && yarnPath.Parts[^3] == ".yarn" && yarnPath.Parts[^2] == "releases" );
        var firstLine = $"yarnPath: \"./{yarnPath.RemoveFirstPart( yarnPath.Parts.Count - 3 )}\"";
        var def = $"""
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
            if( !current.StartsWith( firstLine ) )
            {
                var lines = current.Split( '\n' ).Select( l => l.TrimEnd() ).ToList();
                int idx = lines.IndexOf( l => l.StartsWith( "yarnPath:" ) );
                if( idx == 0 ) lines[0] = firstLine;
                else 
                {
                    if( idx > 0 ) lines.RemoveAt( idx );
                    lines.Insert( 0, firstLine );
                }
                var newOne = string.Join( Environment.NewLine, lines );
                monitor.Info( $"""
                    Updated .yarnrc.yml from:
                    {current}
                    to:
                    {newOne}
                    """ );
                    
                File.WriteAllText( yarnrcFile, newOne );
            }
            else
            {
                monitor.Info( $"File '{yarnrcFile}' exists and has the right yarnPath, leaving it unchanged:{Environment.NewLine}{current}" );
            }
        }
        else
        {
            def = firstLine + Environment.NewLine + def;
            monitor.Info( $"Creating '{yarnrcFile}':{Environment.NewLine}{def}" );
            File.WriteAllText( yarnrcFile, def );
        }
    }

    /// <summary>
    /// The name of the install marker in the ".yarn" folder of a target project: the hash of the package.json and
    /// .yarnrc.yml texts of the last successful install (see <see cref="ComputeInstallHash"/>).
    /// </summary>
    public const string InstallMarkerFileName = "ck-install-state";

    internal static NormalizedPath GetInstallMarkerPath( NormalizedPath targetProjectPath ) => targetProjectPath.Combine( ".yarn/" + InstallMarkerFileName );

    /// <summary>
    /// Computes the install marker: the SHA-256 of the package.json text and the .yarnrc.yml text (empty when there is no file).
    /// </summary>
    /// <param name="packageJson">The package.json text.</param>
    /// <param name="yarnRc">The .yarnrc.yml text. Null when there is no file.</param>
    /// <returns>The hash, in hexadecimal.</returns>
    public static string ComputeInstallHash( string packageJson, string? yarnRc )
    {
        var bytes = Encoding.UTF8.GetBytes( packageJson + '\0' + yarnRc );
        return Convert.ToHexString( SHA256.HashData( bytes ) );
    }

    internal static bool HasInstallStateGZ( NormalizedPath targetProjectPath )
    {
        var integrationsFile = targetProjectPath.Combine( ".yarn/install-state.gz" );
        return File.Exists( integrationsFile );
    }

    internal static bool HasYarnLockFile( NormalizedPath targetProjectPath )
    {
        var f = targetProjectPath.AppendPart( "yarn.lock" );
        return File.Exists( f );
    }

    /// <summary>
    /// Gets whether the existing "yarn.lock" has the entry of the project's workspace (its resolution
    /// "<paramref name="name"/>@workspace:."). A package.json without name is not checked (yarn names its workspace).
    /// </summary>
    internal static bool HasWorkspaceEntry( NormalizedPath targetProjectPath, string? name )
    {
        if( string.IsNullOrWhiteSpace( name ) ) return true;
        var f = targetProjectPath.AppendPart( "yarn.lock" );
        return File.ReadAllText( f ).Contains( $"resolution: \"{name}@workspace:.\"", StringComparison.Ordinal );
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

    /// <summary>
    /// Acquires a machine-wide lock on the root folder of a yarn installation (the folder that contains
    /// ".yarn/releases"). Two "yarn install" processes that write the same package in the shared ".yarn/cache"
    /// can fail on Windows (orphan "*.zip-&lt;hash&gt;.tmp" files). The lock makes the installs on one yarn root
    /// run one after the other.
    /// <para>
    /// The lock is a named <see cref="Mutex"/>: it must be released on the thread that acquired it.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="yarnPath">The path of the yarn runtime (".yarn/releases/yarn-x.y.z.cjs").</param>
    /// <returns>A disposable that releases the lock.</returns>
    internal static IDisposable AcquireYarnRootLock( IActivityMonitor monitor, NormalizedPath yarnPath )
    {
        if( yarnPath.Parts.Count < 4 ) return Util.EmptyDisposable;
        var yarnRoot = yarnPath.RemoveLastPart( 3 );
        var hash = Convert.ToHexString( SHA1.HashData( Encoding.UTF8.GetBytes( yarnRoot.Path.ToUpperInvariant() ) ) );
        var mutex = new Mutex( false, $"CK.TypeScript.YarnRoot.{hash}" );
        try
        {
            if( !mutex.WaitOne( 0 ) )
            {
                monitor.Info( $"Waiting for another yarn install on '{yarnRoot}'." );
                if( !mutex.WaitOne( TimeSpan.FromMinutes( 10 ) ) )
                {
                    monitor.Warn( $"Timeout on the yarn root lock of '{yarnRoot}'. Continuing without it." );
                    mutex.Dispose();
                    return Util.EmptyDisposable;
                }
            }
        }
        catch( AbandonedMutexException )
        {
            // The previous owner exited without a release: this thread owns the mutex now.
        }
        return Util.CreateDisposableAction( () => { mutex.ReleaseMutex(); mutex.Dispose(); } );
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
            bool retriedInstallDone = false;
            retry:
            int code = RunProcess( monitor.ParallelLogger,
                                   "node", $"\"{yarnPath}\" {command}",
                                   workingDirectory,
                                   environmentVariables,
                                   out bool foundInstallYarnErrorMessage );
            if( code != 0 )
            {
                string failedMessage = $"'yarn {command}' failed with code {code}.";
                if( foundInstallYarnErrorMessage && !retriedInstallDone )
                {
                    using( monitor.OpenError( failedMessage + " Running a yarn install and retrying." ) )
                    {
                        // Parallel test runs of the same yarn root do this at the same time: their installs share its ".yarn/cache".
                        using( AcquireYarnRootLock( monitor, yarnPath ) )
                        {
                            code = RunProcess( monitor.ParallelLogger,
                                               "node", $"\"{yarnPath}\" install",
                                               workingDirectory,
                                               environmentVariables,
                                               out _ );
                        }
                        if( code != 0 )
                        {
                            monitor.CloseGroup( "Failed." );
                            return false;
                        }
                        retriedInstallDone = true;
                        goto retry;
                    }
                }
                else
                {
                    monitor.Error( failedMessage );
                }
                return false;
            }
        }
        return true;
    }

    #region ProcessRunner for NodeBuild

    static CKTrait StdErrTag = ActivityMonitor.Tags.Register( "StdErr" );
    static CKTrait StdOutTag = ActivityMonitor.Tags.Register( "StdOut" );
    static readonly TimeSpan _outputEndTimeout = TimeSpan.FromSeconds( 5 );

    internal static int RunProcess( IParallelLogger logger,
                                    string fileName,
                                    string arguments,
                                    string workingDirectory,
                                    Dictionary<string, string>? environmentVariables,
                                    out bool foundInstallYarnErrorMessage )
    {
        return RunProcess( logger,
                           fileName,
                           arguments,
                           workingDirectory,
                           environmentVariables?.ToDictionary( kv => kv.Key, kv => (string?)kv.Value ),
                           outputLines: null,
                           timeout: TimeSpan.Zero,
                           out foundInstallYarnErrorMessage ).ExitCode;
    }

    /// <summary>
    /// Runs a process and logs its standard output and error lines at the Trace level.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="fileName">The process file name.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="environmentVariables">
    /// Optional variables that override the inherited environment. A null value removes the variable.
    /// </param>
    /// <param name="outputLines">Optional list that receives the standard output and error lines.</param>
    /// <param name="timeout">
    /// Zero or negative for no limit. Otherwise the process tree is killed when the process still runs after it,
    /// and the wait for the end of the output is bounded too.
    /// </param>
    /// <param name="foundInstallYarnErrorMessage">True when the standard output contains "running an install might help".</param>
    /// <returns>
    /// The exit code and whether the process has been killed by the timeout. The exit code is -1 when the killed
    /// process has still not ended once the wait for the end of the output is over.
    /// </returns>
    internal static (int ExitCode, bool TimedOut) RunProcess( IParallelLogger logger,
                                                              string fileName,
                                                              string arguments,
                                                              string workingDirectory,
                                                              Dictionary<string, string?>? environmentVariables,
                                                              List<string>? outputLines,
                                                              TimeSpan timeout,
                                                              out bool foundInstallYarnErrorMessage )
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
            // Environment already holds the inherited variables: Add would throw on them.
            foreach( var kv in environmentVariables )
            {
                if( kv.Value == null )
                {
                    info.Environment.Remove( kv.Key );
                }
                else
                {
                    info.Environment[kv.Key] = kv.Value;
                }
            }
        }

        bool localFoundInstallYarnErrorMessage = false;
        bool outputClosed = false;
        using var process = new Process { StartInfo = info };
        process.OutputDataReceived += ( sender, data ) =>
        {
            if( data.Data != null )
            {
                localFoundInstallYarnErrorMessage |= data.Data.Contains( "running an install might help" );
                logger.Trace( StdOutTag, data.Data );
                Collect( data.Data );
            }
        };
        process.ErrorDataReceived += ( sender, data ) =>
        {
            if( data.Data != null )
            {
                logger.Trace( StdErrTag, data.Data );
                Collect( data.Data );
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        bool timedOut = false;
        if( timeout <= TimeSpan.Zero )
        {
            process.WaitForExit();
        }
        else
        {
            if( !process.WaitForExit( timeout ) )
            {
                process.Kill( entireProcessTree: true );
                timedOut = true;
            }
            // The end of both streams is awaited for a bounded time: a process that left the tree can keep them open.
            using var cts = new CancellationTokenSource( _outputEndTimeout );
            try
            {
                process.WaitForExitAsync( cts.Token ).GetAwaiter().GetResult();
            }
            catch( OperationCanceledException )
            {
                if( process.HasExited )
                {
                    logger.Warn( $"The output of '{fileName} {arguments}' is still open {_outputEndTimeout.TotalSeconds} seconds after the process exit: it may be incomplete." );
                }
                else
                {
                    logger.Warn( $"'{fileName} {arguments}' has not ended {_outputEndTimeout.TotalSeconds} seconds after it was killed." );
                }
            }
        }
        if( outputLines != null )
        {
            // The caller reads the list once this method returns: later lines are dropped.
            lock( outputLines )
            {
                outputClosed = true;
            }
        }
        foundInstallYarnErrorMessage = localFoundInstallYarnErrorMessage;
        // Only a killed process can still run here: -1 is also what a killed process exits with on Windows.
        return (process.HasExited ? process.ExitCode : -1, timedOut);

        void Collect( string line )
        {
            if( outputLines != null )
            {
                lock( outputLines )
                {
                    if( !outputClosed )
                    {
                        outputLines.Add( line );
                    }
                }
            }
        }
    }

    #endregion
}
