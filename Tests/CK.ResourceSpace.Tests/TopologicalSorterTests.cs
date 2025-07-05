using CK.Core;
using CK.EmbeddedResources;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using System.Runtime.InteropServices;
using static CK.Testing.MonitorTestHelper;

namespace CK.ResourceSpace.Tests;

[TestFixture]
public partial class TopologicalSorterTests
{
    static ResPackageDescriptor CreatePackage( ResSpaceConfiguration config, string name )
    {
        return config.RegisterPackage( TestHelper.Monitor, name,
                                       default,
                                       new EmptyResourceContainer( name, isDisabled: false ),
                                       new EmptyResourceContainer( name, isDisabled: false ),
                                       false )
                     .ShouldNotBeNull();
    }

    [TestCase( true )]
    [TestCase( false )]
    public void without_any_constraints( bool revertOrdering )
    {
        var config = new ResSpaceConfiguration();
        config.RevertOrderingNames = revertOrdering;
        var dA = CreatePackage( config, "A" );
        var dB = CreatePackage( config, "B" );
        var dC = CreatePackage( config, "C" );
        var dD = CreatePackage( config, "D" );

        var collector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();
        var builder = new ResSpaceDataBuilder( collector );
        var spaceData = builder.Build( TestHelper.Monitor ).ShouldNotBeNull();

        if( !revertOrdering )
        {
            spaceData.Packages.Select( p => p.FullName ).ShouldBe( ["<Code>", "A", "B", "C", "D", "<App>"] );
            spaceData.AllPackageResources
                    .Select( p => p.ToString() ).ShouldBe(
                        [
                            "Before <Code>", "After <Code>",
                            "Before A", "After A", "Before B", "After B", "Before C", "After C", "Before D", "After D",
                            "Before <App>", "After <App>"
                        ] );
        }
        else
        {
            spaceData.Packages.Select( p => p.FullName ).ShouldBe( ["<Code>", "D", "C", "B", "A", "<App>"] );
            spaceData.AllPackageResources
                    .Select( p => p.ToString() ).ShouldBe(
                        [
                            "Before <Code>", "After <Code>",
                            "Before D", "After D", "Before C", "After C", "Before B", "After B", "Before A", "After A",
                            "Before <App>", "After <App>"
                        ] );
        }
    }

    [TestCase( true )]
    [TestCase( false )]
    public void with_requires( bool revertOrdering )
    {
        var config = new ResSpaceConfiguration();
        config.RevertOrderingNames = revertOrdering;
        var dA = CreatePackage( config, "A" );
        var dB = CreatePackage( config, "B" );
        var dC = CreatePackage( config, "C" );
        var dD = CreatePackage( config, "D" );
        dD.Requires.Add( dC );
        dC.Requires.Add( dB );
        dB.Requires.Add( dA );

        var collector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();
        var builder = new ResSpaceDataBuilder( collector );
        var spaceData = builder.Build( TestHelper.Monitor ).ShouldNotBeNull();

        spaceData.Packages.Select( p => p.FullName ).ShouldBe( ["<Code>", "A", "B", "C", "D", "<App>"] );
        spaceData.AllPackageResources
                .Select( p => p.ToString() ).ShouldBe(
                    [
                        "Before <Code>", "After <Code>",
                        "Before A", "After A", "Before B", "After B", "Before C", "After C", "Before D", "After D",
                        "Before <App>", "After <App>"
                    ] );
    }

    [TestCase( true )]
    [TestCase( false )]
    public void with_children( bool revertOrdering )
    {
        var config = new ResSpaceConfiguration();
        config.RevertOrderingNames = revertOrdering;
        var dA = CreatePackage( config, "A" );
        var dB = CreatePackage( config, "B" );
        var dC = CreatePackage( config, "C" );
        var dD = CreatePackage( config, "D" );
        dD.Requires.Add( dA );
        dA.Children.Add( dB );
        dA.Children.Add( dC );
        dB.RequiredBy.Add( dC );

        var collector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();
        var builder = new ResSpaceDataBuilder( collector );
        var spaceData = builder.Build( TestHelper.Monitor ).ShouldNotBeNull();

        spaceData.Packages.Select( p => p.FullName ).ShouldBe( ["<Code>", "B", "C", "A", "D", "<App>"] );
        spaceData.AllPackageResources
                .Select( p => p.ToString() ).ShouldBe(
                    [
                        "Before <Code>", "After <Code>",
                        "Before A", "Before B", "After B", "Before C", "After C", "After A", "Before D", "After D",
                        "Before <App>", "After <App>"
                    ] );
    }

