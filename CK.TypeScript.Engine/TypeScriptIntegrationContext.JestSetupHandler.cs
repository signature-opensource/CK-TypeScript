using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CK.Setup;

public sealed partial class TypeScriptIntegrationContext // JestSetup
{
    /// <summary>
    /// Jest related configuration is centralized in this class that can be specialized
    /// and replaced thanks to <see cref="BaseEventArgs.JestSetup"/>.
    /// <para>
    /// Both <see cref="OnBeforeIntegration"/> and <see cref="OnAfterIntegration"/> can change the handler
    /// as it will <see cref="Run(IActivityMonitor)"/> as the last step of a successful integration.
    /// </para>
    /// </summary>
    public class JestSetupHandler
    {
        /// <summary>
        /// The Jest's config file name.
        /// </summary>
        public const string JestConfigFileName = "jest.config.js";

        /// <summary>
        /// The jest's TS setup name.
        /// </summary>
        public const string JestSetupTSFileName = "jest-setup.ts";

        /// <summary>
        /// CKTypeScriptEnv key used in <see cref="JestConfigFileName"/> with a "true" value when
        /// tests are running.
        /// </summary>
        public const string TestRunningKey = "CK_TYPESCRIPT_ENGINE";

        readonly TypeScriptIntegrationContext _context;
        readonly NormalizedPath _jestConfigFilePath;
        readonly NormalizedPath _jestSetupTSFilePath;

        /// <summary>
        /// Initializes a new Jest handler.
        /// </summary>
        /// <param name="context">The integration context.</param>
        public JestSetupHandler( TypeScriptIntegrationContext context )
        {
            _context = context;

            NormalizedPath targetProjectPath = _context.Configuration.TargetProjectPath;

            _jestConfigFilePath = targetProjectPath.AppendPart( JestConfigFileName );
            _jestSetupTSFilePath = targetProjectPath.AppendPart( JestSetupTSFileName );
        }

        /// <summary>
        /// Gets the integration context.
        /// </summary>
        public TypeScriptIntegrationContext Context => _context;

        /// <summary>
        /// Gets the target project path.
        /// </summary>
        public NormalizedPath TargetProjectPath => _context.Configuration.TargetProjectPath;

        /// <summary>
        /// Gets the /src folder. It necessarily exists.
        /// </summary>
        public NormalizedPath SrcFolderPath => _context.SrcFolderPath;

        /// <summary>
        /// Gets the <see cref="JestConfigFileName"/> path.
        /// </summary>
        public NormalizedPath JestConfigFilePath => _jestConfigFilePath;

        /// <summary>
        /// Gets the <see cref="JestSetupTSFileName"/> path.
        /// </summary>
        public NormalizedPath JestSetupTSFilePath => _jestSetupTSFilePath;

        internal bool DoRun( IActivityMonitor monitor )
        {
            using var _ = monitor.OpenInfo( $"Running {GetType().Name}." );
            return Run( monitor );
        }

        /// <summary>
        /// Calls <see cref="WriteJestConfigFile(IActivityMonitor)"/> and <see cref="EnsureSampleJestTestInSrcFolder(IActivityMonitor)"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false otherwise (error should be logged).</returns>
        protected virtual bool Run( IActivityMonitor monitor )
        {
            var legacyFile = TargetProjectPath.AppendPart( "jest.CKTypeScriptEngine.ts" );
            if( File.Exists( legacyFile ) )
            {
                monitor.Info( "Removing no more used 'jest.CKTypeScriptEngine.ts' file." );
                File.Delete( legacyFile );
            }
            return WriteJestConfigFile( monitor )
                   && EnsureSampleJestTestInSrcFolder( monitor )
                   && WriteJestSetupTSFile( monitor );
        }

