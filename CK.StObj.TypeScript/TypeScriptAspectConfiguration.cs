using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Configures TypeScript generation.
    /// Each <see cref="BinPathConfiguration"/> that requires TypeScript code to be generated must
    /// contain a &lt;TypeScript&gt; element with one or more &lt;OutputPath&gt; children elements.
    /// These OutputPaths can be absolute or start with a {BasePath}, {OutputPath} or {ProjectPath} prefix: the
    /// final paths will be resolved.
    /// <para>
    /// The &lt;TypeScript&gt; element can contain a &lt;Barrels&gt; element with &lt;Barrel Path="sub/folder"/&gt; elements:
    /// that will generate index.ts files in the specified folders (see https://basarat.gitbook.io/typescript/main-1/barrel).
    /// Use &lt;Barrel Path=""/&gt; to create a barrel at the root level.
    /// </para>
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
            GenerateDocumentation = (bool?)e.Element( xGenerateDocumentation ) ?? true;
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
                            : null );
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
        /// Gets the "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine" assembly qualified name.
        /// </summary>
        public string AspectType => "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine";

    }
}
