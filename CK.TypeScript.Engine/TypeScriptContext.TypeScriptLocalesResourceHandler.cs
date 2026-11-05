using CK.BinarySerialization;
using CK.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    // Collector of the TSLocalesContributor (ordered by package, then by registration).
    List<(int Order, TSLocalesContributor Contributor)>? _localesContributors;

    /// <summary>
    /// Registers a contributor to the generated "ck-gen/ts-locales/locales.ts" file. This must be called before the
    /// resource space is built (typically from <see cref="TypeScriptGroupOrPackageAttributeImplExtension"/>'s
    /// <c>OnResPackageAvailable</c>, or from <see cref="ITSCodeGenerator.StartCodeGeneration"/>). The contributions
    /// without package come first, then the ones of the packages in their order, each in the order of their registration.
    /// </summary>
    /// <param name="package">
    /// The package that brings the contribution, or null for a contribution of an engine (a code generator) that comes
    /// before the packages' ones.
    /// </param>
    /// <param name="contributor">The contributor.</param>
    public void AddLocalesContributor( ResPackage? package, TSLocalesContributor contributor )
    {
        Throw.CheckNotNullArgument( contributor );
        _localesContributors ??= new List<(int, TSLocalesContributor)>();
        _localesContributors.Add( (package?.Index ?? -1, contributor) );
    }

    IReadOnlyList<TSLocalesContributor> GetLocalesContributors()
    {
        // OrderBy is a stable sort: the registration order is kept for the same package.
        return _localesContributors == null
                ? []
                : _localesContributors.OrderBy( c => c.Order ).Select( c => c.Contributor ).ToArray();
    }

    sealed class TypeScriptLocalesResourceHandler : LocalesResourceHandler
    {
        readonly NormalizedCultureInfo _defaultCulture;
        readonly IReadOnlyList<TSLocalesContributor> _contributors;

        public TypeScriptLocalesResourceHandler( IResourceSpaceItemInstaller? installer,
                                                 ResCoreData resCoreData,
                                                 ActiveCultureSet activeCultures,
                                                 NormalizedCultureInfo defaultCulture,
                                                 bool sortKeys,
                                                 IReadOnlyList<TSLocalesContributor> contributors )
            : base( installer,
                    resCoreData.SpaceDataCache,
                    "ts-locales",
                    activeCultures,
                    installOption: sortKeys
                                    ? InstallOption.Full | InstallOption.WithSortedKeys
                                    : InstallOption.Full )
        {
            _defaultCulture = defaultCulture;
            _contributors = contributors;
        }

        protected override bool Install( IActivityMonitor monitor )
        {
            if( !base.Install( monitor ) ) return false;
            if( Installer != null )
            {
                using( Installer.PushSubPath( RootFolderName ) )
                {
                    var localesBody = new StringBuilder();
                    // The imports of the contributors, then their preludes.
                    var imports = new List<string>();
                    foreach( var c in _contributors )
                    {
                        c.AddImports( monitor, ActiveCultures, imports );
                    }
                    foreach( var i in imports )
                    {
                        localesBody.AppendLine( i );
                    }
                    foreach( var c in _contributors )
                    {
                        if( !c.WritePrelude( monitor, ActiveCultures, _defaultCulture, localesBody ) ) return false;
                    }

                    localesBody.Append( """
                        export async function loadTranslations( lang: string ): Promise<{ [key: string]: string }> {
                          switch( lang ) {

                        """ );
                    foreach( var c in ActiveCultures.AllActiveCultures )
                    {
                        if( c.Culture != _defaultCulture )
                        {
                            localesBody.Append( "    case '" ).Append( c.Culture.Name ).Append( "': " )
                                        .Append( "return ( await import( './" )
                                        .Append( c.Culture.Name )
                                        .Append( ".json' ) ).default;" )
                                        .AppendLine();
                        }
                    }

                    localesBody.Append( $$"""
                            default: return ( await import( './{{_defaultCulture.Name}}.json' ) ).default;
                          }
                        }

                        """ )
                        .AppendLine();

                    localesBody.Append( """
                        export type LocaleInfo = {
                          name: string;
                          nativeName: string;
                          englishName: string;
                          id: number;

                        """ );
                    var fields = _contributors.SelectMany( c => c.LocaleInfoFields ).ToList();
                    foreach( var f in fields )
                    {
                        localesBody.Append( "  " ).Append( f.Name ).Append( ": " ).Append( f.TypeScriptType ).Append( ';' ).AppendLine();
                    }
                    localesBody.AppendLine( """
                        };

                        """ );

                    localesBody.Append( """
                        export type CKLocales = {
                          [localeCode: string]: LocaleInfo;
                        };

                        export const locales: CKLocales = {

                        """ );

                    foreach( var c in ActiveCultures.AllActiveCultures )
                    {
                        localesBody.Append( "  \"" ).Append( c.Culture.Name )
                                    .Append( "\": { name: '" ).Append( c.Culture.Name )
                                    .Append( "', \"nativeName\": '" ).Append( c.Culture.Culture.NativeName )
                                    .Append( "', \"englishName\": '" ).Append( c.Culture.Culture.EnglishName )
                                    .Append( "', \"id\": " ).Append( c.Culture.Id );
                        foreach( var f in fields )
                        {
                            var value = f.GetValue( monitor, c.Culture );
                            if( value == null ) return false;
                            localesBody.Append( ", \"" ).Append( f.Name ).Append( "\": " ).Append( value );
                        }
                        localesBody.Append( " }," ).AppendLine();
                    }
                    localesBody.Append( """
                        }

                        """ )
                        .AppendLine();

                    localesBody.AppendLine( $"export const DEFAULT_LOCALE_INFO = locales['{_defaultCulture.Name}'];" );

                    foreach( var c in _contributors )
                    {
                        if( !c.WriteEpilogue( monitor, ActiveCultures, _defaultCulture, localesBody ) ) return false;
                    }

                    Installer.Write( $"locales.ts", localesBody.ToString() );
                }
            }
            return true;
        }

        /// <summary>
        /// Direct relay to <see cref="LocalesResourceHandler.ReadLiveState(IActivityMonitor, ResCoreData, IBinaryDeserializer)"/>:
        /// there is no specific live behavior since adding or removing an active culture cannot be done dynamically.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="spaceData">The deserialized resource space data.</param>
        /// <param name="d">The deserializer for the primary <see cref="ResSpace.LiveStateFileName"/>.</param>
        /// <returns>The live updater on success, null on error. Errors are logged.</returns>
        public static new ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResCoreData spaceData, IBinaryDeserializer d )
        {
            return LocalesResourceHandler.ReadLiveState( monitor, spaceData, d );
        }
    }
}
