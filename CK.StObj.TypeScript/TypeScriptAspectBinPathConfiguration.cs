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
        /// Initializes a new configuration with a default root <see cref="Barrels"/>.
        /// The <see cref="OutputPath"/> must be specified.
        /// </summary>
        public TypeScriptAspectBinPathConfiguration()
        {
            Barrels = new HashSet<NormalizedPath>
            {
                new NormalizedPath()
            };
        }

        /// <summary>
        /// Gets the package output path.
        /// This path can be absolute or start with a {BasePath}, {OutputPath} or {ProjectPath} first part: the
        /// final path will be resolved by <see cref="StObjEngineConfiguration.BasePath"/>, <see cref="BinPathConfiguration.OutputPath"/>
        /// or <see cref="BinPathConfiguration.ProjectPath"/>.
        /// </summary>
        public NormalizedPath OutputPath { get; set; }

        /// <summary>
        /// Gets a list of optional barrel paths that are relative to the <see cref="OutputPath"/>.
        /// An index.ts file will be generated in each of these folders (see https://basarat.gitbook.io/typescript/main-1/barrel).
        /// <para>
        /// By default, an empty <see cref="NormalizedPath"/> creates a barrel at the root level.
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
            OutputPath = e.Attribute( TypeScriptAspectConfiguration.xOutputPath )?.Value;
            Barrels = new HashSet<NormalizedPath>( e.Elements( TypeScriptAspectConfiguration.xBarrels )
                                                    .Elements( TypeScriptAspectConfiguration.xBarrel )
                                                        .Select( c => new NormalizedPath( (string?)c.Attribute( StObjEngineConfiguration.xPath ) ?? c.Value ) ) );
        }

        /// <summary>
        /// Creates an Xml element with this configuration values that can be added to a <see cref="BinPathConfiguration.ToXml()"/> element.
        /// </summary>
        /// <returns>The element.</returns>
        public XElement ToXml()
        {
            return new XElement( TypeScriptAspectConfiguration.xTypeScript,
                                 new XAttribute( TypeScriptAspectConfiguration.xOutputPath, OutputPath ),
                                 new XElement( TypeScriptAspectConfiguration.xBarrels,
                                               Barrels.Select( p => new XElement( TypeScriptAspectConfiguration.xBarrels, new XAttribute( StObjEngineConfiguration.xPath, p ) ) ) ) );
        }
    }
}
