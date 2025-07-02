using CK.Core;
using CK.TypeScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CK.Setup;


/// <summary>
/// Helper that models the <see cref="BinPathConfiguration"/> &lt;TypeScript&gt; element.
/// </summary>
public sealed class TypeScriptBinPathAspectConfiguration : MultipleBinPathAspectConfiguration<TypeScriptBinPathAspectConfiguration>
{
    NormalizedPath _targetProjectPath;
    NormalizedPath _targetCKGenPath;

    /// <summary>
    /// Initializes a new empty configuration.
    /// </summary>
    public TypeScriptBinPathAspectConfiguration()
    {
        Barrels = new HashSet<string>();
        OldTypes = new List<TypeScriptTypeConfiguration>();

        Types = new Dictionary<Type, TypeScriptTypeConfiguration2>();
        GlobTypes = new List<TypeScriptTypeGlobConfiguration>();
        ExcludedTypes = new HashSet<Type>();

        ActiveCultures = new HashSet<NormalizedCultureInfo>();
        TypeFilterName = "TypeScript";
        GitIgnoreCKGenFolder = true;
        AutoInstallJest = true;
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
            if( _targetProjectPath != value )
            {
                _targetProjectPath = value;
                _targetCKGenPath = default;
            }
        }
    }

    /// <summary>
    /// Gets the '/ck-gen' path.
    /// </summary>
    public NormalizedPath TargetCKGenPath
    {
        get
        {
            if( _targetCKGenPath.IsEmptyPath )
            {
                _targetCKGenPath = _targetProjectPath.AppendPart( "ck-gen" );
            }
            return _targetCKGenPath;
        }
    }

    /// <summary>
    /// Gets a mutable set of cultures that should be handled by the generated TypeScript code.
    /// <para>
    /// It is useless to register the "en" culture as it is always implicitly added and parent cultures
    /// are automatically added (adding "fr-FR" adds "fr").
    /// </para>
    /// <para>
    /// In configuration files, this is expressed as a comma separated string of BCP47 culture names.
    /// </para>
    /// </summary>
    public HashSet<NormalizedCultureInfo> ActiveCultures { get; }

    [Obsolete("Pouf")]
    public List<TypeScriptTypeConfiguration> OldTypes { get; }

    /// <summary>
    /// Gets the type configurations.
    /// </summary>
    public Dictionary<Type,TypeScriptTypeConfiguration2> Types { get; }

    /// <summary>
    /// Gets the list of <see cref="TypeScriptTypeGlobConfiguration"/>.
    /// </summary>
    public List<TypeScriptTypeGlobConfiguration> GlobTypes { get; }

    /// <summary>
    /// Gets the set of excluded types. When a type appears in this set, it is always
    /// ignored even if it appears in <see cref="Types"/> or <see cref="GlobTypes"/>.
    /// </summary>
    public HashSet<Type> ExcludedTypes { get; }

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
    /// <para>
    /// Defaults to <see cref="CKGenIntegrationMode.Inline"/>.
    /// </para>
    /// </summary>
    public CKGenIntegrationMode IntegrationMode { get; set; }

    /// <summary>
    /// Gets or sets the TypeScript version to install when TypeScript is not installed in "<see cref="TargetProjectPath"/>",
    /// no Yarn sdk is installed and no dependency to TypeScript is declared by the code.
    /// <para>
    /// When missing or invalid, the engine uses a version it knows.
    /// </para>
    /// </summary>
    public string? DefaultTypeScriptVersion { get; set; }

    /// <summary>
    /// Gets or sets whether Yarn must be installed or upgraded.
    /// <para>
    /// Defaults to <see cref="YarnInstallOption.AutoUpgrade"/>.
    /// </para>
    /// </summary>
    public YarnInstallOption InstallYarn { get; set; }

    /// <summary>
    /// Gets or sets whether Jest must be installed.
    /// <para>
    /// Defaults to true.
    /// </para>
    /// </summary>
    public bool AutoInstallJest { get; set; }

    /// <summary>
    /// Gets or sets whether a "/ck-gen/.gitignore" file wih "*" must be created.
    /// Defaults to true.
    /// </summary>
    public bool GitIgnoreCKGenFolder { get; set; }

    /// <summary>
    /// True to add <c>"composite": true</c> to the /ck-gen/tsconfig.json.
    /// <para>
    /// It is up to the developper to ensure that a <c>"references": [ { "path": "./ck-gen" } ]</c> exists in
    /// the target project tsconfig.json and to use <c>tsc --buildMode</c>.
    /// See https://www.typescriptlang.org/docs/handbook/project-references.html.
    /// </para>
    /// <para>
    /// Unfortunately, this is currently not supported by Jest nor by Angular (see <see cref="CKGenIntegrationMode"/>).
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
    public HashSet<string> Barrels { get; }

    /// <inheritdoc />
    protected override void InitializeOnlyThisFrom( XElement e )
    {
        TargetProjectPath = e.Attribute( TypeScriptAspectConfiguration.xTargetProjectPath )?.Value;
        Barrels.Clear();
        Barrels.AddRange( e.Elements( TypeScriptAspectConfiguration.xBarrels )
                                                .Elements( TypeScriptAspectConfiguration.xBarrel )
                                                    .Select( c => (string?)c.Attribute( EngineConfiguration.xPath ) ?? c.Value ) );
        DefaultTypeScriptVersion = (string?)e.Attribute( TypeScriptAspectConfiguration.xDefaultTypeScriptVersion );

        var tsIntegrationMode = (string?)e.Attribute( TypeScriptAspectConfiguration.xIntegrationMode );
        IntegrationMode = tsIntegrationMode == null ? CKGenIntegrationMode.Inline : Enum.Parse<CKGenIntegrationMode>( tsIntegrationMode, ignoreCase: true );

        if( e.Attribute( "AutoInstallYarn" ) != null)
        {
            throw new XmlException( "AutoInstallYarn is obsolete: please use InstallYarn = \"None\", \"AutoInstall\" or \"AutoUpgrade\" (AutoUpgrade is the default)." );
        }
        var yarnInstall = (string?)e.Attribute( TypeScriptAspectConfiguration.xInstallYarn );
        InstallYarn = yarnInstall == null ? YarnInstallOption.AutoUpgrade : Enum.Parse<YarnInstallOption>( yarnInstall, ignoreCase: true );
        
        GitIgnoreCKGenFolder = (bool?)e.Attribute( TypeScriptAspectConfiguration.xGitIgnoreCKGenFolder ) ?? true;

        AutoInstallJest = (bool?)e.Attribute( TypeScriptAspectConfiguration.xAutoInstallJest ) ?? true;
        EnableTSProjectReferences = (bool?)e.Attribute( TypeScriptAspectConfiguration.xEnableTSProjectReferences ) ?? false;

        ActiveCultures.Clear();
        var cultures = (string?)e.Attribute( TypeScriptAspectConfiguration.xActiveCultures );
        if( cultures != null )
        {
            foreach( var name in cultures.Split( ',', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries ) )
            {
                ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( name ) );
            }
        }

        ExcludedTypes.Clear();
        ExcludedTypes.AddRange(
            e.Elements( EngineConfiguration.xExcludedTypes )
             .Elements( EngineConfiguration.xType )
             .Select( e => SimpleTypeFinder.WeakResolver( (string?)e.Attribute( EngineConfiguration.xName ) ?? e.Value, throwOnError: true )! ) );
        Types.Clear();
        GlobTypes.Clear();
        FillTypesAndGlobTypes( e.Elements( EngineConfiguration.xTypes ).Elements( EngineConfiguration.xType ) );

        OldTypes.Clear();
        OldTypes.AddRange( e.Elements( EngineConfiguration.xTypes )
                           .Elements( EngineConfiguration.xType )
                           .Select( e => new TypeScriptTypeConfiguration( e ) ) );
        TypeFilterName = (string?)e.Attribute( TypeScriptAspectConfiguration.xTypeFilterName ) ?? "TypeScript";
    }

    void FillTypesAndGlobTypes( IEnumerable<XElement> typeElements )
    {
        foreach( XElement e in typeElements )
        {
            var tName = (string?)e.Attribute( EngineConfiguration.xName ) ?? e.Value;
            if( string.IsNullOrWhiteSpace( tName ) )
            {
                Throw.XmlException( $"""
                    Invalid assembly qualified type name in:
                    {e}
                    Attribute {EngineConfiguration.xName.LocalName}="Namespace.TypeName, SomeAssembly" is missing.
                    The type name can also appear as the element value: <Type>Namespace.TypeName, SomeAssembly</Type>
                    """ );
            }
            if( tName.Contains( '*' ) )
            {
                ToGlobType( tName, e );
            }
            else
            {
                Type type;
                try
                {
                    type = SimpleTypeFinder.WeakResolver( tName, throwOnError: true )!;
                }
                catch( Exception ex )
                {
                    Throw.XmlException( $"""
                                Unable to resolve type name for:
                                {e}
                                """, ex );
                    return;
                }
                ToType( type, e );
            }
        }
    }

    void ToGlobType( string pattern, XElement e )
    {
        var m = (string?)e.Attribute( TypeScriptAspectConfiguration.xRegistrationMode );
        if( m != null && m.Equals( "None", StringComparison.OrdinalIgnoreCase ) ) return;
        RegistrationMode mode = m == null
                                    ? RegistrationMode.Regular
                                    : Enum.Parse<RegistrationMode>( m, ignoreCase: true );
        GlobTypes.Add( new TypeScriptTypeGlobConfiguration( pattern, TypeScriptTypeAttribute2.ReadFrom( e ), mode ) );
    }

    void ToType( Type type, XElement e )
    {
        if( Types.ContainsKey( type ) )
        {
            Throw.XmlException( $"""
                                Duplicate type name in <Types>:
                                {e}
                                Type '{type.ToCSharpName()}' is already registered. 
                                """ );
        }
        bool required = (bool?)e.Attribute( TypeScriptAspectConfiguration.xRequired ) ?? false;
        Types.Add( type, new TypeScriptTypeConfiguration2( required, TypeScriptTypeAttribute2.ReadFrom( e ) ) );
    }

    /// <inheritdoc />
    protected override void WriteOnlyThisXml( XElement e )
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
               !AutoInstallJest
                ? new XAttribute( TypeScriptAspectConfiguration.xAutoInstallJest, false )
                : null,
               InstallYarn != YarnInstallOption.AutoUpgrade
                ? new XAttribute( TypeScriptAspectConfiguration.xInstallYarn, InstallYarn )
                : null,
               GitIgnoreCKGenFolder is false
                ? new XAttribute( TypeScriptAspectConfiguration.xGitIgnoreCKGenFolder, false )
                : null,
               EnableTSProjectReferences
                ? new XAttribute( TypeScriptAspectConfiguration.xEnableTSProjectReferences, true )
                : null,
               new XAttribute( TypeScriptAspectConfiguration.xActiveCultures, ActiveCultures.Select( c => c.Culture.Name ).Concatenate( ", " ) ),
               new XElement( EngineConfiguration.xTypes,
                                Types.Select( kv => new XElement( EngineConfiguration.xType,
                                                            new XAttribute( TypeScriptAspectConfiguration.xRequired, kv.Value.Required ),
                                                            kv.Value.Configuration?.ToXmlAttributes(),
                                                            kv.Key.GetWeakTypeName() ) )
                                .Concat( GlobTypes.Select( g => g.ToXml() ) )
                           ),
               new XElement( EngineConfiguration.xExcludedTypes,
                             ExcludedTypes.Select( CKCORETypeExtensions.GetWeakTypeName ) )
            );
    }
}
