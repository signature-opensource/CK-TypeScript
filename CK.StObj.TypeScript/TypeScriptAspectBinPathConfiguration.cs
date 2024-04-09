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
    public sealed class TypeScriptAspectBinPathConfiguration
    {
        /// <summary>
        /// The current yarn version that is embedded in the CK.StObj.TypeScript.Engine assembly
        /// and can be automatically installed. See <see cref="AutoInstallYarn"/>.
        /// </summary>
        public const string AutomaticYarnVersion = "4.0.2";

        /// <summary>
        /// The default <see cref="AutomaticTypeScriptVersion"/> version to install.
        /// </summary>
        public const string DefaultTypeScriptVersion = "5.2.2";

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public TypeScriptAspectBinPathConfiguration()
        {
            Barrels = new HashSet<NormalizedPath>();
            Types = new List<TypeScriptTypeConfiguration>();
            TypeFilterName = "TypeScript";
        }

        /// <summary>
        /// Gets or sets the TypeScript target project path that contains the "ck-gen" folder.
        /// This path can be absolute or relative to <see cref="StObjEngineConfiguration.BasePath"/>.
        /// <list type="bullet">
        ///   <item>
        ///   This target project folder is created if it doesn't exist.
        ///   </item>
        ///   <item>
        ///   The "/ck-gen" sub folder is created or cleared if it exists before generating files and folders.
        ///   </item> 
        /// </list>
        /// </summary>
        public NormalizedPath TargetProjectPath { get; set; }

        /// <summary>
        /// Gets the list of <see cref="TypeScriptTypeConfiguration"/>.
        /// </summary>
        public List<TypeScriptTypeConfiguration> Types { get; }

        /// <summary>
        /// Gets or sets the name of this TypeScript configuration that is the <see cref="ExchangeableRuntimeFilter.Name"/>
        /// of the exhangeable type set defined by this configuration.
        /// <para>
        /// This defaults to "TypeScript". Any other name MUST start with "TypeScript": this acts as a simple yet effective
        /// filter to secure a endpoint that exchange Poco compliant types to/from TypeScript. Any endpoint that handles
        /// exchange with an external javascript/TypeScript application MUST reject any ExchangeableRuntimeFilter whose name
        /// is not or doesn't start with "TypeScript".
        /// </para>
        /// </summary>
        public string TypeFilterName { get; set; }

        /// <summary>
        /// Gets or sets the TypeScript version to install when TypeScript is not installed in "<see cref="TargetProjectPath"/>".
        /// Defaults to <see cref="DefaultTypeScriptVersion"/>.
        /// </summary>
        public string AutomaticTypeScriptVersion { get; set; } = DefaultTypeScriptVersion;

        /// <summary>
        /// Gets or sets whether yarn build of "<see cref="TargetProjectPath"/>/ck-gen" should be skipped
        /// as well as any other TypeScript tooling:
        /// when set to true, <see cref="AutoInstallYarn"/>, <see cref="AutoInstallVSCodeSupport"/> and <see cref="EnsureTestSupport"/>
        /// are ignored.
        /// <para>
        /// Defaults to false.
        /// </para>
        /// <para>
        /// </para>
        /// </summary>
        public bool SkipTypeScriptTooling { get; set; }

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
        /// package.json. When no test is available, this installs jest, ts-jest and @types/jest.
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
        /// Gets a list of optional barrel paths that are relative to the "<see cref="TargetProjectPath"/>/ck-gen" folder.
        /// An index.ts file will be generated in each of these folders (see https://basarat.gitbook.io/typescript/main-1/barrel).
        /// <para>
        /// A barrel is systematically generated at the root OutputPath level. This allows sub folders to also have barrels.
        /// </para>
        /// </summary>
        public HashSet<NormalizedPath> Barrels { get; }

        /// <summary>
        /// Initializes a new configuration from a Xml element <see cref="BinPathConfiguration"/>
        /// <see cref="TypeScriptAspectConfiguration.xTypeScript"/> child.
        /// </summary>
        /// <param name="e">The configuration element.</param>
        public TypeScriptAspectBinPathConfiguration( XElement e )
        {
            TargetProjectPath = e.Attribute( TypeScriptAspectConfiguration.xTargetProjectPath )?.Value;
            Barrels = new HashSet<NormalizedPath>( e.Elements( TypeScriptAspectConfiguration.xBarrels )
                                                    .Elements( TypeScriptAspectConfiguration.xBarrel )
                                                        .Select( c => new NormalizedPath( (string?)c.Attribute( StObjEngineConfiguration.xPath ) ?? c.Value ) ) );
            AutomaticTypeScriptVersion = (string?)e.Attribute( TypeScriptAspectConfiguration.xAutomaticTypeScriptVersion ) ?? DefaultTypeScriptVersion;
            AutoInstallVSCodeSupport = (bool?)e.Attribute( TypeScriptAspectConfiguration.xAutoInstallVSCodeSupport ) ?? false;
            AutoInstallYarn = (bool?)e.Attribute( TypeScriptAspectConfiguration.xAutoInstallYarn ) ?? false;
            GitIgnoreCKGenFolder = (bool?)e.Attribute( TypeScriptAspectConfiguration.xGitIgnoreCKGenFolder ) ?? false;
            SkipTypeScriptTooling = (bool?)e.Attribute( TypeScriptAspectConfiguration.xSkipTypeScriptTooling ) ?? false;
            EnsureTestSupport = (bool?)e.Attribute( TypeScriptAspectConfiguration.xEnsureTestSupport ) ?? false;
            Types = e.Elements( StObjEngineConfiguration.xTypes )
                       .Elements( StObjEngineConfiguration.xType )
                       .Select( e => new TypeScriptTypeConfiguration( e ) )
                       .ToList();
            TypeFilterName = (string?)e.Attribute( TypeScriptAspectConfiguration.xTypeFilterName ) ?? "TypeScript";
        }

        /// <summary>
        /// Creates an Xml element with this configuration values that can be added to a <see cref="BinPathConfiguration.ToXml()"/> element.
        /// </summary>
        /// <returns>The element.</returns>
        public XElement ToXml()
        {
            return new XElement( TypeScriptAspectConfiguration.xTypeScript,
                                 new XAttribute( TypeScriptAspectConfiguration.xTargetProjectPath, TargetProjectPath ),
                                 new XElement( TypeScriptAspectConfiguration.xBarrels,
                                               Barrels.Select( p => new XElement( TypeScriptAspectConfiguration.xBarrels, new XAttribute( StObjEngineConfiguration.xPath, p ) ) ) ),
                                 new XAttribute( TypeScriptAspectConfiguration.xTypeFilterName, TypeFilterName ),
                                 new XAttribute( TypeScriptAspectConfiguration.xAutomaticTypeScriptVersion, AutomaticTypeScriptVersion ),
                                 SkipTypeScriptTooling
                                    ? new XAttribute( TypeScriptAspectConfiguration.xSkipTypeScriptTooling, true )
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
                                 new XElement( StObjEngineConfiguration.xTypes, Types.Select( t => t.ToXml() ) )
                               );
        }
    }
}
