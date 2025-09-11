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
        <ck-private-page />
        } @else {
        <!-- <PrePublic revert /> -->
        <router-outlet />
        <!-- <PostPublic /> -->
        }
        """
    )]
    public void src_app_transform( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new HtmlLanguage() );
        h.ApplyAndCheck( source, transformer, result );
    }

    [TestCase( "n°1",
        """
        TEXT
        """,
        """"
        create <html> transformer
        begin
            insert """
                   <fa-icon [icon]="ytIcon" class="icon"></fa-icon>
        
                   """ before "TEXT";
        end
        """",
        """
        <fa-icon [icon]="ytIcon" class="icon"></fa-icon>
        TEXT
        """
    )]
    [TestCase( "n°2",
        """
        TEXT
        """,
        """"
        create <html> transformer
        begin
            insert """<fa-icon [icon]="ytIcon" class="icon" ></fa-icon>""" after "TEXT";
        end
        """",
        """
        TEXT<fa-icon [icon]="ytIcon" class="icon" ></fa-icon>
        """
    )]

    public void insert_before( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new HtmlLanguage() );
        h.ApplyAndCheck( source, transformer, result );
    }
}
