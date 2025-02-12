using CK.Core;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveTSLocales
{
    readonly LiveState _state;
    ImmutableArray<ITSLocalePackage> _packages;

    public LiveTSLocales( LiveState state )
    {
        _state = state;
    }

    public bool Load( IActivityMonitor monitor )
    {
        var a = StateSerializer.ReadFile( monitor,
                                          _state.Paths.StateFolderPath + TSLocaleSerializer.FileName,
                                          ( monitor, r ) => TSLocaleSerializer.ReadLiveTSLocales( monitor, r, _state.LocalPackages ) );
        if( a == null )
        {
            _packages = default;
            return false;
        }
        _packages = ImmutableCollectionsMarshal.AsImmutableArray( a );
        return true;
    }

    public bool Apply( IActivityMonitor monitor )
    {
        using var _ = monitor.OpenInfo( $"Updating 'ck-gen/ts-locales'." );
        bool success = true;
        var f = new FinalLocaleCultureSet( isPartialSet: false, "LiveTSLocales" );
        foreach( var p in _packages )
        {
            success &= p.ApplyLocaleCultureSet( monitor, _state.ActiveCultures, f );
        }
        if( success )
        {
            success &= _state.CKGenTransform.LoadLocales( monitor,
                                                          _state.ActiveCultures,
                                                          out var appLocales,
                                                          "ts-locales",
                                                          isOverrideFolder: true );
            if( appLocales != null )
            {
                f.Add( monitor, appLocales );
            }
            if( success )
            {
                WriteFinalSet( monitor, f );
            }
        }
        return success;
    }

    void WriteFinalSet( IActivityMonitor monitor, FinalLocaleCultureSet final )
    {
        final.PropagateFallbackTranslations( monitor );
        var tsLocaleTarget = _state.Paths.CKGenPath + "ts-locales" + Path.DirectorySeparatorChar;
        foreach( var set in final.Root.FlattenedAll )
        {
            // Use the CultureInfo to have the "correct" casing for culture names.
            var fileName = $"{set.Culture.Culture.Name}.json";
            using( var stream = File.Create( tsLocaleTarget + fileName ) )
            {
                using var w = new Utf8JsonWriter( stream, new JsonWriterOptions() { Indented = true } );
                w.WriteStartObject();
                foreach( var t in set.Translations )
                {
                    w.WriteString( t.Key, t.Value.Text );
                }
                w.WriteEndObject();
            }
        }
    }

}

