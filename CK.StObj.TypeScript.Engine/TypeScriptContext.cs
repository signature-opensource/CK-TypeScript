using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace CK.Setup
{
    /// <summary>
    /// Central class that handles TypeScript generation in a <see cref="TypeScriptRoot"/> (the <see cref="Root"/>)
    /// and <see cref="ICodeGenerationContext"/> (the <see cref="CodeContext"/>).
    /// <para>
    /// This is instantiated and made available to the participants (<see cref="ITSCodeGenerator"/> and <see cref="ITSCodeGeneratorType"/>)
    /// only if the configuration actually allows the TypeScript generation for this <see cref="CodeContext"/>.
    /// </para>
    /// </summary>
    public sealed class TypeScriptContext
    {
        readonly ICodeGenerationContext _codeContext;
        readonly TypeScriptAspectConfiguration _configuration;
        readonly TypeScriptAspectBinPathConfiguration _binPathConfiguration;
        readonly TSContextInitializer _initializer;
        readonly TypeScriptRoot _tsRoot;
        readonly PocoCodeGenerator _pocoGenerator;

        internal TypeScriptContext( ICodeGenerationContext codeCtx,
                                    TypeScriptAspectConfiguration tsConfig,
                                    TypeScriptAspectBinPathConfiguration tsBinPathConfig,
                                    TSContextInitializer initializer,
                                    IPocoTypeNameMap? jsonExchangeableNames )
        {
            _codeContext = codeCtx;
            _configuration = tsConfig;
            _binPathConfiguration = tsBinPathConfig;
            _initializer = initializer;
            _tsRoot = new TypeScriptRoot( tsConfig.LibraryVersions, tsConfig.PascalCase, tsConfig.GenerateDocumentation );
            _tsRoot.FolderCreated += OnFolderCreated;
            _tsRoot.TSTypes.TSFromTypeRequired += OnTSFromTypeRequired;
            _tsRoot.TSTypes.TSFromObjectRequired += OnTSFromObjectRequired;
            Root.Root.EnsureBarrel();
            _pocoGenerator = new PocoCodeGenerator( this, initializer.TypeScriptExchangeableSet, jsonExchangeableNames );
        }

        void OnFolderCreated( TypeScriptFolder f )
        {
            if( _binPathConfiguration.Barrels.Contains( f.Path ) )
            {
                f.EnsureBarrel();
            }
        }

        /// <summary>
        /// When an object must be resolved, we simply dispatch the event to all the global ITSCodeGenerator
        /// available. It's up to them to handle it if they recognize the object.
        /// </summary>
        /// <param name="sender">The TSTypeManager.</param>
        /// <param name="e">The event with the key and the final <see cref="RequireTSFromObjectEventArgs.ResolvedType"/> to be set.</param>
        void OnTSFromObjectRequired( object? sender, RequireTSFromObjectEventArgs e )
        {
            var success = true;
            foreach( var g in _initializer.GlobalCodeGenerators )
            {
                success &= g.OnResolveObjectKey( e.Monitor, this, e );
            }
            if( success )
            {
                _pocoGenerator.OnResolveObjectKey( e.Monitor, e );
            }
        }

        /// <summary>
        /// To resolve a C# type, we first see if a configuration must be applied to it. If it's the case, the
        /// configuration is applied to the event (FolderType, TypeName, etc.).
        /// Then we call all the global ITSCodeGenerator with the event and then all the ITSCodeGeneratorType associated
        /// to the type.
        /// </summary>
        /// <param name="sender">The TSTypeManager.</param>
        /// <param name="e">The event that acts as a TSType builder.</param>
        void OnTSFromTypeRequired( object? sender, RequireTSFromTypeEventArgs e )
        {
            bool success = true;
            _initializer.RegisteredTypes.TryGetValue( e.Type, out RegisteredType regType );
            var a = regType.Attribute;
            if( a != null )
            {
                success &= e.TryInitialize( e.Monitor, a.Folder, a.FileName, a.TypeName, a.SameFolderAs, a.SameFileAs );
            }
            // Applies type generators.
            var typeGenerators = regType.Generators;
            if( typeGenerators != null )
            {
                foreach( var g in typeGenerators )
                {
                    success &= g.ConfigureBuilder( e.Monitor, this, e );
                }
            }
            // Applies global generators.
            foreach( var g in _initializer.GlobalCodeGenerators )
            {
                success &= g.OnResolveType( e.Monitor, this, e );
            }
            if( success )
            {
                _pocoGenerator.OnResolveType( e.Monitor, e );
            }
            // Consider any initialization error as an error that condems the type (and will eventually
            // condemn the whole process).
            if( !success ) e.SetError();
        }

        /// <summary>
        /// Gets the <see cref="TypeScriptRoot"/>.
        /// </summary>
        public TypeScriptRoot Root => _tsRoot;

        /// <summary>
        /// Gets the <see cref="ICodeGenerationContext"/> that is being processed.
        /// </summary>
        public ICodeGenerationContext CodeContext => _codeContext;

        /// <summary>
        /// Gets the <see cref="ITSPocoCodeGenerator "/>.
        /// </summary>
        public ITSPocoCodeGenerator PocoCodeGenerator => _pocoGenerator;

        /// <summary>
        /// Gets the <see cref="TypeScriptAspectConfiguration"/>.
        /// </summary>
        public TypeScriptAspectConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the <see cref="TypeScriptAspectBinPathConfiguration"/>.
        /// </summary>
        public TypeScriptAspectBinPathConfiguration BinPathConfiguration => _binPathConfiguration;

        /// <summary>
        /// Gets all the global generators.
        /// </summary>
        public IReadOnlyList<ITSCodeGenerator> GlobalGenerators => _initializer.GlobalCodeGenerators;

        internal bool Run( IActivityMonitor monitor )
        {
            _tsRoot.TSTypes.RegisterStandardTypes( monitor );
            using( monitor.OpenInfo( $"Running TypeScript code generation for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                return  // Initializes the global generators.
                        TSContextInitializer.CallGlobalCodeGenerators( monitor, _initializer.GlobalCodeGenerators, null, this )
                        // Calls Root.TSTypes.ResolveType for each RegisteredType:
                        // - When the RegisteredType is a PocoType, TSTypeManager.ResolveTSType is called with the IPocoType (object resolution).
                        // - When the RegisteredType is only a C# type, TSTypeManager.ResolveTSType is called with the type (C# type resolution). 
                        && ResolveRegisteredTypes( monitor )
                        // Calls the TypeScriptRoot to generate the code for all ITSFileCSharpType (run the deferred Implementors).
                        && _tsRoot.GenerateCode( monitor );
            }
        }

        bool ResolveRegisteredTypes( IActivityMonitor monitor )
        {
            bool success = true;
            Type? t = null;
            IPocoType? pT = null;
            try
            {
                using( monitor.OpenInfo( $"Declaring {_initializer.RegisteredTypes.Count} registered types." ) )
                {
                    foreach( var (type, reg) in _initializer.RegisteredTypes )
                    {
                        if( reg.PocoType != null )
                        {
                            pT = reg.PocoType;
                            _tsRoot.TSTypes.ResolveTSType( monitor, pT );
                        }
                        else
                        {
                            pT = null;
                            t = type;
                            _tsRoot.TSTypes.ResolveTSType( monitor, type );
                        }
                    }
                }
                using( monitor.OpenInfo( $"Ensuring that all the Poco of the TypeScriptSet are registered." ) )
                {
                    foreach( var p in _pocoGenerator.TypeScriptSet.NonNullableTypes )
                    {
                        pT = p;
                        _tsRoot.TSTypes.ResolveTSType( monitor, pT );
                    }
                }
            }
            catch( Exception ex )
            {
                success = false;
                if( pT != null )
                {
                    monitor.Error( $"Unable to resolve Poco type '{pT}'.", ex );
                }
                else
                {
                    monitor.Error( $"Unable to resolve type '{t:C}'.", ex );
                }
            }
            return success;
        }

        internal bool Save( IActivityMonitor monitor )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Saving generated TypeScript for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                var ckGenFolder = BinPathConfiguration.TargetProjectPath.AppendPart( "ck-gen" );
                var ckGenFolderSrc = ckGenFolder.AppendPart( "src" );
                // To minimize impacts on file watchers, we don't destroy/recreate the ck-gen folder.
                // Instead we update the existing files in place and then remove any paths that have not
                // been generated.
                HashSet<string>? cleanupPaths = null;
                if( Directory.Exists( ckGenFolderSrc ) )
                {
                    var previous = Directory.EnumerateFiles( ckGenFolderSrc, "*", SearchOption.AllDirectories );
                    if( Path.DirectorySeparatorChar != NormalizedPath.DirectorySeparatorChar )
                    {
                        previous = previous.Select( p => p.Replace( Path.DirectorySeparatorChar, NormalizedPath.DirectorySeparatorChar ) );
                    }
                    cleanupPaths = new HashSet<string>( previous );
                    monitor.Trace( $"Found {cleanupPaths.Count} existing files in '{ckGenFolderSrc}'." );
                }
                int? savedCount = Root.Save( monitor, ckGenFolderSrc, cleanupPaths );
                if( savedCount.HasValue )
                {
                    if( savedCount.Value == 0 )
                    {
                        monitor.Warn( $"No files or folders have been generated in '{ckGenFolder}'. Skipping TypeScript generation." );
                    }
                    else
                    {
                        if( BinPathConfiguration.GitIgnoreCKGenFolder )
                        {
                            File.WriteAllText( Path.Combine( ckGenFolder, ".gitignore" ), "*" );
                        }
                        // Preload the target package.json if it exists to extract the typescriptVersion if it exists.
                        // Even if the target package is on error or has no typescript installed, we CAN build the generated
                        // typescript by installing the BinPathConfiguration.AutomaticTypeScriptVersion typescript package.
                        var targetProjectPath = BinPathConfiguration.TargetProjectPath;
                        var projectJsonPath = targetProjectPath.AppendPart( "package.json" );
                        var targetPackageJson = YarnHelper.LoadPackageJson( monitor, projectJsonPath, out bool invalidPackageJson );

                        // Even if we don't know yet whether Yarn is installed, we lookup the Yarn typescript sdk version.
                        // Before compiling the ck-gen folder, we must ensure that the Yarn typescript sdk is installed: if it's not, package resolution fails miserably.
                        // We read the typeScriptSdkVersion here.
                        // We also return whether the target project has TypeScript installed: if yes, there's no need to "yarn add" it to
                        // the ck-gen project, "yarn install" is enough.
                        var targetTypeScriptVersion = FindBestTypeScriptVersion( monitor, targetProjectPath, targetPackageJson,
                                                                                 out var typeScriptSdkVersion,
                                                                                 out var targetProjectHasTypeScript );

                        // Generates "/ck-gen" files "package.json", "tsconfig.json" and "tsconfig-cjs.json".
                        // This may fail if there's an error in the dependencies declared by the code
                        // generator (in LibraryImport).
                        if( YarnHelper.SaveCKGenBuildConfig( monitor, ckGenFolder, targetTypeScriptVersion, this ) )
                        {
                            if( BinPathConfiguration.SkipTypeScriptTooling )
                            {
                                monitor.Info( "Skipping any TypeScript project setup since SkipTypeScriptTooling is true." );
                            }
                            else
                            {
                                var yarnPath = YarnHelper.GetYarnInstallPath( monitor,
                                                                              targetProjectPath,
                                                                              BinPathConfiguration.AutoInstallYarn );
                                if( yarnPath.HasValue )
                                {
                                    // We have a yarn, we can build "@local/ck-gen".
                                    using( monitor.OpenInfo( $"Building '@local/ck-gen' package..." ) )
                                    {
                                        // Ensuring that TypeScript is installed.
                                        if( !targetProjectHasTypeScript )
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, $"add --dev typescript@{targetTypeScriptVersion}", yarnPath.Value );
                                        }
                                        else
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "install", yarnPath.Value );
                                        }
                                        if( typeScriptSdkVersion == null )
                                        {
                                            monitor.Info( $"Yarn TypeScript sdk is not installed. Installing it with{(BinPathConfiguration.AutoInstallVSCodeSupport ? "" : "out")} VSCode support." );
                                            success &= YarnHelper.InstallYarnSdkSupport( monitor, targetProjectPath, BinPathConfiguration.AutoInstallVSCodeSupport, yarnPath.Value );
                                            if( success )
                                            {
                                                typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, targetProjectPath );
                                                if( typeScriptSdkVersion == null )
                                                {
                                                    monitor.Error( $"Unable to read back the TypeScript version used by the Yarn sdk." );
                                                    success = false;
                                                }
                                                else
                                                {
                                                    CheckTypeScriptSdkVersion( monitor, typeScriptSdkVersion, targetTypeScriptVersion );
                                                }
                                            }
                                        }
                                        // Only try a compilation if no error occurred so far.
                                        if( success )
                                        {
                                            success = YarnHelper.DoRunYarn( monitor, ckGenFolder, "run build", yarnPath.Value );
                                        }
                                        monitor.CloseGroup( success ? "Success." : "Failed." );
                                    }

                                    // If the lookup made previously to the target package.json is not on error, we handle EnsureTestSupport.
                                    // We do this even on compilation failure (if asked to do so) to be able to work in the
                                    // target project.

                                    // We always ensure that the workspaces:["ck-gen"] and "@local/ck-gen" dependency are here.
                                    // We may read again the typeScriptVersion here but we don't care and it unifies the behavior:
                                    // EnsureJestTestSupport "yarn add" only the packages that are not already here.
                                    if( !invalidPackageJson
                                        && YarnHelper.SetupTargetProjectPackageJson( monitor,
                                                                                     projectJsonPath,
                                                                                     targetPackageJson,
                                                                                     out var testScriptCommand,
                                                                                     out var typeScriptVersion,
                                                                                     out var jestVersion,
                                                                                     out var tsJestVersion,
                                                                                     out var typesJestVersion )
                                        && BinPathConfiguration.EnsureTestSupport )
                                    {
                                        // If we must ensure test support, we consider that as soon as a "test" script is available
                                        // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                                        // not required.
                                        if( testScriptCommand != null && typeScriptVersion != null )
                                        {
                                            monitor.Info( $"TypeScript test script command '{testScriptCommand}' (and 'typescript@{typeScriptVersion}') already exists. " +
                                                          "Skipping EnsureJestTestSupport." );
                                        }
                                        else
                                        {
                                            // Note: Only the "typescript" package has a version here. We may 
                                            success &= EnsureJestTestSupport( monitor,
                                                                              targetProjectPath,
                                                                              projectJsonPath,
                                                                              testScriptCommand == null,
                                                                              typeScriptVersion, targetTypeScriptVersion,
                                                                              yarnPath.Value,
                                                                              jestVersion,
                                                                              tsJestVersion,
                                                                              typesJestVersion );
                                        }
                                    }
                                }
                            }
                        }
                        else success = false;
                    }
                    if( cleanupPaths != null )
                    {
                        if( cleanupPaths.Count == 0 )
                        {
                            monitor.Info( "No previous file exist that have not been regenerated." );
                        }
                        else
                        {
                            using( monitor.OpenInfo( $"Deleting {cleanupPaths.Count} previous files." ) )
                            {
                                foreach( var p in cleanupPaths )
                                {
                                    monitor.Debug( $"Deleting '{p.AsSpan( ckGenFolder.Path.Length )}'." );
                                    try
                                    {
                                        if( File.Exists( p ) ) File.Delete( p );
                                    }
                                    catch( Exception ex )
                                    {
                                        monitor.Error( $"While deleting '{p}'. Ignoring.", ex );
                                    }
                                }
                            }
                        }
                    }
                }
                else success = false;
            }
            return success;
        }

        static void CheckTypeScriptSdkVersion( IActivityMonitor monitor, string typeScriptSdkVersion, string targetTypeScriptVersion )
        {
            if( typeScriptSdkVersion != targetTypeScriptVersion )
            {
                monitor.Warn( $"The TypeScript version used by the Yarn sdk '{typeScriptSdkVersion}' differs from the selected one '{targetTypeScriptVersion}'.{Environment.NewLine}" +
                              $"This can lead to annoying issues such as import resolution failures." );
            }
        }

        string FindBestTypeScriptVersion( IActivityMonitor monitor,
                                          NormalizedPath targetProjectPath,
                                          JsonObject? targetPackageJson,
                                          out string? typeScriptSdkVersion,
                                          out bool targetProjectHasTypeScript )
        {
            targetProjectHasTypeScript = true;
            var typeScriptVersionSource = "target project";
            var targetTypeScriptVersion = targetPackageJson?["devDependencies"]?["typescript"]?.ToString();
            typeScriptSdkVersion = YarnHelper.GetYarnSdkTypeScriptVersion( monitor, targetProjectPath );
            if( targetTypeScriptVersion == null )
            {
                targetProjectHasTypeScript = false;
                if( typeScriptSdkVersion == null )
                {
                    typeScriptVersionSource = "Configuration.AutomaticTypeScriptVersion";
                    targetTypeScriptVersion = BinPathConfiguration.AutomaticTypeScriptVersion;
                }
                else
                {
                    typeScriptVersionSource = "Yarn TypeScript sdk";
                    targetTypeScriptVersion = typeScriptSdkVersion;
                }
            }
            monitor.Info( $"Considering TypeScript version '{targetTypeScriptVersion}' from {typeScriptVersionSource}." );
            if( typeScriptSdkVersion != null )
            {
                CheckTypeScriptSdkVersion( monitor, typeScriptSdkVersion, targetTypeScriptVersion );
            }
            return targetTypeScriptVersion;
        }

        static bool EnsureJestTestSupport( IActivityMonitor monitor,
                                           NormalizedPath targetProjectPath,
                                           NormalizedPath projectJsonPath,
                                           bool addTestJestScript,
                                           string? typeScriptVersion,
                                           string targetTypescriptVersion,
                                           NormalizedPath yarnPath,
                                           string? jestVersion,
                                           string? tsJestVersion,
                                           string? typesJestVersion )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
            {
                string a = string.Empty, i = string.Empty;
                Add( ref a, ref i, "typescript", typeScriptVersion, targetTypescriptVersion );
                Add( ref a, ref i, "jest", jestVersion, null );
                Add( ref a, ref i, "ts-jest", tsJestVersion, null );
                Add( ref a, ref i, "@types/jest", typesJestVersion, null );

                static void Add( ref string a, ref string i, string name, string? currentVersion, string? version )
                {
                    if( currentVersion == null )
                    {
                        if( version != null ) name = $"{name}@{version}";
                        if( i.Length == 0 ) i = name;
                        else i += ' ' + name;
                    }
                    else
                    {
                        a = $"{a}{(a.Length == 0 ? "" : ", ")}{name}@{currentVersion}";
                    }
                }

                if( a.Length > 0 ) monitor.Info( $"Already installed: {a}." );
                if( i.Length > 0 )
                {
                    success &= YarnHelper.DoRunYarn( monitor, targetProjectPath, $"add --dev {i}", yarnPath );
                }
                // Always setup jest even on error.
                YarnHelper.EnsureSampleJestTestInSrcFolder( monitor, targetProjectPath );
                YarnHelper.SetupJestConfigFile( monitor, targetProjectPath );

                var packageJsonObject = YarnHelper.LoadPackageJson( monitor, projectJsonPath, out var invalidPackageJson );
                // There is absolutely no reason here to not be able to read back the package.json.
                // Defensive programming here.
                if( !invalidPackageJson && packageJsonObject != null )
                {
                    bool modified = false;
                    var scripts = YarnHelper.EnsureJsonObject( monitor, packageJsonObject, "scripts", ref modified );
                    Throw.DebugAssert( scripts != null );
                    scripts.Add( "test", "jest" );
                    success &= YarnHelper.SavePackageJsonFile( monitor, projectJsonPath, packageJsonObject );
                }
            }

            return success;
        }
    }
}
