using CK.Transform.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Html.Transform.Tests;

[TestFixture]
public class InsertTests
{
    [TestCase( "n°1",
        """
        <!-- <PrePublic revert /> -->
        <router-outlet />
        <!-- <PostPublic /> -->
        
        """,
        """"
        create <html> transformer
        begin
            insert before * """
                            @if( isAuthenticated() ) {

                            """;
            insert after * """
                           } @else {
                           <ck-private-page />
                           }
                           """;
        end
        """",
        """
        @if( isAuthenticated() ) {
        <!-- <PrePublic revert /> -->
        <router-outlet />
        <!-- <PostPublic /> -->
        } @else {
        <ck-private-page />
        }
        """
    )]
    [TestCase( "n°2 - with unless",
        """
        <!-- <PrePublic revert /> -->
        <router-outlet />
        <!-- <PostPublic /> -->
        
        """,
        """"
        create <html> transformer
        begin
            unless <CK.Ng.AspNet.Auth>
            begin
                insert before * """
                                @if( isAuthenticated() ) {

                                """;
                insert after * """
                               } @else {
                               <ck-private-page />
                               }
                               """;
            end
        end
        """",
        """
        <!-- <CK.Ng.AspNet.Auth /> -->
        @if( isAuthenticated() ) {
        <!-- <PrePublic revert /> -->
        <router-outlet />
        <!-- <PostPublic /> -->
        } @else {
        <ck-private-page />
        }
        """
    )]
    public void src_app_transform( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new HtmlLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }
}
