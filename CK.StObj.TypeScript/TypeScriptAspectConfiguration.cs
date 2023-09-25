using CK.Core;
using CK.StObj.TypeScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Configures TypeScript generation.
    /// <para>
    /// Each <see cref="BinPathConfiguration"/> that requires TypeScript code to be generated must
    /// contain a &lt;TypeScript&gt; with the attribute &lt;OutputPath&gt;.
    /// These OutputPaths can be absolute or start with a {BasePath}, {OutputPath} or {ProjectPath} prefix: the
    /// final paths will be resolved.
    /// </para>
    /// <para>
    /// The &lt;TypeScript&gt; element can contain a &lt;Barrels&gt; element with &lt;Barrel Path="sub/folder"/&gt; elements:
    /// that will generate index.ts files in the specified folders (see https://basarat.gitbook.io/typescript/main-1/barrel).
    /// Use &lt;Barrel Path=""/&gt; to create a barrel at the root level.
    /// </para>
    /// See <see cref="TypeScriptAspectBinPathConfiguration"/> that models this required BinPathConfiguration.
    /// </summary>
    public sealed class TypeScriptAspectConfiguration : IStObjEngineAspectConfiguration
    {
        /// <summary>
        /// The <see cref="PascalCase"/> attribute name.
        /// </summary>
        public static readonly XName xPascalCase = XNamespace.None + "PascalCase";

        /// <summary>
        /// The <see cref="GenerateDocumentation"/> attribute name.
        /// </summary>
        public static readonly XName xGenerateDocumentation = XNamespace.None + "GenerateDocumentation";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration.SkipTypeScriptBuild"/> attribute name.
        /// </summary>
        public static readonly XName xSkipTypeScriptBuild = XNamespace.None + "SkipTypeScriptBuild";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration.EnsureTestSupport"/> attribute name.
        /// </summary>
        public static readonly XName xEnsureTestSupport = XNamespace.None + "EnsureTestSupport";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration.AutoInstallYarn"/> attribute name.
        /// </summary>
        public static readonly XName xAutoInstallYarn = XNamespace.None + "AutoInstallYarn";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration.GitIgnoreCKGenFolder"/> attribute name.
        /// </summary>
        public static readonly XName xGitIgnoreCKGenFolder = XNamespace.None + "GitIgnoreCKGenFolder";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration.AutoInstallVSCodeSupport"/> attribute name.
        /// </summary>
        public static readonly XName xAutoInstallVSCodeSupport = XNamespace.None + "AutoInstallVSCodeSupport";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration"/> element name.
        /// </summary>
        public static readonly XName xTypeScript = XNamespace.None + "TypeScript";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptAspectBinPathConfiguration.TargetProjectPath"/>.
        /// </summary>
        public static readonly XName xTargetProjectPath = XNamespace.None + "TargetProjectPath";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.TypeName"/>.
        /// </summary>
        public static readonly XName xTypeName = XNamespace.None + "TypeName";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.Folder"/>.
        /// </summary>
        public static readonly XName xFolder = XNamespace.None + "Folder";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.FileName"/>.
        /// </summary>
        public static readonly XName xFileName = XNamespace.None + "FileName";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.SameFileAs"/>.
        /// </summary>
        public static readonly XName xSameFileAs = XNamespace.None + "SameFileAs";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.SameFolderAs"/>.
        /// </summary>
        public static readonly XName xSameFolderAs = XNamespace.None + "SameFolderAs";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration.Barrels"/> element name.
        /// </summary>
        public static readonly XName xBarrels = XNamespace.None + "Barrels";

        /// <summary>
        /// The child element name of <see cref="TypeScriptAspectBinPathConfiguration.Barrels"/>.
        /// </summary>
        public static readonly XName xBarrel = XNamespace.None + "Barrel";

        /// <summary>
        /// Initializes a new default configuration.
        /// </summary>
        public TypeScriptAspectConfiguration()
        {
            GenerateDocumentation = true;
        }

        /// <summary>
        /// Initializes a new configuration from a Xml element.
        /// </summary>
        /// <param name="e">The configuration element.</param>
        public TypeScriptAspectConfiguration( XElement e )
        {
            PascalCase = (bool?)e.Element( xPascalCase ) ?? false;
            GenerateDocumentation = (bool?)e.Attribute( xGenerateDocumentation ) ?? true;
        }

        /// <summary>
        /// Fills the given Xml element with this configuration values.
        /// </summary>
        /// <param name="e">The element to fill.</param>
        /// <returns>The element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( new XAttribute( StObjEngineConfiguration.xVersion, "1" ),
                        PascalCase == false
                            ? new XAttribute( xPascalCase, false )
                            : null,
                        GenerateDocumentation == false
                            ? new XAttribute( xGenerateDocumentation, false )
                            : null
                 );
            return e;
        }

        /// <summary>
        /// Gets or sets whether TypeScript generated properties should be PascalCased.
        /// Defaults to false (identifiers are camelCased).
        /// </summary>
        public bool PascalCase { get; set; }

        /// <summary>
        /// Gets or sets whether documentation should be generated.
        /// Defaults to true.
        /// </summary>
        public bool GenerateDocumentation { get; set; }

        /// <summary>
        /// Gets the "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine" assembly qualified name.
        /// </summary>
        public string AspectType => "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine";

    }

}