    [TestCase( true )]
    [TestCase( false )]
    public void with_children_and_requires( bool revertOrdering )
    {
        var config = new ResSpaceConfiguration();
        config.RevertOrderingNames = revertOrdering;
        var dA = CreatePackage( config, "A" );
        var dBInA = CreatePackage( config, "BinA" );
        var dCInA = CreatePackage( config, "CinA" );
        var dD = CreatePackage( config, "D" );
        var dEinD = CreatePackage( config, "EinD" );
        dD.Requires.Add( dA );
        dA.Children.Add( dBInA );
        dA.Children.Add( dCInA );
        dBInA.RequiredBy.Add( dCInA );
        dD.Children.Add( dEinD );

        var collector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();
        var builder = new ResSpaceDataBuilder( collector );
        var spaceData = builder.Build( TestHelper.Monitor ).ShouldNotBeNull();

        spaceData.Packages.Select( p => p.FullName ).ShouldBe( ["<Code>", "BinA", "CinA", "A", "EinD", "D", "<App>"] );
        spaceData.AllPackageResources
                .Select( p => p.ToString() ).ShouldBe(
                    [
                        "Before <Code>", "After <Code>",
                        "Before A", "Before BinA", "After BinA", "Before CinA", "After CinA", "After A",
                        "Before D", "Before EinD", "After EinD", "After D",
                        "Before <App>", "After <App>"
                    ] );
    }

