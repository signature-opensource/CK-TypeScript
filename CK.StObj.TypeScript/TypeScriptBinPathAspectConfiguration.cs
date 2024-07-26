using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CK.Setup
{

    /// <summary>
    /// Helper that models the <see cref="BinPathConfiguration"/> &lt;TypeScript&gt; element.
    /// </summary>
    public sealed class TypeScriptBinPathAspectConfiguration : MultipleBinPathAspectConfiguration<TypeScriptBinPathAspectConfiguration>
    {
        private NormalizedPath _targetProjectPath;
        private NormalizedPath _targetCKGenPath;
        private bool _useSrcFolder;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public TypeScriptBinPathAspectConfiguration()
        {
            Barrels = new HashSet<NormalizedPath>();
            Types = new List<TypeScriptTypeConfiguration>();
            TypeFilterName = "TypeScript";
            ModuleSystem = TSModuleSystem.Default;
            UseSrcFolder = true;
        }

        /// <summary>
        /// Gets or sets the TypeScript target project path that contains the "ck-gen" folder.
        /// This path can be absolute or relative to <see cref="EngineConfiguration.BasePath"/>.
        /// It can start with the "{BasePath}", "{OutputPath}" or "{ProjectPath}" placeholders.
        /// <list type="bullet">
        ///   <item>
        ///   This target project folder is created if it doesn't exist.
        ///   </item>
        ///   <item>
        ///   The "/ck-gen" sub folder is created and cleaned up of useless (non generated) files at each generation.
        ///   </item> 
        /// </list>
        /// </summary>
        public NormalizedPath TargetProjectPath
        {
            get => _targetProjectPath;
            set
            {
                _targetProjectPath = value;
                _targetCKGenPath = _targetProjectPath.AppendPart( "ck-gen" );
                if( _useSrcFolder ) _targetCKGenPath = _targetCKGenPath.AppendPart( "src" );
            }
        }

        /// <summary>
        /// Gets the '/ck-gen/src' or '/ck-gen' whether <see cref="UseSrcFolder"/> is true or false.
        /// </summary>
        public NormalizedPath TargetCKGenPath => _targetCKGenPath;

        /// <summary>
        /// Gets or sets whether the generated files will be in '/ck-gen/src' instead of '/ck-gen'.
        /// <para>
        /// Defaults to true.
        /// </para>
        /// </summary>
        public bool UseSrcFolder { get => _useSrcFolder; set => _useSrcFolder = value; }

        /// <summary>
        /// Gets the list of <see cref="TypeScriptTypeConfiguration"/>.
        /// </summary>
        public List<TypeScriptTypeConfiguration> Types { get; }

        /// <summary>
        /// Gets or sets the name of this TypeScript configuration that is the <see cref="ExchangeableRuntimeFilter.Name"/>
        /// of the exhangeable type set defined by this configuration.
        /// <para>
        /// This defaults to "TypeScript". Any other name MUST be "None" or start with "TypeScript": this acts as a simple yet effective
        /// filter to secure a endpoint that exchange Poco compliant types to/from TypeScript. Any endpoint that handles
        /// exchange with an external javascript/TypeScript application MUST reject any ExchangeableRuntimeFilter whose name
        /// is not or doesn't start with "TypeScript".
        /// </para>
        /// <para>
        /// "None" designates a special filter that prevents any Poco compliant types to be exchanged.
        /// When set to "None", no <c>/ck-gen/CK/Core/CTSType.ts</c> file is generated.
        /// </para>
        /// </summary>
        public string TypeFilterName { get; set; }

        /// <summary>
        /// Gets or sets how the /ck-gen generated sources are integrated in the <see cref="TargetProjectPath"/>.
        /// </summary>
        public CKGenIntegrationMode IntegrationMode { get; set; }

        /// <summary>
        /// Gets or sets whether we are building a package. In this mode, the target <see cref="TargetCKGenPath"/> is protected: when a file
        /// doesn't match the result of the generation the file is not overwritten instead a ".G.ext" is written and an error is raised.
        /// <para>
        /// Defaults to false.
        /// </para>
        /// </summary>
        public bool CKGenBuildMode { get; set; }

        /// <summary>
        /// Gets or sets the TypeScript version to install when TypeScript is not installed in "<see cref="TargetProjectPath"/>".
        /// Defaults to <see cref="TypeScriptAspectConfiguration.DefaultTypeScriptVersion"/>.
        /// </summary>
        public string AutomaticTypeScriptVersion { get; set; } = TypeScriptAspectConfiguration.DefaultTypeScriptVersion;

        /// <summary>
        /// Gets or sets whether yarn will be automatically installed (in version <see cref="AutomaticYarnVersion"/>)
        /// if not found in <see cref="TargetProjectPath"/> or above.
        /// <para>
        /// if no yarn can be found in <see cref="TargetProjectPath"/> or above and this is set to false, no TypeScript build will
        /// be done (as if <see cref="SkipTypeScriptTooling"/> was set to true).
        /// </para>
        /// <para>
        /// Defaults to false.
        /// </para>
        /// </summary>
        public bool AutoInstallYarn { get; set; }

        /// <summary>
        /// Gets or sets whether VSCode support must be initialized in <see cref="TargetProjectPath"/>.
        /// <para>
        /// If "<see cref="TargetProjectPath"/>/.vscode" folder or "<see cref="TargetProjectPath"/>/.yarn/sdks" is missing,
        /// the commands "add --dev @yarnpkg/sdks" and "sdks vscode" are executed.
        /// </para>
        /// <para>
        /// This installs the package locally instead of "yarn dlx @yarnpkg/sdks vscode" that does a one-shot install from a temporary
        /// folder: when using zero-install, the vscode support is available directly.
        /// </para>
        /// <para>
        /// Defaults to false.
        /// </para>
        /// </summary>
        public bool AutoInstallVSCodeSupport { get; set; }

        /// <summary>
        /// Gets or sets whether a test command (<c>"scripts": { "test": "..." }</c>) must be available in <see cref="TargetProjectPath"/>'s
        /// package.json. When no "test" command exists in target project package.json, this installs also install jest, ts-jest, @types/jest
        /// and jest-environment-jsdom (as we use <c>testEnvironment: 'jsdom'</c> in jest.config.js).
        /// <para>
        /// Defaults to false.
        /// </para>
        /// </summary>
        /// <remarks>
        /// When set to true, <see cref="AutoInstallVSCodeSupport"/> is also considered true.
        /// </remarks>
        public bool EnsureTestSupport { get; set; }

        /// <summary>
        /// Gets or sets whether a "/ck-gen/.gitignore" file wih "*" must be created.
        /// Defaults to false.
        /// </summary>
        public bool GitIgnoreCKGenFolder { get; set; }

        /// <summary>
        /// Choose the <see cref="TSModuleSystem"/>.
        /// Defaults to <see cref="TSModuleSystem.Default"/>.
        /// </summary>
        public TSModuleSystem ModuleSystem { get; set; }

        /// <summary>
        /// True to add <c>"composite": true</c> to the /ck-gen/tsconfig.json.
        /// <para>
        /// It is up to the developper to ensure that a <c>"references": [ { "path": "./ck-gen" } ]</c> exists in
        /// the target project tsconfig.json and to use <c>tsc --buildMode</c>.
        /// See <see cref="https://www.typescriptlang.org/docs/handbook/project-references.html"/>.
        /// </para>
        /// <para>
        /// Unforunately, this is currently not supported by Jest.
        /// </para>
        /// </summary>
        public bool EnableTSProjectReferences { get; set; }

        /// <summary>
        /// Gets a list of optional barrel paths that are relative to the "<see cref="TargetProjectPath"/>/ck-gen" folder.
        /// An index.ts file will be generated in each of these folders (see https://basarat.gitbook.io/typescript/main-1/barrel).
        /// <para>
        /// A barrel is systematically generated at the root OutputPath level. This allows sub folders to also have barrels.
        /// </para>
        /// </summary>
        public HashSet<NormalizedPath> Barrels { get; }

        /// <inheritdoc />
        protected override void InitializeOneFrom( XElement e )
        {
            TargetProjectPath = e.Attribute( TypeScriptAspectConfiguration.xTargetProjectPath )?.Value;
            Barrels.Clear();
            Barrels.AddRange( e.Elements( TypeScriptAspectConfiguration.xBarrels )
                                                    .Elements( TypeScriptAspectConfiguration.xBarrel )
                                                        .Select( c => new NormalizedPath( (string?)c.Attribute( EngineConfiguration.xPath ) ?? c.Value ) ) );
            AutomaticTypeScriptVersion = (string?)e.Attribute( TypeScriptAspectConfiguration.xAutomaticTypeScriptVersion ) ?? TypeScriptAspectConfiguration.DefaultTypeScriptVersion;

            var tsIntegrationMode = (string?)e.Attribute( TypeScriptAspectConfiguration.xIntegrationMode );
            IntegrationMode = tsIntegrationMode == null ? CKGenIntegrationMode.Inline : Enum.Parse<CKGenIntegrationMode>( tsIntegrationMode, ignoreCase: true );

            AutoInstallVSCodeSupport = (bool?)e.Attribute( TypeScriptAspectConfiguration.xAutoInstallVSCodeSupport ) ?? false;
            AutoInstallYarn = (bool?)e.Attribute( TypeScriptAspectConfiguration.xAutoInstallYarn ) ?? false;
            GitIgnoreCKGenFolder = (bool?)e.Attribute( TypeScriptAspectConfiguration.xGitIgnoreCKGenFolder ) ?? false;
            EnsureTestSupport = (bool?)e.Attribute( TypeScriptAspectConfiguration.xEnsureTestSupport ) ?? false;
            CKGenBuildMode = (bool?)e.Attribute( TypeScriptAspectConfiguration.xCKGenBuildMode ) ?? false;
            UseSrcFolder = (bool?)e.Attribute( TypeScriptAspectConfiguration.xUseSrcFolder ) ?? true;
            var tsModuleSystem = (string?)e.Attribute( TypeScriptAspectConfiguration.xModuleSystem );
            ModuleSystem = tsModuleSystem == null ? TSModuleSystem.Default : Enum.Parse<TSModuleSystem>( tsModuleSystem, ignoreCase: true );
            EnableTSProjectReferences = (bool?)e.Attribute( TypeScriptAspectConfiguration.xEnableTSProjectReferences ) ?? false;

            Types.Clear();
            Types.AddRange( e.Elements( EngineConfiguration.xTypes )
                               .Elements( EngineConfiguration.xType )
                               .Select( e => new TypeScriptTypeConfiguration( e ) ) );
            TypeFilterName = (string?)e.Attribute( TypeScriptAspectConfiguration.xTypeFilterName ) ?? "TypeScript";
        }

        /// <inheritdoc />
        protected override void WriteOneXml( XElement e )
        {
            e.Add( new XAttribute( TypeScriptAspectConfiguration.xTargetProjectPath, TargetProjectPath ),
                   new XElement( TypeScriptAspectConfiguration.xBarrels,
                                    Barrels.Select( p => new XElement( TypeScriptAspectConfiguration.xBarrel, new XAttribute( EngineConfiguration.xPath, p ) ) ) ),
                   new XAttribute( TypeScriptAspectConfiguration.xTypeFilterName, TypeFilterName ),
                   new XAttribute( TypeScriptAspectConfiguration.xAutomaticTypeScriptVersion, AutomaticTypeScriptVersion ),
                   IntegrationMode is not CKGenIntegrationMode.Inline
                    ? new XAttribute( TypeScriptAspectConfiguration.xIntegrationMode, IntegrationMode.ToString() )
                    : null,
                   EnsureTestSupport
                    ? new XAttribute( TypeScriptAspectConfiguration.xEnsureTestSupport, true )
                    : null,
                   AutoInstallYarn
                    ? new XAttribute( TypeScriptAspectConfiguration.xAutoInstallYarn, true )
                    : null,
                   GitIgnoreCKGenFolder
                    ? new XAttribute( TypeScriptAspectConfiguration.xGitIgnoreCKGenFolder, true )
                    : null,
                   AutoInstallVSCodeSupport
                    ? new XAttribute( TypeScriptAspectConfiguration.xAutoInstallVSCodeSupport, true )
                    : null,
                   CKGenBuildMode
                    ? new XAttribute( TypeScriptAspectConfiguration.xCKGenBuildMode, true )
                    : null,
                   !UseSrcFolder
                    ? new XAttribute( TypeScriptAspectConfiguration.xUseSrcFolder, false )
                    : null,
                   ModuleSystem != TSModuleSystem.Default
                    ? new XAttribute( TypeScriptAspectConfiguration.xModuleSystem, ModuleSystem.ToString() )
                    : null,
                   EnableTSProjectReferences
                    ? new XAttribute( TypeScriptAspectConfiguration.xEnableTSProjectReferences, true )
                    : null,
                   new XElement( EngineConfiguration.xTypes, Types.Select( t => t.ToXml() ) )
                );
        }
    }

}
