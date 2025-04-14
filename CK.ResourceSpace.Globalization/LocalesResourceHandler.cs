using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CK.Core;

/// <summary>
/// Locales folder handler.
/// See <see cref="ResourceContainerGlobalizationExtension.LoadTranslations(IResourceContainer, CK.Core.IActivityMonitor, CK.Core.ActiveCultureSet, out TranslationDefinitionSet?, string, bool)"/>.
/// </summary>
public partial class LocalesResourceHandler : ResourceSpaceFolderHandler
{
    readonly LocalesCache _cache;
    readonly InstallOption _installOption;
    FinalTranslationSet? _finalTranslations;

    /// <summary>
    /// Specify how the final locale translation files must be generated.
    /// </summary>
    public enum InstallOption
    {
        /// <summary>
        /// Generates a file for each culture in the <see cref="ActiveCultureSet"/> with the
        /// <see cref="IFinalTranslationSet.RootPropagatedTranslations"/> even when there is no translations.
        /// </summary>
        Full,

        /// <summary>
        /// Generates a file for each existing <see cref="FinalTranslationSet.AllTranslationSets"/>
        /// with the <see cref="IFinalTranslationSet.RootPropagatedTranslations"/> only if the set
        /// is not empty.
        /// </summary>
        FullNoEmptySet,

        /// <summary>
        /// Generates a file for each culture in the <see cref="ActiveCultureSet"/> with its
        /// <see cref="IFinalTranslationSet.Translations"/> even when there is no translations.
        /// </summary>
        Minimal,

        /// <summary>
        /// Generates a file for each existing <see cref="FinalTranslationSet.AllTranslationSets"/>
        /// with its <see cref="IFinalTranslationSet.Translations"/> only if the set
        /// is not empty.
        /// </summary>
        MinimalNoEmptySet
    }

    /// <summary>
    /// Initializes a new locales resources handler.
    /// </summary>
    /// <param name="packageDataCache">The package data cache.</param>
    /// <param name="rootFolderName">The folder name (typically "locales", "ts-locales", etc.).</param>
    /// <param name="activeCultures">The required active cultures.</param>
    /// <param name="installOption">How the final locale files must be generated.</param>
    public LocalesResourceHandler( IResourceSpaceItemInstaller? installer,
                                   ISpaceDataCache packageDataCache,
                                   string rootFolderName,
                                   ActiveCultureSet activeCultures,
                                   InstallOption installOption )
        : base( installer, rootFolderName )
    {
        _cache = new LocalesCache( packageDataCache, activeCultures, RootFolderName );
        _installOption = installOption;
    }

    /// <summary>
    /// Gets the cache instance to which this data handler is bound.
    /// </summary>
    public ISpaceDataCache ResPackageDataCache => _cache.SpaceCache;

    /// <summary>
    /// Gets the final assets that have been successfully initialized.
    /// <see cref="FinalTranslationSet.IsAmbiguous"/> is necessarily false.
    /// </summary>
    public FinalTranslationSet? FinalTranslations => _finalTranslations;

    /// <summary>
    /// Gets whether this is empty: there is only the "en" <see cref="ActiveCultureSet.Root"/> default active culture
    /// and there is no translation at all for this root.
    /// <para>
    /// This is always empty until <see cref="Initialize(IActivityMonitor, ResSpaceData)"/> is called.
    /// </para>
    /// </summary>
    public bool IsEmpty => _cache.ActiveCultures.Count == 1 && _finalTranslations?.Translations.Count == 0;

    /// <summary>
    /// Checks that the <see cref="FinalTranslations"/> is not ambiguous.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="spaceData">The resource data.</param>
    /// <returns>True if the FinalTranslations can be successfully computed and that no ambiguous resources exist.</returns>
    protected override bool Initialize( IActivityMonitor monitor, ResSpaceData spaceData )
    {
        _finalTranslations = GetUnambiguousFinalTranslations( monitor, spaceData );
        return _finalTranslations != null;
    }

