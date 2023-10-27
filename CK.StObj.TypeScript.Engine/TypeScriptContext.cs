using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.Setup.Json;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

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
    public sealed class TypeScriptContext : TypeScriptRoot
    {
        readonly Dictionary<Type, TypeScriptAttribute> _fromConfiguration;
        readonly Dictionary<Type, TSTypeFile?> _typeMappings;
        readonly IReadOnlyDictionary<Type, ITypeAttributesCache> _attributeCache;
        readonly List<TSTypeFile> _typeFiles;
        // Required to store the pure type generator associated to a type when it is
        // not decorated with a TypeScriptAttribute and not declared in configuration.
        Dictionary<Type, ITSCodeGeneratorType[]>? _undeclaredGenerators;
        IReadOnlyList<ITSCodeGenerator> _globals;
        bool _success;

        internal TypeScriptContext( ICodeGenerationContext codeCtx,
                                    TypeScriptAspectConfiguration tsConfig,
                                    TypeScriptAspectBinPathConfiguration tsBinPathConfig,
                                    JsonSerializationCodeGen? jsonGenerator )
               : base( tsConfig.PascalCase, tsConfig.GenerateDocumentation )
        {
            CodeContext = codeCtx;
            Configuration = tsConfig;
            BinPathConfiguration = tsBinPathConfig;
            JsonGenerator = jsonGenerator;
            _fromConfiguration = new Dictionary<Type, TypeScriptAttribute>();
            _typeMappings = new Dictionary<Type, TSTypeFile?>();
            _attributeCache = codeCtx.CurrentRun.EngineMap.AllTypesAttributesCache;
            _typeFiles = new List<TSTypeFile>();
            _success = true;
            Root.EnsureBarrel();
        }

        protected override void OnFolderCreated( TypeScriptFolder f )
        {
            if( BinPathConfiguration.Barrels.Contains( f.Path ) )
            {
                f.EnsureBarrel();
            }
        }

        /// <summary>
        /// Gets the typed folder root.
        /// </summary>
        public new TypeScriptFolder<TypeScriptContext> Root => (TypeScriptFolder<TypeScriptContext>)base.Root;

        /// <summary>
        /// Gets the <see cref="ICodeGenerationContext"/> that is being processed.
        /// </summary>
        public ICodeGenerationContext CodeContext { get; }

        /// <summary>
        /// Gets the <see cref="TypeScriptAspectConfiguration"/>.
        /// </summary>
        public TypeScriptAspectConfiguration Configuration { get; }

        /// <summary>
        /// Gets the <see cref="TypeScriptAspectBinPathConfiguration"/>.
        /// </summary>
        public TypeScriptAspectBinPathConfiguration BinPathConfiguration { get; }

        /// <summary>
        /// Gets the <see cref="JsonSerializationCodeGen"/> it is available.
        /// </summary>
        public JsonSerializationCodeGen? JsonGenerator { get; }

        /// <summary>
        /// Gets all the global generators.
        /// </summary>
        public IReadOnlyList<ITSCodeGenerator> GlobalGenerators => _globals;

        /// <summary>
        /// Gets the Poco code generator (the first <see cref="GlobalGenerators"/>).
        /// </summary>
        public PocoCodeGenerator PocoCodeGenerator => (PocoCodeGenerator)_globals[0];

        /// <summary>
        /// Gets a <see cref="TSTypeFile"/> for a type if it has been declared so far and is not
        /// an intrinsic types (see remarks of <see cref="DeclareTSType(IActivityMonitor, Type, bool)"/>).
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The Type to File association if is has been declared.</returns>
        public TSTypeFile? FindDeclaredTSType( Type t ) => _typeMappings.GetValueOrDefault( t );

        /// <summary>
        /// Declares the required support of a type.
        /// This may create one <see cref="TSTypeFile"/> for this type (if it is not an intrinsic type, see remarks)
        /// and others for generic parameters.
        /// All declared types that requires a file must eventually be generated.
        /// </summary>
        /// <remarks>
        /// "Intrinsic types" can be written directly in TypeScript and don't require a dedicated file. See <see cref="IntrinsicTypeName(Type)"/>.
        /// </remarks>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="t">The type that should be available in TypeScript.</param>
        /// <param name="requiresFile">True to emit an error log if the returned file is null.</param>
        /// <returns>The mapped file or null if <paramref name="t"/> doesn't need a file or cannot be mapped.</returns>
        public TSTypeFile? DeclareTSType( IActivityMonitor monitor, Type t, bool requiresFile = false )
        {
            Throw.CheckNotNullArgument( t );
            HashSet<Type>? _ = null;
            var f = DeclareTSType( monitor, t, ref _ );
            if( f == null && requiresFile )
            {
                monitor.Error( $"Unable to obtain a TypeScript file mapping for type '{t}'." );
            }
            return f;
        }

        /// <summary>
        /// Tries to get the TypeScript type name for basic types. This follows the ECMAScriptStandard
        /// mapping rules (short numerics up to <see cref="UInt32"/> and double are "number",
        /// long, ulong, decimal and <see cref="System.Numerics.BigInteger"/> are "bigInteger".
        /// Object is mapped to "unknown" and void, boolean and string are "void", "boolean" and "string".
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The TypeScript name if it's a basic type, null if it requires a definition file.</returns>
        public static string? IntrinsicTypeName( Type t )
        {
            Throw.CheckNotNullArgument( t );
            if( t == typeof( void ) ) return "void";
            else if( t == typeof( bool ) ) return "boolean";
            else if( t == typeof( string ) ) return "string";
            else if( t == typeof( int )
                     || t == typeof( uint )
                     || t == typeof( short )
                     || t == typeof( ushort )
                     || t == typeof( byte )
                     || t == typeof( sbyte )
                     || t == typeof( float )
                     || t == typeof( double ) ) return "number";
            else if( t == typeof( long )
                     || t == typeof( ulong )
                     || t == typeof( decimal )
                     || t == typeof( System.Numerics.BigInteger ) ) return "BigInteger";
            else if( t == typeof( object ) ) return "unknown";
            return null;
        }

        /// <summary>
        /// Declares the required support of any number of types.
        /// This may create any number of <see cref="TSTypeFile"/> that must eventually be generated.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="types">The types that should be available in TypeScript.</param>
        public void DeclareTSType( IActivityMonitor monitor, IEnumerable<Type> types )
        {
            foreach( var t in types )
                if( t != null )
                    DeclareTSType( monitor, t );
        }

        /// <summary>
        /// Declares the required support of any number of types.
        /// This may create any number of <see cref="TSTypeFile"/> that must eventually be generated.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="types">The types that should be available in TypeScript.</param>
        public void DeclareTSType( IActivityMonitor monitor, params Type[] types ) => DeclareTSType( monitor, (IEnumerable<Type>)types );

        TSTypeFile? DeclareTSType( IActivityMonitor monitor, Type t, ref HashSet<Type>? cycleDetector )
        {
            TSTypeFile Create( Type t )
            {
                var attrFromConfig = _fromConfiguration.GetValueOrDefault( t );
                var attribs = t.GetCustomAttributes( typeof( TypeScriptAttribute ), false );
                var attr = attribs.Length == 1
                            ? ((TypeScriptAttribute)attribs[0]).ApplyOverride( attrFromConfig )
                            : attrFromConfig;
                var generators = _undeclaredGenerators?.GetValueOrDefault( t ) ?? Array.Empty<ITSCodeGeneratorType>();
                var f = new TSTypeFile( this, t, generators, attr );
                _typeMappings.Add( t, f );
                _typeFiles.Add( f );
                return f;
            }

            if( !_typeMappings.TryGetValue( t, out var f ) )
            {
                if( t.IsArray )
                {
                    DeclareTSType( monitor, t.GetElementType()! );
                }
                else if( t.IsValueTuple() )
                {
                    foreach( var s in t.GetGenericArguments() )
                    {
                        DeclareTSType( monitor, s );
                    }
                }
                else if( t.IsGenericType )
                {
                    Type tDef;
                    if( t.IsGenericTypeDefinition )
                    {
                        tDef = t;
                    }
                    else
                    {
                        foreach( var a in t.GetGenericArguments() )
                        {
                            DeclareTSType( monitor, a );
                        }
                        tDef = t.GetGenericTypeDefinition();
                    }
                    if( tDef != typeof( IDictionary<,> )
                        && tDef != typeof( Dictionary<,> )
                        && tDef != typeof( ISet<> )
                        && tDef != typeof( HashSet<> )
                        && tDef != typeof( IList<> )
                        && tDef != typeof( List<> ) )
                    {
                        f = Create( tDef );
                    }
                }
                else if( IntrinsicTypeName( t ) == null )
                {
                    f = Create( t );
                }
            }

            // We always check the initialization to handle TSTypeFile created by
            // the attributes.
            if( f != null && !f.IsInitialized )
            {
                EnsureInitialized( monitor, f, ref cycleDetector );
            }
            return f;
        }

        TSTypeFile EnsureInitialized( IActivityMonitor monitor, TSTypeFile f, ref HashSet<Type>? cycleDetector )
        {
            Debug.Assert( !f.IsInitialized );
            TypeScriptAttribute attr = f.Attribute;
            var generators = f.Generators;
            var t = f.Type;

            foreach( var g in _globals )
            {
                _success &= g.ConfigureTypeScriptAttribute( monitor, f, attr );
            }
            if( generators.Count > 0 )
            {
                foreach( var g in generators )
                {
                    _success &= g.ConfigureTypeScriptAttribute( monitor, f, attr );
                }
            }

            NormalizedPath folder;
            string? fileName = null;
            Type? refTarget = attr.SameFileAs ?? attr.SameFolderAs;
            if( refTarget != null )
            {
                if( cycleDetector == null ) cycleDetector = new HashSet<Type>();
                if( !cycleDetector.Add( t ) ) Throw.InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {cycleDetector.Select( c => c.Name ).Concatenate( " => " )}." );

                var target = DeclareTSType( monitor, refTarget, ref cycleDetector );
                if( target == null )
                {
                    monitor.Error( $"Type '{refTarget}' cannot be used in SameFileAs or SameFolderAs attributes." );
                    _success = false;
                    folder = default;
                }
                else
                {
                    folder = target.Folder;
                    if( attr.SameFileAs != null )
                    {
                        fileName = target.FileName;
                    }
                }
            }
            else
            {
                folder = attr.Folder ?? t.Namespace!.Replace( '.', '/' );
            }
            string typeName = attr.TypeName ?? SafeNameChars( t.GetExternalName() ?? GetSafeName( t ) );
            fileName ??= attr.FileName ?? (SafeFileChars( typeName ) + ".ts");
            f.Initialize( folder, fileName, typeName );
            monitor.Trace( f.ToString() );
            return f;
        }

        internal static string GetPocoClassNameFromPrimaryInterface( IPocoRootInfo itf )
        {
            var typeName = itf.PrimaryInterface.GetExternalName();
            if( typeName == null )
            {
                typeName = TypeScriptContext.GetSafeName( itf.PrimaryInterface );
                if( typeName.StartsWith( "I" ) ) typeName = typeName.Remove( 0, 1 );
            }
            typeName = TypeScriptContext.SafeNameChars( typeName );
            return typeName;
        }

        internal static string SafeNameChars( string name )
        {
            return name.Replace( '+', '_' );
        }
        internal static string SafeFileChars( string name )
        {
            return name.Replace( '<', '{' ).Replace( '>', '}' );
        }
        internal static string GetSafeName( Type t )
        {
            var n = t.Name;
            if( !t.IsGenericType )
            {
                return n;
            }
            Type tDef = t.IsGenericTypeDefinition ? t : t.GetGenericTypeDefinition();
            n += '<' + tDef.GetGenericArguments().Select( a => a.Name ).Concatenate() + '>';
            return n;
        }

        internal bool Run( IActivityMonitor monitor )
        {
            IPocoSupportResult pocoTypeSystem = CodeContext.CurrentRun.ServiceContainer.GetService<IPocoSupportResult>( true );
            using( monitor.OpenInfo( $"Running TypeScript code generation for:{Environment.NewLine}{BinPathConfiguration.ToXml()}" ) )
            {
                return BuildAttributesFromConfiguration( monitor, pocoTypeSystem )
                        && BuildTSTypeFilesFromAttributesAndDiscoverGenerators( monitor, pocoTypeSystem )
                        && DeclarePurelyConfiguredTypes( monitor )
                        && CallCodeGenerators( monitor, initialize: true )
                        && CallCodeGenerators( monitor, false )
                        && EnsureTypesGeneration( monitor );
            }
        }


        bool BuildAttributesFromConfiguration( IActivityMonitor monitor, IPocoSupportResult pocoTypeSystem )
        {
            using( monitor.OpenInfo( $"Building TypeScriptAttribute for {BinPathConfiguration.Types.Count} Type configurations." ) )
            {
                bool success = true;
                foreach( var c in BinPathConfiguration.Types )
                {
                    Type? t = ResolveType( pocoTypeSystem, c.Type );
                    if( t == null )
                    {
                        monitor.Error( $"Unable to resolve type '{c.Type}' in TypeScriptAspectConfiguration in:{Environment.NewLine}{c.ToXml()}" );
                        success = false;
                    }
                    else
                    {
                        var attr = c.ToAttribute( monitor, ( monitor, typeName ) => ResolveType( pocoTypeSystem, typeName ) );
                        if( attr == null ) success = false;
                        else
                        {
                            _fromConfiguration.Add( t, attr );
                        }
                    }
                }
                monitor.CloseGroup( $"{_fromConfiguration.Count} configurations processed." );
                return success;
            }

            static Type? ResolveType( IPocoSupportResult pocoTypeSystem, string typeName )
            {
                var t = SimpleTypeFinder.WeakResolver( typeName, false );
                if( t == null )
                {
                    if( pocoTypeSystem.NamedRoots.TryGetValue( typeName, out var rootInfo ) )
                    {
                        t = rootInfo.PrimaryInterface;
                    }
                }
                return t;
            }

        }

        bool BuildTSTypeFilesFromAttributesAndDiscoverGenerators( IActivityMonitor monitor, IPocoSupportResult pocoTypeSystem )
        {
            var globals = new List<ITSCodeGenerator>
            {
                new PocoCodeGenerator( pocoTypeSystem ),
                new SystemTypesCodeGenerator(),
                new GlobalizationTypesCodeGenerator()
            };

            using( monitor.OpenInfo( "Analyzing types with TypeScript and/or ITSCodeGeneratorType or ITSCodeGenerator attributes." ) )
            {
                // These variables are reused per type.
                TypeScriptAttributeImpl? impl;
                List<ITSCodeGeneratorType> generators = new List<ITSCodeGeneratorType>();

                foreach( ITypeAttributesCache attributeCache in _attributeCache.Values )
                {
                    impl = null;
                    generators.Clear();

                    foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                    {
                        if( m is ITSCodeGenerator g )
                        {
                            globals.Add( g );
                        }
                        if( m is TypeScriptAttributeImpl a )
                        {
                            if( impl != null )
                            {
                                monitor.Error( $"Multiple TypeScriptImpl decorates '{attributeCache.Type}'." );
                                _success = false;
                            }
                            impl = a;
                        }
                        if( m is ITSCodeGeneratorType tG )
                        {
                            generators.Add( tG );
                        }
                    }
                    // If the attribute is only a ITSCodeGeneratorType, we don't consider it as an empty TypeScriptArribute,
                    // we register it if and only if it appears in the configuration.
                    // And if it is not declared in the configuration we must keep the array of its generators to be able to
                    // use them if the type is referenced by another referenced type.
                    if( impl != null || generators.Count > 0 )
                    {
                        // Lookups the potential configuration to override the code defined values
                        // and removes it from the map: after this code attribute process, the remaining
                        // purely configured types must be declared.
                        _fromConfiguration.Remove( attributeCache.Type, out var attrFromConfig );
                        TSTypeFile? f = null;
                        if( impl != null )
                        {
                            f = new TSTypeFile( this, attributeCache.Type, generators.ToArray(), impl.Attribute.ApplyOverride( attrFromConfig ) );
                        }
                        else if( attrFromConfig != null )
                        {
                            f = new TSTypeFile( this, attributeCache.Type, generators.ToArray(), attrFromConfig );
                        }
                        if( f != null )
                        {
                            _typeMappings.Add( attributeCache.Type, f );
                            _typeFiles.Add( f );
                        }
                        else
                        {
                            _undeclaredGenerators ??= new Dictionary<Type, ITSCodeGeneratorType[]>();
                            _undeclaredGenerators.Add( attributeCache.Type, generators.ToArray() );
                        }
                    }
                }
                if( _success ) monitor.CloseGroup( $"Found {globals.Count} global generators and {_typeMappings.Count} types to generate." );
                _globals = globals;
                return _success;
            }
        }

        bool DeclarePurelyConfiguredTypes( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( $"Declaring {_fromConfiguration.Count} purely declared types." ) )
            {
                foreach( var (type, attr) in _fromConfiguration )
                {
                    var f = new TSTypeFile( this, type, Array.Empty<ITSCodeGeneratorType>(), attr );
                    _typeMappings.Add( type, f );
                    _typeFiles.Add( f );
                }
                return true;
            }
        }

        bool CallCodeGenerators( IActivityMonitor monitor, bool initialize )
        {
            string action = initialize ? "Initializing" : "Executing";
            Debug.Assert( _success );
            // Executes all the globals.
            using( monitor.OpenInfo( $"{action} the {_globals.Count} global {nameof(ITSCodeGenerator)} TypeScript generators." ) )
            {
                foreach( var global in _globals )
                {
                    using( monitor.OpenTrace( $"{action} '{global.GetType().FullName}' global TypeScript generator." ) )
                    {
                        try
                        {
                            _success = initialize
                                        ? global.Initialize( monitor, this )
                                        : global.GenerateCode( monitor, this );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( ex );
                            _success = false;
                        }
                        if( !_success )
                        {
                            monitor.CloseGroup( "Failed." );
                            return false;
                        }
                    }
                }
            }
            return _success;
        }

        bool EnsureTypesGeneration( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( $"Ensuring that {_typeFiles.Count} types are initialized and implemented." ) )
            {
                for( int i = 0; i < _typeFiles.Count; ++i )
                {
                    var f = _typeFiles[i];
                    if( !f.IsInitialized )
                    {
                        HashSet<Type>? _ = null;
                        EnsureInitialized( monitor, f, ref _ );
                    }
                    _success &= f.Implement( monitor );
                }
            }
            return _success;
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
                        // typescript by using the latest typescript.
                        var targetProjectPath = BinPathConfiguration.TargetProjectPath;
                        var projectJsonPath = targetProjectPath.AppendPart( "package.json" );
                        var targetPackageJson = YarnHelper.LoadPackageJson( monitor, projectJsonPath, out bool invalidPackageJson );
                        var targetTypescriptVersion = targetPackageJson?["devDependencies"]?["typescript"]?.ToString();
                        if( targetTypescriptVersion != null )
                        {
                            monitor.Info( $"Found typescript in version '{targetTypescriptVersion}' in target '{projectJsonPath}'." );
                        }
                        else 
                        {
                            if( !invalidPackageJson )
                            {
                                if( targetPackageJson == null )
                                {
                                    monitor.Info( "No target package.json found, we'll install typescript current version in '@local/ck-gen'." );
                                }
                                else
                                {
                                    monitor.Warn( $"Typescript is not installed in target '{projectJsonPath}', we'll install typescript current version in '@local/ck-gen'." );
                                }
                            }
                        }
                        // Generates "/ck-gen" files "package.json", "tsconfig.json" and "tsconfig-cjs.json".
                        // This may fail if there's an error in the dependencies declared by the code
                        // generator (in LibraryImport).
                        if( YarnHelper.SaveCKGenBuildConfig( monitor, ckGenFolder, targetTypescriptVersion, this ) )
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
                                        if( targetTypescriptVersion == null )
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "add --dev typescript", yarnPath.Value );
                                        }
                                        else
                                        {
                                            success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "install", yarnPath.Value );
                                        }
                                        success &= YarnHelper.DoRunYarn( monitor, ckGenFolder, "run build", yarnPath.Value );
                                        monitor.CloseGroup( success ? "Success." : "Failed." );
                                    }

                                    // If the lookup made previously to the target package.json is not on error, we handle
                                    // AutoInstallVSCodeSupport and EnsureTestSupport.
                                    // We do this even on compilation failure (if asked to do so) to be able to work in the
                                    // target project.

                                    // We always ensure that the workspaces:["ck-gen"] and "@local/ck-gen" dependency are here.
                                    if( !invalidPackageJson
                                        && YarnHelper.SetupTargetProjectPackageJson( monitor, projectJsonPath,
                                                                                     targetPackageJson,
                                                                                     out var testScriptCommand,
                                                                                     out var jestVersion,
                                                                                     out var tsJestVersion,
                                                                                     out var typesJestVersion ) )
                                    {
                                        // Before installing VSCode support, we must ensure that typescript is installed in the target
                                        // project, otherwise the TypeScript support won't be installed by the yarn sdks.
                                        // EnsureTestSupport => AutoInstallVSCodeSupport.
                                        if( (BinPathConfiguration.AutoInstallVSCodeSupport || BinPathConfiguration.EnsureTestSupport)
                                            && !YarnHelper.HasVSCodeSupport( monitor, targetProjectPath ) )
                                        {
                                            if( targetTypescriptVersion == null )
                                            {
                                                if( YarnHelper.DoRunYarn( monitor, targetProjectPath, "add --dev typescript", yarnPath.Value ) )
                                                {
                                                    // So the EnsureTestSupport won't reinstall it.
                                                    targetTypescriptVersion = "latest";
                                                }
                                            }
                                            YarnHelper.InstallVSCodeSupport( monitor, targetProjectPath, yarnPath.Value );
                                        }

                                        // If we must ensure test support, we consider that as soon as a "test" script is available
                                        // we are done: the goal is to support "yarn test", Jest is our default test framework but is
                                        // not required.
                                        if( BinPathConfiguration.EnsureTestSupport )
                                        {
                                            if( testScriptCommand != null )
                                            {
                                                monitor.Info( $"TypeScript test script command '{testScriptCommand}' already exists. Skipping EnsureJestTestSupport." );
                                            }
                                            else
                                            {
                                                success &= EnsureJestTestSupport( monitor,
                                                                                  targetProjectPath,
                                                                                  projectJsonPath,
                                                                                  targetTypescriptVersion,
                                                                                  yarnPath.Value,
                                                                                  jestVersion,
                                                                                  tsJestVersion,
                                                                                  typesJestVersion );
                                            }
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
                                    monitor.Debug( $"Deleting '{p.AsSpan(ckGenFolder.Path.Length)}'." );
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

        private static bool EnsureJestTestSupport( IActivityMonitor monitor,
                                                   NormalizedPath targetProjectPath,
                                                   NormalizedPath projectJsonPath,
                                                   string? targetTypescriptVersion,
                                                   NormalizedPath yarnPath,
                                                   string? jestVersion,
                                                   string? tsJestVersion,
                                                   string? typesJestVersion )
        {
            bool success = true;
            using( monitor.OpenInfo( $"Ensuring TypeScript test with Jest." ) )
            {
                string a = string.Empty, i = string.Empty;
                Add( ref a, ref i, "typescript", targetTypescriptVersion );
                Add( ref a, ref i, "jest", jestVersion );
                Add( ref a, ref i, "ts-jest", tsJestVersion );
                Add( ref a, ref i, "@types/jest", typesJestVersion );
                static void Add( ref string a, ref string i, string name, string? version )
                {
                    if( version == null )
                    {
                        if( i.Length == 0 ) i = name;
                        else i += ' ' + name;
                    }
                    else
                    {
                        a = $"{a}{(a.Length == 0 ? "" : ", ")}{name} ({version})";
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
