using CK.Core;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.EmbeddedResources.Globalization.Tests;

[TestFixture]
public class ReadTests
{
    static NormalizedCultureInfo _en = NormalizedCultureInfo.CodeDefault;
    static NormalizedCultureInfo _fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );
    static NormalizedCultureInfo _frFR = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" );
    static NormalizedCultureInfo _frCA = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-CA" );
    static NormalizedCultureInfo _es = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "es" );
    static NormalizedCultureInfo _esES = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "es-ES" );
    static NormalizedCultureInfo _esMX = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "es-MX" );

    static HashSet<NormalizedCultureInfo> _activeCultures = [_fr,_frFR,_frCA,_es,_esES,_esMX];

    [Test]
    public void basic_resource_reads()
    {
        var p = new CodeGenResourceContainer( "P" );

        p.AddText( "locales/default.jsonc", """{ "Msg": "Hello" }""" );
        p.AddText( "locales/fr.jsonc", """{ "Msg": "Bonjour" }""" );
        p.AddText( "locales/fr-FR.jsonc", """{ "Msg": "Salut" }""" );
        p.AddText( "locales/fr-CA.jsonc", """{ "Msg": "Bon matin" }""" );

        p.LoadTranslations( TestHelper.Monitor, _activeCultures, out var set, "locales" );
        set.ShouldNotBeNull();
        set.Children.Count.ShouldBe( 3 );

        set.Culture.ShouldBe( _en );
        set.Translations.Count.ShouldBe( 1 );
        set.Translations["Msg"].Override.ShouldBe( ResourceOverrideKind.None );
        set.Translations["Msg"].Origin.Container.ShouldBe( p );
        set.Translations["Msg"].Origin.FullResourceName.ShouldBe( "locales/default.jsonc" );
        set.Translations["Msg"].Text.ShouldBe( "Hello" );

        var fr = set.Children.Single( s => s.Culture == _fr );
        fr.Translations.Count.ShouldBe( 1 );
        fr.Translations["Msg"].Override.ShouldBe( ResourceOverrideKind.None );
        fr.Translations["Msg"].Origin.Container.ShouldBe( p );
        fr.Translations["Msg"].Origin.FullResourceName.ShouldBe( "locales/fr.jsonc" );
        fr.Translations["Msg"].Text.ShouldBe( "Bonjour" );

        var frFR = set.Children.Single( s => s.Culture == _frFR );
        frFR.Translations.Count.ShouldBe( 1 );
        frFR.Translations["Msg"].Override.ShouldBe( ResourceOverrideKind.None );
        frFR.Translations["Msg"].Origin.Container.ShouldBe( p );
        frFR.Translations["Msg"].Origin.FullResourceName.ShouldBe( "locales/fr-FR.jsonc" );
        frFR.Translations["Msg"].Text.ShouldBe( "Salut" );

        var frCA = set.Children.Single( s => s.Culture == _frCA );
        frFR.Translations.Count.ShouldBe( 1 );
        frFR.Translations["Msg"].Override.ShouldBe( ResourceOverrideKind.None );
        frFR.Translations["Msg"].Origin.Container.ShouldBe( p );
        frFR.Translations["Msg"].Origin.FullResourceName.ShouldBe( "locales/fr-CA.jsonc" );
        frFR.Translations["Msg"].Text.ShouldBe( "Bon matin" );
    }
}
