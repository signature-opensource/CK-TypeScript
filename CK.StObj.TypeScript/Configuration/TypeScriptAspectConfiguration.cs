using CK.Core;
using CK.StObj.TypeScript;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CK.Setup;

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
/// See <see cref="TypeScriptBinPathAspectConfiguration"/> that models this required BinPathConfiguration.
/// </summary>
public sealed partial class TypeScriptAspectConfiguration : EngineAspectConfiguration
{
    /// <summary>
    /// Initializes a new default configuration.
    /// </summary>
    public TypeScriptAspectConfiguration()
    {
        GenerateDocumentation = true;
        DeferFileSave = true;
        LibraryVersions = new Dictionary<string, SVersionBound>();
    }

    /// <summary>
    /// Initializes a new configuration from a Xml element.
    /// </summary>
    /// <param name="e">The configuration element.</param>
    public TypeScriptAspectConfiguration( XElement e )
    {
        PascalCase = (bool?)e.Element( xPascalCase ) ?? false;
        GenerateDocumentation = (bool?)e.Attribute( xGenerateDocumentation ) ?? true;
        DeferFileSave = (bool?)e.Attribute( xDeferFileSave ) ?? true;
        LibraryVersions = e.Element( xLibraryVersions )?
                            .Elements( xLibrary )
                            .Select( e => (e.Attribute( EngineConfiguration.xName )?.Value, e.Attribute( EngineConfiguration.xVersion )?.Value) )
                            .Where( e => !string.IsNullOrWhiteSpace( e.Item1 ) && !string.IsNullOrWhiteSpace( e.Item2 ) )
                            .ToDictionary( e => e.Item1!, e => ParseSVersionBound( e.Item1!, e.Item2! ) )
                            ?? new Dictionary<string, SVersionBound>();
        IgnoreVersionsBound = (bool?)e.Attribute( xIgnoreVersionsBound ) ?? false;

        static SVersionBound ParseSVersionBound( string name, string version )
        {
            var parseResult = SVersionBound.NpmTryParse( version, includePrerelease: false );
            if( !parseResult.IsValid )
            {
                Throw.XmlException( $"Invalid version '{version}' for library '{name}': {parseResult.Error}" );
            }
            // Don't call NormalizeNpmVersionBoundAll() to normalize "*" and "" to ">=0.0.0-0" here as the list
            // may be manually modified.
            // The normalization will be done by the engine.
            return parseResult.Result;
        }
    }

    /// <summary>
    /// Gets a dictionary that defines the version to use for an external package.
    /// The versions specified here override the ones specified in code while
    /// declaring an import.
    ///<para>
    /// The code can provide default versions (final version is upgrade, see <see cref="IgnoreVersionsBound"/>)
    /// or no version at all: in this case the library version must be defined here. The code can also
    /// provide the ">=0.0.0-0" version, that is <see cref="SVersionBound.All"/>.ToString(): the "latest" version of
    /// the package will eventually be used if no other version are set for the library.
    ///</para>
    /// <para>
    /// Example:
    /// <code>
    ///     &lt;LibraryVersions&gt;
    ///        &lt;Library Name="axios" Version="^1.7.2" /&gt;
    ///        &lt;Library Name="luxon" Version=">=3.1.1" /&gt;
    ///     &lt;/LibraryVersions&gt;
    /// </code>
    /// </para>
    /// <para>
    /// It is empty by default. This can contain any kind of library: whether it will be used as a regular, development or even
    /// peer dependency, versions configured here will be used.
    /// </para>
    /// </summary>
    public Dictionary<string, SVersionBound> LibraryVersions { get; }

    /// <summary>
    /// Gets or sets whether when code declares multiple versions for the same library, 
    /// version compatibility must be enforced or not.
    /// <para>
    /// When false (the default), if a package wants "axios": "^0.28.0" (in <see cref="SVersionBound"/> semantics: "0.28.0[LockMajor,Stable]")
    /// and another one wants ">=1.7.2" (that is "1.7.2[Stable]"), this will fail. 
    /// </para>
    /// <para>
    /// When set to true, the greatest <see cref="SVersionBound.Base"/> wins: "1.7.2[Stable]" will be selected.
    /// </para>
    /// </summary>
    public bool IgnoreVersionsBound { get; set; }

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
    /// Gets or sets whether the file system is updated once all code generation is done.
    /// Defaults to true.
    /// </summary>
    public bool DeferFileSave { get; set; }

    /// <summary>
    /// Gets the "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine" assembly qualified name.
    /// </summary>
    public override string AspectType => "CK.Setup.TypeScriptAspect, CK.StObj.TypeScript.Engine";

    /// <summary>
    /// Fills the given Xml element with this configuration values.
    /// </summary>
    /// <param name="e">The element to fill.</param>
    /// <returns>The element.</returns>
    public override XElement SerializeXml( XElement e )
    {
        e.Add( new XAttribute( EngineConfiguration.xVersion, "1" ),
                    PascalCase == false
                        ? new XAttribute( xPascalCase, false )
                        : null,
                    GenerateDocumentation == false
                        ? new XAttribute( xGenerateDocumentation, false )
                        : null,
                    DeferFileSave == false
                        ? new XAttribute( xDeferFileSave, false )
                        : null,
                    IgnoreVersionsBound == true
                        ? new XAttribute( xIgnoreVersionsBound, true )
                        : null,
                    new XElement( xLibraryVersions,
                                  LibraryVersions.Select( kv => new XElement( xLibrary,
                                    new XAttribute( EngineConfiguration.xName, kv.Key ),
                                    new XAttribute( EngineConfiguration.xVersion, kv.Value.ToNpmString() ) ) ) )
             );

        return e;
    }

    /// <inheritdoc />
    public override BinPathAspectConfiguration CreateBinPathConfiguration() => new TypeScriptBinPathAspectConfiguration();

}