    FinalTranslationSet? GetUnambiguousFinalTranslations( IActivityMonitor monitor, ResSpaceData spaceData )
    {
        FinalTranslationSet? r = _cache.Obtain( monitor, spaceData.AppPackage );
        if( r != null )
        {
            if( r.IsAmbiguous )
            {
                var ambiguities = r.Ambiguities
                                          .Select( kv => $"""
                                      '{kv.Key}' is defined in {kv.Value.Origin} with text '{kv.Value.Text}'.
                                      But is also defined in:
                                      {kv.Value.Ambiguities!.Select( r => $"{r.Origin.ToString()} with text '{r.Text}'." ).Concatenate( Environment.NewLine )}.
                                      """ );

                monitor.Error( $"""
                Ambiguities detected in final translations:
                {ambiguities.Concatenate( Environment.NewLine )}
                """ );
                return null;
            }
        }
        return r;
    }

    /// <summary>
    /// Saves the initialized <see cref="FinalTranslations"/> into into this <see cref="ResourceSpaceFolderHandler.Installer"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on succes, false one error (errors have been logged).</returns>
    protected override bool Install( IActivityMonitor monitor )
    {
        if( Installer is null )
        {
            monitor.Warn( $"No installer associated to '{ToString()}'. Skipped." );
            return true;
        }
        if( IsEmpty )
        {
            monitor.Warn( $"No '{RootFolderName}' folders found. Skipped." );
            return true;
        }
        Throw.CheckState( _finalTranslations != null );
        return WriteFinal( monitor, _finalTranslations, Installer );
    }

    bool WriteFinal( IActivityMonitor monitor,
                     FinalTranslationSet final,
                     IResourceSpaceItemInstaller installer )
    {
        try
        {
            NormalizedPath folder = RootFolderName;
            switch( _installOption )
            {
                case InstallOption.Full:
                    WriteAllCultures( final, folder, installer, rootPropagated: true );
                    break;
                case InstallOption.FullNoEmptySet:
                    WriteExistingSets( final, folder, installer, rootPropagated: true );
                    break;
                case InstallOption.Minimal:
                    WriteAllCultures( final, folder, installer, rootPropagated: false );
                    break;
                case InstallOption.MinimalNoEmptySet:
                    WriteExistingSets( final, folder, installer, rootPropagated: false );
                    break;
            }
            return true;
        }
        catch( Exception ex )
        {
            monitor.Error( "While saving FinalTranslations.", ex );
            return false;
        }


        static void WriteExistingSets( FinalTranslationSet final, NormalizedPath folder, IResourceSpaceItemInstaller target, bool rootPropagated )
        {
            foreach( var set in final.AllTranslationSets.Where( set => set.Translations.Count > 0 ) )
            {
                var fPath = folder.AppendPart( $"{set.Culture.Culture.Name}.json" );
                var translations = rootPropagated
                                    ? set.RootPropagatedTranslations
                                    : set.Translations;
                WriteJson( target, fPath, translations );
            }
        }

        static void WriteAllCultures( FinalTranslationSet final, NormalizedPath folder, IResourceSpaceItemInstaller target, bool rootPropagated )
        {
            foreach( var c in final.Culture.ActiveCultures.AllActiveCultures )
            {
                var fPath = folder.AppendPart( $"{c.Culture.Name}.json" );

                var set = final.FindTranslationSet( c );
                var translations = set == null
                                    ? null
                                    : rootPropagated
                                        ? set.RootPropagatedTranslations
                                        : set.Translations;
                if( translations == null )
                {
                    target.Write( fPath, "{}" );
                }
                else
                {
                    WriteJson( target, fPath, translations );
                }
            }
        }

        static void WriteJson( IResourceSpaceItemInstaller target,
                               NormalizedPath fPath,
                               IEnumerable<KeyValuePair<string, FinalTranslationValue>> translations )
        {
            using( var s = target.OpenWriteStream( fPath ) )
            using( var w = new Utf8JsonWriter( s, new JsonWriterOptions() { Indented = true } ) )
            {
                w.WriteStartObject();
                foreach( var t in translations )
                {
                    w.WriteString( t.Key, t.Value.Text );
                }
                w.WriteEndObject();
            }
        }
    }

}
