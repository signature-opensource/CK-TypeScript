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
    public class TypeScriptAspectConfiguration : IStObjEngineAspectConfiguration
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
        /// The <see cref="GeneratePocoInterfaces"/> attribute name.
        /// </summary>
        public static readonly XName xGeneratePocoInterfaces = XNamespace.None + "GeneratePocoInterfaces";

        /// <summary>
        /// The <see cref="SkipTypeScriptBuild"/> attribute name.
        /// </summary>
        public static readonly XName xSkipTypeScriptBuild = XNamespace.None + "SkipTypeScriptBuild";

        /// <summary>
        /// The <see cref="TypeScriptAspectBinPathConfiguration"/> element name.
        /// </summary>
        public static readonly XName xTypeScript = XNamespace.None + "TypeScript";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptAspectBinPathConfiguration.OutputPath"/>.
        /// </summary>
        public static readonly XName xOutputPath = XNamespace.None + "OutputPath";

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
            Types = new List<string>();
        }

        /// <summary>
        /// Initializes a new configuration from a Xml element.
        /// </summary>
        /// <param name="e">The configuration element.</param>
        public TypeScriptAspectConfiguration( XElement e )
        {
            PascalCase = (bool?)e.Element( xPascalCase ) ?? false;
            GenerateDocumentation = (bool?)e.Element( xGenerateDocumentation ) ?? true;
            SkipTypeScriptBuild = (bool?)e.Element( xSkipTypeScriptBuild ) ?? false;
            Types = e.Element( StObjEngineConfiguration.xTypes )?.Elements( StObjEngineConfiguration.xType ).Select( e => e.Value ).ToList()
                    ?? new List<string>();
        }

        /// <summary>
        /// Fills the given Xml element with this configuration values.
        /// </summary>
        /// <param name="e">The element to fill.</param>
        /// <returns>The element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( new XAttribute( StObjEngineConfiguration.xVersion, "1" ),
                        new XElement( xPascalCase, PascalCase ),
                        GenerateDocumentation == false
                            ? new XAttribute( xGenerateDocumentation, false )
                            : null,
                        GeneratePocoInterfaces == true
                            ? new XAttribute( xGeneratePocoInterfaces, true )
                            : null,
                        SkipTypeScriptBuild
                            ? new XAttribute( xSkipTypeScriptBuild, true )
                            : null,
                        new XElement( StObjEngineConfiguration.xTypes,
                                Types.Select( t => new XElement( StObjEngineConfiguration.xType, t ) ) ) );
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
        /// Gets or sets whether documentation should be generated.
        /// Defaults to false: this is an opt-in since TypeScript interfaces are not really useful.
        /// </summary>
        public bool GeneratePocoInterfaces { get; set; }

        /// <summary>
        /// Gets or sets whether TypeScriptBuild should be skipped.
        /// Defaults to false.
        /// </summary>
        public bool SkipTypeScriptBuild { get; set; }

        /// <summary>
        /// Gets the list of Poco type names (primary interface full name or <see cref="ExternalNameAttribute"/>)
        /// that will be generated in addition to types marked with <see cref="TypeScriptAttribute"/>.
        /// </summary>
        public List<string> Types { get; }

        /// <summary>
        /// Gets the "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine" assembly qualified name.
        /// </summary>
        public string AspectType => "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine";

    }
}