        /// <summary>
        /// Ensures that at least one ".spec.ts" exists in <see cref="SrcFolderPath"/> and if not, creates a dummy one.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false otherwise (error should be logged). At this level, always true.</returns>
        protected virtual bool EnsureSampleJestTestInSrcFolder( IActivityMonitor monitor )
        {
            var existingTestFile = Directory.EnumerateFiles( SrcFolderPath, "*.spec.ts", SearchOption.AllDirectories ).FirstOrDefault();
            if( existingTestFile != null )
            {
                monitor.Info( $"At least a test file exists in 'src' folder: skipping 'src/sample.spec.ts' creation (found '{existingTestFile}')." );
                return true;
            }
            var sampleTestPath = SrcFolderPath.AppendPart( "sample.spec.ts" );
            monitor.Info( $"Creating 'src/sample.spec.ts' test file." );
            File.WriteAllText( sampleTestPath, """
            // Trick from https://stackoverflow.com/a/77047461/190380
            // When debugging ("Debug Test at Cursor" in menu), this cancels jest timeout.
            if( process.env["VSCODE_INSPECTOR_OPTIONS"] ) jest.setTimeout(30 * 60 * 1000 ); // 30 minutes

            // Sample test.
            describe('Sample test', () => {
                it('should be true', () => {
                    expect(true).toBeTruthy();
                });
                });
            """ );
            return true;
        }

        /// <summary>
        /// Reusable detection of the first line "-AllowAutoUpdate" and content change.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="content">The content that should be updated.</param>
        /// <returns>True if <paramref name="content"/> should be written in <see cref="JestConfigFileName"/>, false if it shouldn't.</returns>
        protected static bool ShouldWriteAllowAutoUpdateFile( IActivityMonitor monitor, NormalizedPath filePath, string content )
        {
            Throw.CheckArgument( filePath.Parts.Count > 1 );
            Throw.CheckArgument( content != null && content.StartsWith( "//-AllowAutoUpdate" ) );

            if( File.Exists( filePath ) )
            {
                var current = File.ReadAllText( filePath );
                if( !current.StartsWith( "//-AllowAutoUpdate" )
                    /*Previous version for jest.config.js.*/ && !current.StartsWith( "//-Auto-Version:" ) )
                {
                    monitor.Info( $"File '{filePath.LastPart}' exists and doesn't start with '-AllowAutoUpdate'. Skipping any updates." );
                    return false;
                }
                if( content == current )
                {
                    monitor.Trace( $"File '{filePath.LastPart}' exists and is up to date." );
                    return false;
                }
                monitor.Trace( $"Updating '{filePath.LastPart}'." );
            }
            else
            {
                monitor.Info( $"Creating '{filePath.LastPart}'." );
            }
            return true;
        }

        /// <summary>
        /// Updates the <see cref="JestConfigFileName"/>.
        /// The "setupFilesAfterEnv" must contain the <see cref="JestSetupTSFileName"/> and globals definition
        /// with CKTypeScriptEnv and its marker comments.
        /// <para>
        /// This file can be "locked" by removing the first line "-AllowAutoUpdate".
        /// <see cref="ShouldWriteAllowAutoUpdateFile(IActivityMonitor, NormalizedPath, string)"/> takes care of this and can be reused.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false otherwise (error should be logged). At this level, always true.</returns>
        protected virtual bool WriteJestConfigFile( IActivityMonitor monitor )
        {
            var content = $$"""
                //-AllowAutoUpdate
                // Notes:
                //   - To settle this file, simply remove the //-AllowAutoUpdate
                //     first line: it will not be updated.
                //   - Jest is not ESM compliant. Using CJS here.
                //
                const { pathsToModuleNameMapper } = require('ts-jest');
                const { compilerOptions } = require('./tsconfig');

                module.exports = {
                    moduleFileExtensions: ['js', 'json', 'ts'],
                    testRegex: 'src/.*\\.spec\\.ts$',
                    transform: {
                        '^.+\\.ts$': ['ts-jest', {
                            // Removes annoying ts-jest[config] (WARN) message TS151001: If you have issues related to imports, you should consider...
                            diagnostics: {ignoreCodes: ['TS151001']}
                        }],
                    },
                    moduleNameMapper: pathsToModuleNameMapper(compilerOptions.paths ?? {}, { prefix: '<rootDir>/' } ),
                    testEnvironment: 'jsdom',
                    globals: {
                        // CK.Testing.TypeScriptEngine uses this:
                        // TestHelper.CreateTypeScriptTestRunner inject test environment variables here
                        // in a "persistent" way: these environment variables will be available until
                        // the Runner returned by TestHelper.CreateTypeScriptTestRunner is disposed.
                        // The CKTypeScriptEnv is then available to the tests (with whatever required like urls of
                        // backend servers).
                        // Do NOT alter the comments below.
                        // Start-CKTypeScriptEnv
                        CKTypeScriptEnv: {}
                        // Stop-CKTypeScriptEnv
                    },
                    // TestHelper.CreateTypeScriptTestRunner also replaces the 'http://localhost' (that is the default)
                    // with the server address (like 'http://[::1]:55235' - a free dynamic port is used). 
                    // Do NOT alter the comments below.
                    testEnvironmentOptions: { 
                        // Start-CKTypeScriptSrv
                        url: 'http://localhost' 
                        // Stop-CKTypeScriptSrv
                    },
                    setupFilesAfterEnv: ['<rootDir>/{{JestSetupTSFileName}}']
                };
                """;
            if( ShouldWriteAllowAutoUpdateFile( monitor, JestConfigFilePath, content ) )
            {
                File.WriteAllText( JestConfigFilePath, content );
            }
            return true;
        }