    [TestCase( true, "RequiresAndChild" )]
    [TestCase( false, "RequiresAndChild" )]
    [TestCase( true, "FromType" )]
    [TestCase( false, "FromType" )]
    public void TS_Angular_structure( bool revertOrdering, string regType )
    {
        /*
            AppRoutedComponent => <Code>
            DemoNgModule => <Code>
            PublicPageComponent => <Code>
            Zorro => <Code>
            LogoutConfirmComponent => <Code>
            LoginComponent => PublicPageComponent
            LogoutResultComponent => LogoutConfirmComponent
            PublicFooterComponent => <Code>
            PublicTopbarComponent => <Code>
            PasswordLostComponent => LoginComponent
            PublicSectionComponent => Zorro
                            |PublicTopbarComponent
                            |PublicFooterComponent
            SomeAuthPackage => <Code>
                            |PasswordLostComponent
                            |LogoutResultComponent
                            |LogoutConfirmComponent
                            |LoginComponent
            <App> => AppRoutedComponent, DemoNgModule, PublicSectionComponent, SomeAuthPackage
         */

        ResSpaceConfiguration config = regType == "RequiresAndChild"
                                        ? ExplicitGraph( revertOrdering )
                                        : FromType( revertOrdering );

        var collector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();
        var builder = new ResSpaceDataBuilder( collector );
        var spaceData = builder.Build( TestHelper.Monitor ).ShouldNotBeNull();

        if( !revertOrdering )
        {
            spaceData.Packages.Select( p => p.FullName ).ShouldBe(
            [
                "<Code>",
                "AppRoutedComponent", "DemoNgModule", "PublicPageComponent", "LoginComponent",
                "LogoutConfirmComponent", "LogoutResultComponent", "PasswordLostComponent", "Zorro",
                "PublicFooterComponent", "PublicTopbarComponent", "PublicSectionComponent", "SomeAuthPackage",
                "<App>"] );
            spaceData.AllPackageResources
                    .Select( p => p.ToString() ).ShouldBe( [
                        "Before <Code>",
                        "After <Code>",
                        "Before AppRoutedComponent", "After AppRoutedComponent",
                        "Before DemoNgModule", "After DemoNgModule",
                        "Before SomeAuthPackage",
                            "Before PublicPageComponent",
                            "After PublicPageComponent",
                            "Before LoginComponent",
                            "After LoginComponent",
                            "Before LogoutConfirmComponent",
                            "After LogoutConfirmComponent",
                            "Before LogoutResultComponent",
                            "After LogoutResultComponent",
                            "Before PasswordLostComponent",
                            "After PasswordLostComponent",
                        "Before Zorro",
                        "After Zorro",
                        "Before PublicSectionComponent",
                            "Before PublicFooterComponent",
                            "After PublicFooterComponent",
                            "Before PublicTopbarComponent",
                            "After PublicTopbarComponent",
                        "After PublicSectionComponent",
                        "After SomeAuthPackage",
                        "Before <App>",
                        "After <App>" ] );
        }
        else
        {
            spaceData.Packages.Select( p => p.FullName ).ShouldBe( [
                        "<Code>",
                        "Zorro", "PublicPageComponent", "LoginComponent", "PasswordLostComponent", "LogoutConfirmComponent",
                        "LogoutResultComponent", "SomeAuthPackage", "PublicTopbarComponent", "PublicFooterComponent",
                        "PublicSectionComponent", "DemoNgModule", "AppRoutedComponent",
                        "<App>" ] );

            spaceData.AllPackageResources
                    .Select( p => p.ToString() ).ShouldBe( [
                        "Before <Code>",
                        "After <Code>",
                        "Before Zorro",
                        "After Zorro",
                        "Before SomeAuthPackage",
                            "Before PublicPageComponent",
                            "After PublicPageComponent",
                            "Before LoginComponent",
                            "After LoginComponent",
                            "Before PasswordLostComponent",
                            "After PasswordLostComponent",
                            "Before LogoutConfirmComponent",
                            "After LogoutConfirmComponent",
                            "Before LogoutResultComponent",
                            "After LogoutResultComponent",
                        "After SomeAuthPackage",
                        "Before PublicSectionComponent",
                            "Before PublicTopbarComponent",
                            "After PublicTopbarComponent",
                            "Before PublicFooterComponent",
                            "After PublicFooterComponent",
                        "After PublicSectionComponent",
                        "Before DemoNgModule",
                        "After DemoNgModule",
                        "Before AppRoutedComponent",
                        "After AppRoutedComponent",
                        "Before <App>",
                        "After <App>"
                     ] );
        }

        static ResSpaceConfiguration ExplicitGraph( bool revertOrdering )
        {
            var config = new ResSpaceConfiguration();
            config.RevertOrderingNames = revertOrdering;
            var dAppRoutedComponent = CreatePackage( config, "AppRoutedComponent" );
            var dDemoNgModule = CreatePackage( config, "DemoNgModule" );
            var dPublicPageComponent = CreatePackage( config, "PublicPageComponent" );
            var dZorro = CreatePackage( config, "Zorro" );
            var dLogoutConfirmComponent = CreatePackage( config, "LogoutConfirmComponent" );
            var dLoginComponent = CreatePackage( config, "LoginComponent" );
            var dLogoutResultComponent = CreatePackage( config, "LogoutResultComponent" );
            var dPublicFooterComponent = CreatePackage( config, "PublicFooterComponent" );
            var dPublicTopbarComponent = CreatePackage( config, "PublicTopbarComponent" );
            var dPasswordLostComponent = CreatePackage( config, "PasswordLostComponent" );
            var dPublicSectionComponent = CreatePackage( config, "PublicSectionComponent" );
            var dSomeAuthPackage = CreatePackage( config, "SomeAuthPackage" );
            dLoginComponent.Requires.Add( dPublicPageComponent );
            dLogoutResultComponent.Requires.Add( dLogoutConfirmComponent );
            dPasswordLostComponent.Requires.Add( dLoginComponent );
            dPublicSectionComponent.Requires.Add( dZorro );
            dPublicSectionComponent.Children.Add( dPublicTopbarComponent );
            dPublicSectionComponent.Children.Add( dPublicFooterComponent );
            dSomeAuthPackage.Children.Add( dPasswordLostComponent );
            dSomeAuthPackage.Children.Add( dLogoutResultComponent );
            dSomeAuthPackage.Children.Add( dLogoutConfirmComponent );
            dSomeAuthPackage.Children.Add( dLoginComponent );
            return config;
        }

        static ResSpaceConfiguration FromType( bool revertOrdering )
        {
            var config = new ResSpaceConfiguration();
            config.RevertOrderingNames = revertOrdering;
            config.RegisterPackage( TestHelper.Monitor, typeof( AppRoutedComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( DemoNgModule ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( PublicPageComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( Zorro ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( LogoutConfirmComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( LoginComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( LogoutResultComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( PublicFooterComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( PublicTopbarComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( PasswordLostComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( PublicSectionComponent ) );
            config.RegisterPackage( TestHelper.Monitor, typeof( SomeAuthPackage ) );
            return config;
        }
    }

}
