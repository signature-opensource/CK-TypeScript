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
                            <ck-private-page />
                            } @else {

                            """;
            insert "}" after *;
        end
        """",
        """
        @if( isAuthenticated() ) {
        <ck-private-page />
        } @else {
        <!-- <PrePublic revert /> -->
        <router-outlet />
        <!-- <PostPublic /> -->
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
                                <ck-private-page />
                                } @else {
        
                                """;
                insert "}" after *;
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
