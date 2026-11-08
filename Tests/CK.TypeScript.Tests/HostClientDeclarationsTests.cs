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
        return d.RootElement.GetProperty( "CK-AppIdentity" ).GetProperty( "Local" ).GetProperty( "PartyClient" )
                .EnumerateObject().Select( p => p.Name ).ToArray();
    }

    [TestCase( """{}""" )]
    [TestCase( """{ "Logging": { "LogLevel": "Info" } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "FullName": "D/$P" } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": { "PublicUrl": "https://acme.com" } } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": { "PartyClient": {} } } }""" )]
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
        var result = Add( """{ "CK-AppIdentity": { "Local": { "PartyClient": { "App": { "Color": "Blue" } } } } }""", "App", "MiniApp" ).ShouldNotBeNull();
        Declared( result ).ShouldBe( ["App", "MiniApp"] );
        result.ShouldContain( "\"Color\": \"Blue\"" );
    }

    [Test]
    public void nothing_is_written_when_everything_is_declared_whatever_the_case()
    {
        Add( """{ "ck-appidentity": { "local": { "partyclient": { "app": {} } } } }""", "App" ).ShouldBeNull();
    }

    [Test]
    public void the_comments_the_BOM_and_the_line_endings_are_kept()
    {
        var json = "{\r\n  // The application.\r\n  \"CK-AppIdentity\": {\r\n    /* Its name. */\r\n    \"FullName\": \"D/$P\"\r\n  }\r\n}\r\n";
        var bytes = Encoding.UTF8.GetPreamble().Concat( Encoding.UTF8.GetBytes( json ) ).ToArray();
        var result = HostClientDeclarations.AddMissing( bytes, ["App"], out var added ).ShouldNotBeNull();
        added.ShouldBe( ["App"] );
        result.AsSpan().StartsWith( Encoding.UTF8.GetPreamble() ).ShouldBeTrue();
        // Only the new text is inserted.
        Encoding.UTF8.GetString( result.AsSpan( 3 ) ).ShouldBe(
            "{\r\n  // The application.\r\n  \"CK-AppIdentity\": {\r\n    /* Its name. */\r\n    \"FullName\": \"D/$P\",\r\n    \"Local\": { \"PartyClient\": { \"App\": {} } }\r\n  }\r\n}\r\n" );
    }

    [Test]
    public void the_layout_and_the_indentation_of_a_host_configuration_are_kept()
    {
        var json = """
            {
                // The application.
                "CK-AppIdentity": {
                    "FullName": "D/$P", // Its name.
                    "Local": {
                        "AllowFeatures": "HttpEndpoint",
                        // The "Main" endpoint.
                        "HttpEndpoint": { "Main": { "Urls": "http://localhost:5302" } }
                    }
                },
                "Logging": { "LogLevel": { "Default": "Warning" } }
            }
            """;
        Add( json, "App", "MiniApp" ).ShouldBe( """
            {
                // The application.
                "CK-AppIdentity": {
                    "FullName": "D/$P", // Its name.
                    "Local": {
                        "AllowFeatures": "HttpEndpoint",
                        // The "Main" endpoint.
                        "HttpEndpoint": { "Main": { "Urls": "http://localhost:5302" } },
                        "PartyClient": { "App": {}, "MiniApp": {} }
                    }
                },
                "Logging": { "LogLevel": { "Default": "Warning" } }
            }
            """ );
    }

    [Test]
    public void the_declarations_are_added_one_per_line_to_an_existing_multi_line_client_section()
    {
        var json = """
            {
              "CK-AppIdentity": {
                "Local": {
                  "PartyClient": {
                    "App": {}, // The default one.
                  }
                }
              }
            }
            """;
        // The existing trailing comma is kept, no other one is added.
        Add( json, "App", "MiniApp", "Other" ).ShouldBe( """
            {
              "CK-AppIdentity": {
                "Local": {
                  "PartyClient": {
                    "App": {}, // The default one.
                    "MiniApp": {},
                    "Other": {}
                  }
                }
              }
            }
            """ );
    }

    [TestCase( """{}""", """{ "CK-AppIdentity": { "Local": { "PartyClient": { "App": {} } } } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": { "PartyClient": {} } } }""", """{ "CK-AppIdentity": { "Local": { "PartyClient": { "App": {} } } } }""" )]
    [TestCase( """{ "A": 1 }""", """{ "A": 1, "CK-AppIdentity": { "Local": { "PartyClient": { "App": {} } } } }""" )]
    public void a_compact_object_receives_the_declarations_on_its_line( string json, string expected )
    {
        Add( json, "App" ).ShouldBe( expected );
    }

    [TestCase( """{ "CK-AppIdentity": "oops" }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": [] } }""" )]
    [TestCase( """{ "CK-AppIdentity": { "Local": { "PartyClient": 3 } } }""" )]
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
