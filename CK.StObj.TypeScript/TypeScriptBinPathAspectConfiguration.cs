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
        NormalizedPath _targetProjectPath;
        NormalizedPath _targetCKGenPath;
        bool _useSrcFolder;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public TypeScriptBinPathAspectConfiguration()
        {
            Barrels = new HashSet<NormalizedPath>();
            Types = new List<TypeScriptTypeConfiguration>();
            TypeFilterName = "TypeScript";
            ModuleSystem = TSModuleSystem.Default;
            GitIgnoreCKGenFolder = true;
        }

        /// <summary>
        /// Gets the Engine level configuration.
        /// </summary>
        public new TypeScriptAspectConfiguration? AspectConfiguration => (TypeScriptAspectConfiguration?)base.AspectConfiguration;

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
        /// Defaults to false.
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
        /// Gets or sets the TypeScript version to install when TypeScript is not installed in "<see cref="TargetProjectPath"/>",
        /// no Yarn sdk is installed and no dependency to TypeScript is declared by the code.
        /// <para>
        /// When missing or invalid, the engine uses a version it knows.
        /// </para>
        /// </summary>
        public string? DefaultTypeScriptVersion { get; set; }

        /// <summary>
        /// Gets or sets whether yarn will be automatically installed (in version <see cref="AutomaticYarnVersion"/>)
        /// if not found in <see cref="TargetProjectPath"/> or above.
        /// <para>
        /// if no yarn can be found in <see cref="TargetProjectPath"/> or above and this is set to false, no TypeScript build will
        /// be done (as if <see cref="IntegrationMode"/> was set to <see cref="CKGenIntegrationMode.None"/>).
        /// </para>
        /// <para>
        /// Defaults to false.
        /// </para>
        /// </summary>
        public bool AutoInstallYarn { get; set; }

        /// <summary>
        /// Gets or sets whether Jest must be installed.
        /// <para>
        /// Defaults to false.
        /// </para>
        /// </summary>
        public bool AutoInstallJest { get; set; }

        /// <summary>
        /// Gets or sets whether a "/ck-gen/.gitignore" file wih "*" must be created.
        /// Defaults to true.
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
            DefaultTypeScriptVersion = (string?)e.Attribute( TypeScriptAspectConfiguration.xDefaultTypeScriptVersion );

            var tsIntegrationMode = (string?)e.Attribute( TypeScriptAspectConfiguration.xIntegrationMode );
            IntegrationMode = tsIntegrationMode == null ? CKGenIntegrationMode.Inline : Enum.Parse<CKGenIntegrationMode>( tsIntegrationMode, ignoreCase: true );

            AutoInstallYarn = (bool?)e.Attribute( TypeScriptAspectConfiguration.xAutoInstallYarn ) ?? false;
            GitIgnoreCKGenFolder = (bool?)e.Attribute( TypeScriptAspectConfiguration.xGitIgnoreCKGenFolder ) ?? true;

            AutoInstallJest = (bool?)e.Attribute( TypeScriptAspectConfiguration.xAutoInstallJest )
                              ?? (bool?)e.Attribute( "EnsureTestSupport" ) // Legacy.
                              ?? false;

            CKGenBuildMode = (bool?)e.Attribute( TypeScriptAspectConfiguration.xCKGenBuildMode ) ?? false;
            UseSrcFolder = (bool?)e.Attribute( TypeScriptAspectConfiguration.xUseSrcFolder ) ?? false;
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
                   DefaultTypeScriptVersion != null 
                    ? new XAttribute( TypeScriptAspectConfiguration.xDefaultTypeScriptVersion, DefaultTypeScriptVersion )
                    : null,
                   IntegrationMode is not CKGenIntegrationMode.Inline
                    ? new XAttribute( TypeScriptAspectConfiguration.xIntegrationMode, IntegrationMode.ToString() )
                    : null,
                   AutoInstallJest
                    ? new XAttribute( TypeScriptAspectConfiguration.xAutoInstallJest, true )
                    : null,
                   AutoInstallYarn
                    ? new XAttribute( TypeScriptAspectConfiguration.xAutoInstallYarn, true )
                    : null,
                   GitIgnoreCKGenFolder is false
                    ? new XAttribute( TypeScriptAspectConfiguration.xGitIgnoreCKGenFolder, false )
                    : null,
                   CKGenBuildMode
                    ? new XAttribute( TypeScriptAspectConfiguration.xCKGenBuildMode, true )
                    : null,
                   UseSrcFolder
                    ? new XAttribute( TypeScriptAspectConfiguration.xUseSrcFolder, true )
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