        /// <summary>
        /// Writes the <see cref="JestSetupTSFileName"/>.
        /// <para>
        /// This file can be "locked" by removing the first line "-AllowAutoUpdate".
        /// <see cref="ShouldWriteAllowAutoUpdateFile(IActivityMonitor, NormalizedPath, string)"/> takes care of this and can be reused.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false otherwise (error should be logged). At this level, always true.</returns>
        protected virtual bool WriteJestSetupTSFile( IActivityMonitor monitor )
        {
            const string content = """
                //-AllowAutoUpdate

                // Makes this file a module (see https://stackoverflow.com/questions/57132428/augmentations-for-the-global-scope-can-only-be-directly-nested-in-external-modul).
                export {} 

                // Augments the global with our CKTypeScriptEnv. See https://stackoverflow.com/questions/59459312/using-globalthis-in-typescript
                declare global { var CKTypeScriptEnv: { [key: string]: string}; }

                // This fixes a bug in testEnvionment: 'jsdom'. See https://stackoverflow.com/questions/68468203/why-am-i-getting-textencoder-is-not-defined-in-jest
                const { TextEncoder, TextDecoder } = require('util');
                if (typeof globalThis.TextEncoder === 'undefined') {
                    globalThis.TextEncoder = TextEncoder;
                    globalThis.TextDecoder = TextDecoder;
                }
                """;
            if( ShouldWriteAllowAutoUpdateFile( monitor, JestSetupTSFilePath, content ) )
            {
                File.WriteAllText( JestSetupTSFilePath, content );
            }
            return true;
        }

