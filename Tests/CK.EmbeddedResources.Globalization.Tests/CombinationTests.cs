using CK.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.EmbeddedResources.Globalization.Tests;

[TestFixture]
public class CombinationTests
{
    [Test]
    public void ToInitialFinalSet()
    {
        var p = new CodeGenResourceContainer( "P" );

        p.AddText( "locales/default.jsonc", """{ "Msg": "Hello" }""" );
        p.AddText( "locales/fr.jsonc", """{ "Msg": "Bonjour" }""" );
        p.AddText( "locales/fr-FR.jsonc", """{ "Msg": "Salut" }""" );
        p.AddText( "locales/fr-FR-A1.jsonc", """{ "Msg": "Wesh" }""" );
        p.AddText( "locales/fr-CA.jsonc", """{ "Msg": "Bon matin" }""" );

        p.LoadTranslations( TestHelper.Monitor, C.ActiveCultures, out var set, "locales" ).ShouldBeTrue();
        set.ShouldNotBeNull();

        var final = set.ToInitialFinalSet( TestHelper.Monitor ).ShouldNotBeNull();
        final.FindTranslationSet( C.En ).ShouldNotBeNull().Translations["Msg"].Text.ShouldBe( "Hello" );
        final.FindTranslationSet( C.Fr ).ShouldNotBeNull().Translations["Msg"].Text.ShouldBe( "Bonjour" );
        final.FindTranslationSet( C.FrFR ).ShouldNotBeNull().Translations["Msg"].Text.ShouldBe( "Salut" );
        final.FindTranslationSet( C.FrFRA1 ).ShouldNotBeNull().Translations["Msg"].Text.ShouldBe( "Wesh" );
        final.FindTranslationSet( C.FrCA ).ShouldNotBeNull().Translations["Msg"].Text.ShouldBe( "Bon matin" );

        final.FindTranslationSet( C.FrFRA2 ).ShouldBeNull();
        final.FindTranslationSet( C.Es ).ShouldBeNull();
    }

    [Test]
    public void ToInitialFinalSet_with_regular_Override_is_an_error()
    {
        var p = new CodeGenResourceContainer( "P" );
        p.AddText( "locales/default.jsonc", """{ "O:Regular": "R", "O?:Optional": "O", "O!:Always": "A", }""" );

        p.LoadTranslations( TestHelper.Monitor, C.ActiveCultures, out var set, "locales" ).ShouldBeTrue();
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            set.ShouldNotBeNull().ToInitialFinalSet( TestHelper.Monitor ).ShouldBeNull();
            logs.ShouldContain( """
                Invalid initial set of translation definitions 'locales/default.jsonc' in 'P'.
                No translation can be defined as regular override ("O:"). Only optional ("O?" - that will be skipped) and always ("O!:" - that will be kept) are allowed.
                The following resources are regular override definitions:
                Regular
                """ );
        }
    }

    [Test]
    public void Aggregate_with_ambiguities_and_resolution()
    {
        var p1 = new CodeGenResourceContainer( "P1" );
        p1.AddText( "locales/default.jsonc", """{ "Msg": "Hello" }""" );
        p1.AddText( "locales/fr.jsonc", """{ "Msg": "Bonjour" }""" );
        p1.AddText( "locales/fr-FR-A1.jsonc", """{ "Msg": "Wesh" }""" );
        p1.AddText( "locales/es.jsonc", """{ "Msg": "Hola" }""" );
        p1.LoadTranslations( TestHelper.Monitor, C.ActiveCultures, out var def1, "locales" ).ShouldBeTrue();
        var f1 = def1.ShouldNotBeNull().ToInitialFinalSet( TestHelper.Monitor ).ShouldNotBeNull();

        var p2 = new CodeGenResourceContainer( "P2" );
        p2.AddText( "locales/default.jsonc", """{ "Msg": "Hello" }""" );
        p2.AddText( "locales/fr-FR-A1.jsonc", """{ "Msg": "Yo" }""" );
        p2.LoadTranslations( TestHelper.Monitor, C.ActiveCultures, out var def2, "locales" ).ShouldBeTrue();
        var f2 = def2.ShouldNotBeNull().ToInitialFinalSet( TestHelper.Monitor ).ShouldNotBeNull();

        var agg = f1.Aggregate( f2 );
        var fr = agg.FindTranslationSet( C.Fr ).ShouldNotBeNull();
        fr.IsAmbiguous.ShouldBeFalse( "Ambiguity is not hierachical." );
        fr.Children.ShouldBeEmpty( "Translation sets without translations are not instantiated" );

        var frFRA1 = agg.FindTranslationSet( C.FrFRA1 ).ShouldNotBeNull();
        frFRA1.IsAmbiguous.ShouldBeTrue();
        frFRA1.Translations["Msg"].Ambiguities.ShouldNotBeEmpty();

        agg.FindTranslationSet( C.Es ).ShouldNotBeNull( "Spanish has been aggregated." ).IsAmbiguous.ShouldBeFalse();
        agg.IsAmbiguous.ShouldBeTrue( "The root or'ed all the sets ambiguities." );

        // Resolution by the "Override folder": this folder doesn't need to specify "O:" overrides
        // as all its definitions are by design overrides.
        // Moreover, it doesn't require a "default.jsonc" (and if there's one, it can be named "en.json).
        {
            var pOverride = new CodeGenResourceContainer( "POverride" );
            pOverride.AddText( "locales/fr-FR-A1.jsonc", """{ "Msg": "It's Yo!" }""" );
            pOverride.LoadTranslations( TestHelper.Monitor, C.ActiveCultures, out var defOverride, "locales", isOverrideFolder: true ).ShouldBeTrue();
            var fOverride = defOverride.ShouldNotBeNull().Combine( TestHelper.Monitor, agg ).ShouldNotBeNull();
            fOverride.IsAmbiguous.ShouldBeFalse();
            var frFRA1Fixed = fOverride.FindTranslationSet( C.FrFRA1 ).ShouldNotBeNull();
            frFRA1Fixed.IsAmbiguous.ShouldBeFalse();
            var t = frFRA1Fixed.Translations["Msg"];
            t.Ambiguities.ShouldBeNull();
            t.Text.ShouldBe( "It's Yo!" );
        }

        // Resoltion by a regular folder. The 'default.jsonc' must exist, even if it can be empty because
        // overrides are not actual definitions!
        {
            var pOverride = new CodeGenResourceContainer( "SomeP" );
            pOverride.AddText( "locales/default.jsonc", "{}" );
            pOverride.AddText( "locales/fr-FR-A1.jsonc", """{ "O:Msg": "It's Yo!" }""" );
            pOverride.LoadTranslations( TestHelper.Monitor, C.ActiveCultures, out var defOverride, "locales", isOverrideFolder: true ).ShouldBeTrue();
            var fOverride = defOverride.ShouldNotBeNull().Combine( TestHelper.Monitor, agg ).ShouldNotBeNull();
            fOverride.IsAmbiguous.ShouldBeFalse();
            var frFRA1Fixed = fOverride.FindTranslationSet( C.FrFRA1 ).ShouldNotBeNull();
            frFRA1Fixed.IsAmbiguous.ShouldBeFalse();
            var t = frFRA1Fixed.Translations["Msg"];
            t.Ambiguities.ShouldBeNull();
            t.Text.ShouldBe( "It's Yo!" );
        }
    }
}
