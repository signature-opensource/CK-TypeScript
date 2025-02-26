using CK.Core;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveTSLocales
{
    readonly LiveState _liveState;
    ImmutableArray<ITSLocalePackage> _packages;
    InternalState _state;

    public LiveTSLocales( LiveState state )
    {
        _liveState = state;
    }

    public enum InternalState
    {
        None,
        Error,
        Loaded,
        Dirty
    }

    public bool IsLoaded => _state != InternalState.None;

    public bool IsValid => _state >= InternalState.Loaded;

    public bool IsDirty => _state == InternalState.Dirty;

    public bool Load( IActivityMonitor monitor )
    {
        Throw.DebugAssert( !IsLoaded );
        var a = StateSerializer.ReadFile( monitor,
                                          _liveState.Paths.StateFolderPath + TSLocaleSerializer.FileName,
                                          ( monitor, r ) => TSLocaleSerializer.ReadLiveTSLocales( monitor, r, _liveState.LocalPackages ) );
        if( a == null )
        {
            _state = InternalState.Error;
            _packages = default;
            return false;
        }
        _state = InternalState.Loaded;
        _packages = ImmutableCollectionsMarshal.AsImmutableArray( a );
        return true;
    }

    internal void OnChange( IActivityMonitor monitor, LocalPackage? package, string subPath )
    {
        Throw.DebugAssert( IsValid );
        // Locales are computed as a whole. Each jsonc file can have an
        // impact on the final set. We don't try to be clever here.
        _state = InternalState.Dirty;
    }

    internal bool ApplyChanges( IActivityMonitor monitor )
    {
        Throw.DebugAssert( IsDirty );
        bool success = true;
        using var _ = monitor.OpenInfo( $"Updating 'ck-gen/ts-locales'." );
        var f = new FinalLocaleCultureSet( isPartialSet: false, "LiveTSLocales" );
        foreach( var p in _packages )
        {
            success &= p.ApplyLocaleCultureSet( monitor, _liveState.ActiveCultures, f );
        }
        if( success )
        {
            success &= _liveState.CKGenTransform.LoadLocales( monitor,
                                                              _liveState.ActiveCultures,
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
                _state = InternalState.Loaded;
            }
        }
        return success;
    }

    void WriteFinalSet( IActivityMonitor monitor, FinalLocaleCultureSet final )
    {
        final.PropagateFallbackTranslations( monitor );
        var tsLocaleTarget = _liveState.Paths.CKGenPath + "ts-locales" + Path.DirectorySeparatorChar;
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