        static void InjectConfigJS( NormalizedPath jestConfigFilePath,
                                    string? serverAdress,
                                    IEnumerable<KeyValuePair<string, string>>? environmentVariables )
        {
            var content = File.ReadAllText( jestConfigFilePath );
            var (iStartEnv, iStopEnv) = GetRange( jestConfigFilePath, content, "Env" );
            var (iStartSrv, iStopSrv) = GetRange( jestConfigFilePath, content, "Srv" );
            ReadOnlySpan<char> head;
            ReadOnlySpan<char> middle;
            ReadOnlySpan<char> tail;
            bool envFirst = true;
            if( iStartEnv < iStartSrv )
            {
                var middleLen = iStartSrv - iStopEnv;
                if( middleLen <= 0 ) ThrowMismatch( jestConfigFilePath, content );
                head = content.AsSpan( 0, iStartEnv );
                middle = content.AsSpan( iStopEnv, middleLen );
                tail = content.AsSpan( iStopSrv );
            }
            else
            {
                var middleLen = iStartEnv - iStopSrv;
                if( middleLen <= 0 ) ThrowMismatch( jestConfigFilePath, content );
                head = content.AsSpan( 0, iStartSrv );
                middle = content.AsSpan( iStopSrv, middleLen );
                tail = content.AsSpan( iStopEnv );
                envFirst = false;
            }
            using var f = File.Create( jestConfigFilePath );
            using var text = new StreamWriter( f );
            text.Write( head );
            WriteReplacement( envFirst, serverAdress, environmentVariables, f, text );
            text.Write( middle );
            WriteReplacement( !envFirst, serverAdress, environmentVariables, f, text );
            text.Write( tail );

            static void ThrowMismatch( NormalizedPath jestConfigFilePath, string content )
            {
                throw new InvalidDataException( $"""
                File '{jestConfigFilePath}' is invalid. Markers are overlapping. Content: 
                {content}
                """ );
            }

            static (int, int) GetRange( NormalizedPath jestConfigFilePath, string content, string markSuffix )
            {
                Throw.DebugAssert( markSuffix.Length == 3 );
                var startPattern = $"// Start-CKTypeScript{markSuffix}";
                var stopPattern = $"// Stop-CKTypeScript{markSuffix}";
                var idxStart = content.IndexOf( startPattern );
                if( idxStart > 0 )
                {
                    idxStart += startPattern.Length;
                    int idxStop = content.IndexOf( stopPattern, idxStart );
                    if( idxStop > 0 )
                    {
                        return (idxStart, idxStop);
                    }
                }
                throw new InvalidOperationException( $"""
                        File '{jestConfigFilePath}' is invalid: unable to find '{startPattern}' and '{stopPattern}' markers. Content:
                        {content}
                        """ );
            }

            static void WriteReplacement( bool envFirst, string? serverAdress, IEnumerable<KeyValuePair<string, string>>? environmentVariables, FileStream f, StreamWriter text )
            {
                text.WriteLine();
                text.Write( "        " );
                if( envFirst )
                {
                    WriteEnv( environmentVariables, f, text );
                }
                else
                {
                    WriteSrv( serverAdress, text );
                }
                text.Write( "        " );

                static void WriteEnv( IEnumerable<KeyValuePair<string, string>>? environmentVariables, FileStream f, StreamWriter text )
                {
                    text.Write( "CKTypeScriptEnv: " );
                    if( environmentVariables == null )
                    {
                        text.WriteLine( "{}," );
                    }
                    else
                    {
                        text.Flush();
                        using( var w = new Utf8JsonWriter( f ) )
                        {
                            w.WriteStartObject();
                            bool hasTestKey = false;
                            foreach( var kv in environmentVariables )
                            {
                                hasTestKey |= kv.Key == TestRunningKey;
                                w.WriteString( kv.Key, kv.Value );
                            }
                            if( !hasTestKey )
                            {
                                w.WriteString( TestRunningKey, "true" );
                            }
                            w.WriteEndObject();
                            w.Flush();
                        }
                        text.WriteLine( "," );
                    }
                }

                static void WriteSrv( string? serverAdress, StreamWriter text )
                {
                    text.Write( "url: '" );
                    text.Write( serverAdress ?? "http://localhost" );
                    text.WriteLine( "'," );
                }
            }
        }

        /// <summary>
        /// Prepares the project to run Jest by updating the <see cref="JestConfigFileName"/> file with
        /// the provided <paramref name="ckTypeScriptEnv"/> and (at least) the "CK_TYPESCRIPT_ENGINE = true".
        /// </summary>
        /// <param name="monitor">Required monitor.</param>
        /// <param name="targetProjectPath">The project path.</param>
        /// <param name="afterRun">
        /// A cleanup action that must be run once the test is over.
        /// This is null if the target package.json file has no "test" script or if the "test" is
        /// not "jest".
        /// </param>
        /// <param name="serverAddress">Optional server address that will replace the default "http://localhost".</param>
        /// <param name="ckTypeScriptEnv">Optional CKTypeScriptEnv variables.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool PrepareJestRun( IActivityMonitor monitor,
                                           NormalizedPath targetProjectPath,
                                           out Action? afterRun,
                                           string? serverAddress = null,
                                           IReadOnlyDictionary<string, string>? ckTypeScriptEnv = null )
        {
            afterRun = null;
            var o = PackageJsonFile.ReadFile( monitor,
                                              targetProjectPath.AppendPart( "package.json" ),
                                              "Target project package.json",
                                              ignoreVersionsBound: true );
            if( o == null ) return false;
            if( o.Scripts.TryGetValue( "test", out var command ) && (command == "jest" || command.StartsWith( "jest " )) )
            {
                var jestConfigFilePath = targetProjectPath.AppendPart( JestConfigFileName );
                InjectConfigJS( jestConfigFilePath, serverAddress, ckTypeScriptEnv ?? ImmutableDictionary<string, string>.Empty );
                afterRun = () => InjectConfigJS( jestConfigFilePath, null, null );
            }
            return true;
        }
    }

}
