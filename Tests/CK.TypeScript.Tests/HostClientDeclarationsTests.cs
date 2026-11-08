using CK.Core;
using CK.Setup;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Tests;

[TestFixture]
public class HostClientDeclarationsTests
{
    static string? Add( string json, params string[] names )
    {
        var r = HostClientDeclarations.AddMissing( Encoding.UTF8.GetBytes( json ), names, out _ );
        return r == null ? null : Encoding.UTF8.GetString( r );
    }

    static string[] Declared( string json )
    {
        using var d = JsonDocument.Parse( json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip } );
        return d.RootElement.GetProperty( "CK-AppIdentity" ).GetProperty( "Local" ).GetProperty( "Client" )
                .EnumerateObject().Select( p => p.Name ).ToArray();
    }

    [TestCase( """{}""" )]
    [TestCase( """{ "Logging": { "LogLevel": "Info" } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "FullName": "D/$P" } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": { "PublicUrl": "https://acme.com" } } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": { "Client": {} } } }""" )]
    public void the_missing_sections_are_created( string json )
    {
        var result = Add( json, "App", "MiniApp" ).ShouldNotBeNull();
        Declared( result ).ShouldBe( ["App", "MiniApp"] );
        // What existed is kept.
        if( json.Contains( "LogLevel" ) ) result.ShouldContain( "\"LogLevel\": \"Info\"" );
        if( json.Contains( "FullName" ) ) result.ShouldContain( "\"FullName\": \"D/$P\"" );
        if( json.Contains( "PublicUrl" ) ) result.ShouldContain( "\"PublicUrl\": \"https://acme.com\"" );
    }

    [Test]
    public void an_existing_declaration_is_kept_and_only_the_missing_ones_are_added()
    {
        var result = Add( """{ "CK-AppIdentity": { "Local": { "Client": { "App": { "Color": "Blue" } } } } }""", "App", "MiniApp" ).ShouldNotBeNull();
        Declared( result ).ShouldBe( ["App", "MiniApp"] );
        result.ShouldContain( "\"Color\": \"Blue\"" );
    }

    [Test]
    public void nothing_is_written_when_everything_is_declared_whatever_the_case()
    {
        Add( """{ "ck-appidentity": { "local": { "client": { "app": {} } } } }""", "App" ).ShouldBeNull();
    }

    [Test]
    public void the_comments_the_BOM_and_the_line_endings_are_kept()
    {
        var json = "{\r\n  // The application.\r\n  \"CK-AppIdentity\": {\r\n    /* Its name. */\r\n    \"FullName\": \"D/$P\"\r\n  }\r\n}\r\n";
        var bytes = Encoding.UTF8.GetPreamble().Concat( Encoding.UTF8.GetBytes( json ) ).ToArray();
        var result = HostClientDeclarations.AddMissing( bytes, ["App"], out var added ).ShouldNotBeNull();
        added.ShouldBe( ["App"] );
        result.AsSpan().StartsWith( Encoding.UTF8.GetPreamble() ).ShouldBeTrue();
        var text = Encoding.UTF8.GetString( result.AsSpan( 3 ) );
        text.ShouldContain( "The application." );
        text.ShouldContain( "Its name." );
        text.ShouldContain( "\r\n" );
        text.Replace( "\r\n", "" ).ShouldNotContain( "\n" );
        Declared( text ).ShouldBe( ["App"] );
    }

    [TestCase( """{ "CK-AppIdentity": "oops" }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": [] } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": { "Client": 3 } } }""" )]
    public void a_section_that_is_not_an_object_is_an_error( string json )
    {
        Util.Invokable( () => Add( json, "App" ) ).ShouldThrow<InvalidDataException>();
    }

    [Test]
    public void only_a_host_project_with_an_appsettings_is_updated()
    {
        var root = new NormalizedPath( Path.GetTempPath() ).AppendPart( nameof( HostClientDeclarationsTests ) + "-" + Guid.NewGuid().ToString( "N" ) );
        var notAHost = root.AppendPart( "Some.Tests" );
        var host = root.AppendPart( "Some.Host" );
        var noSettings = root.AppendPart( "Other.Host" );
        foreach( var d in new[] { notAHost, host, noSettings } )
        {
            if( Directory.Exists( d ) ) Directory.Delete( d, true );
            Directory.CreateDirectory( d );
        }
        File.WriteAllText( notAHost.AppendPart( "appsettings.json" ), "{}" );
        File.WriteAllText( host.AppendPart( "appsettings.json" ), "{}" );

        HostClientDeclarations.EnsureDeclared( TestHelper.Monitor, notAHost, ["App"] ).ShouldBeTrue();
        HostClientDeclarations.EnsureDeclared( TestHelper.Monitor, noSettings, ["App"] ).ShouldBeTrue();
        HostClientDeclarations.EnsureDeclared( TestHelper.Monitor, host, ["App"] ).ShouldBeTrue();

        File.ReadAllText( notAHost.AppendPart( "appsettings.json" ) ).ShouldBe( "{}" );
        File.Exists( noSettings.AppendPart( "appsettings.json" ) ).ShouldBeFalse();
        Declared( File.ReadAllText( host.AppendPart( "appsettings.json" ) ) ).ShouldBe( ["App"] );
        Directory.Delete( root, true );
    }
}
