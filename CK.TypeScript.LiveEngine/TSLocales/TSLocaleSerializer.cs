using CK.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.LiveEngine;

static class TSLocaleSerializer
{
    public const string FileName = "TSLocale.dat";

    internal static void WriteLocaleCultureSet( CKBinaryWriter w,
                                                LocaleCultureSet set,
                                                CKBinaryWriter.ObjectPool<IResourceContainer> containerPool )
    {
        w.Write( set.Culture.Name );
        StateSerializer.WriteResourceLocator( w, containerPool, set.Origin );
        if( set.HasTranslations )
        {
            w.Write( set.Translations.Count );
            foreach( var t in set.Translations )
            {
                StateSerializer.WriteResourceLocator( w, containerPool, t.Value.Origin );
                w.Write( t.Key );
                w.Write( t.Value.Text );
                w.Write( (byte)t.Value.Override );
            }
        }
        else
        {
            w.Write( 0 );
        }
        if( set.Children.Count > 0 )
        {
            w.Write( set.Children.Count );
            foreach( var c in set.Children )
            {
                WriteLocaleCultureSet( w, c, containerPool );
            }
        }
        else
        {
            w.Write( 0 );
        }

    }

    internal static LocaleCultureSet ReadLocalCultureSet( CKBinaryReader r,
                                                          CKBinaryReader.ObjectPool<IResourceContainer> containerPool )
    {
        var cName = r.ReadString();
        var origin = StateSerializer.ReadResourceLocator( r, containerPool );
        Dictionary<string, TranslationValue>? tr = null;
        int count = r.ReadInt32();
        if( count > 0 )
        {
            tr = new Dictionary<string, TranslationValue>( count );
            for( int i = 0; i < count; i++ )
            {
                var tOrigin = StateSerializer.ReadResourceLocator( r, containerPool );
                var key = r.ReadString();
                tr.Add( key, new TranslationValue( r.ReadString(), tOrigin, (ResourceOverrideKind)r.ReadByte() ) );
            }
        }
        count = r.ReadInt32();
        List<LocaleCultureSet>? children = count > 0 ? new List<LocaleCultureSet>( count ) : null;
        for( int i = 0; i < count; i++ )
        {
            children!.Add( ReadLocalCultureSet( r, containerPool ) );
        }
        return LocaleCultureSet.UnsafeCreate( origin, NormalizedCultureInfo.EnsureNormalizedCultureInfo( cName ), tr, children );
    }

    internal static void WriteTSLocalesState( CKBinaryWriter w, List<object> localePackages )
    {
        // We don't use a CKBinaryWriter.ObjectPool<AssemblyResourceContainer> here:
        // regular packages are a simple index in the localePackages array.
        var containerPool = new CKBinaryWriter.ObjectPool<IResourceContainer>( w );
        w.WriteNonNegativeSmallInt32( localePackages.Count );
        foreach( var locale in localePackages )
        {
            if( locale is LocaleCultureSet c )
            {
                w.WriteSmallInt32( -1 );
                WriteLocaleCultureSet( w, c, containerPool );
            }
            else
            {
                Throw.DebugAssert( locale is LocalPackageRef );
                w.WriteSmallInt32( Unsafe.As<LocalPackageRef>( locale ).IdxLocal );
            }
        }
    }

    internal static ITSLocalePackage[]? ReadLiveTSLocales( IActivityMonitor monitor,
                                                           CKBinaryReader r,
                                                           CKBinaryReader.ObjectPool<IResourceContainer> resourceContainerPool,
                                                           ImmutableArray<LocalPackage> localPackages )
    {
        int count = r.ReadNonNegativeSmallInt32();
        var b = new ITSLocalePackage[count];
        for( int i = 0; i < count; i++ )
        {
            var idx = r.ReadSmallInt32();
            b[i] = idx < 0
                    ? new LiveTSLocales.Regular( ReadLocalCultureSet( r, resourceContainerPool ) )
                    : new LiveTSLocales.Local( localPackages[idx] );
        }
        return b;
    }

}
